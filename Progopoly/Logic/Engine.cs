using Progopoly.Models;
using Progopoly.Models.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Progopoly.Logic
{
    public class Engine
    {
        private IDiceRoller _diceRoller;
        private IGameLog _gameLog;
        public event EventHandler GameStateUpdated;

        private void OnGameStateUpdated(EventArgs e)
        {
            GameStateUpdated?.Invoke(this, e);
        }

        public Engine(IDiceRoller diceRoller, IGameLog gameLog)
        {
            _diceRoller = diceRoller;
            _gameLog = gameLog;
        }

        public GameState CreateInitialGameState(List<Player> players)
        {
            var initialGameState = new GameState
            {
                Players = players,
                Tiles = GetTiles(),
                CurrentPlayerListIndex = 0,
                CurrentFrameId = 0
            };

            for (var i = 0; i < initialGameState.Players.Count(); i++)
            {
                initialGameState.Players[i].TurnOrder = i + 1;
                initialGameState.Players[i].Color = (Constants.PlayerColor)(i + 1);
            }

            for (var i=0; i<initialGameState.Tiles.Count(); i++)
            {
                initialGameState.Tiles[i].ID = i;
                initialGameState.Tiles[i].OnGameStateUpdated = OnGameStateUpdated;
            }
            
            initialGameState.TradeOffers = new List<TradeOffer>();
            initialGameState.CommunityChestDeck = new CommunityChestDeck(_gameLog);
            initialGameState.ChanceDeck = new ChanceDeck(_gameLog);
            initialGameState.TurnTimeoutSeconds = Constants.DEFAULT_TURN_TIMEOUT_SECONDS;

            return initialGameState;
        }

        public Player CreateNewPlayer(string playerName)
        {
            return new Player
            {
                BoardPosition = 0,
                ID = Guid.NewGuid(),
                Money = Constants.STARTING_MONEY,
                Name = playerName
            };
        }

        public void RollDiceAndMovePlayer(Guid playerID, GameState gameState)
        {
            if (playerID != gameState.CurrentPlayer.ID)
            {
                _gameLog.Log($"Player [{playerID}] was stopped from rolling out of his turn.");
                return;
            }
            if (gameState.CurrentPlayer.CurrentDiceRoll != null && gameState.CurrentPlayer.NumDoublesRolledInARow == 0)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' was stopped from rolling again on his turn.");
                return;
            }
            if (gameState.AuctionInProgress || gameState.WaitForBuyOrAuctionStart)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot roll until the current property is bought or auctioned.");
                return;
            }
            if (gameState.CurrentPlayer.Money < 0)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot roll until he sells, mortgages, or declares bankruptcy.");
                return;
            }

            var diceRoll = _diceRoller.Roll(2);
            gameState.CurrentPlayer.CurrentDiceRoll = diceRoll;
            _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' rolled a {diceRoll}");

            if (diceRoll.IsDouble)
            {
                gameState.CurrentPlayer.NumDoublesRolledInARow++;
            }
            else
            {
                gameState.CurrentPlayer.NumDoublesRolledInARow = 0;
            }

            //Handle get out of jail
            if (gameState.CurrentPlayer.IsInJail)
            {
                if (gameState.CurrentPlayer.NumDoublesRolledInARow > 0)
                {
                    _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' rolled a double and got out of jail!");
                    gameState.CurrentPlayer.IsInJail = false;
                }
                else if (++gameState.CurrentPlayer.RollsWhileInJail >= 3)
                {
                    gameState.CurrentPlayer.Money -= Constants.GET_OUT_OF_JAIL_FEE;
                    gameState.CurrentPlayer.IsInJail = false;
                    _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' paid ${Constants.GET_OUT_OF_JAIL_FEE} to get out of jail.");
                }
            }

            if (!gameState.CurrentPlayer.IsInJail)
            {
                //Handle "speeding" jail condition (doubles rolled in a row = 3)
                gameState.CurrentPlayer.RollsWhileInJail = 0;
                if (gameState.CurrentPlayer.NumDoublesRolledInARow >= 3)
                {
                    _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' went to jail for over-speeding!");
                    gameState.CurrentPlayer.BoardPosition = gameState.Tiles.IndexOf(gameState.Tiles.First(x => x is JailTile));
                    gameState.CurrentPlayer.WasDirectMovement = true;
                    gameState.CurrentPlayer.IsInJail = true;

                    gameState.CurrentPlayer.NumDoublesRolledInARow = 0; //reset double counter?

                    FinishPlayerTurn(gameState.CurrentPlayer.ID, gameState);
                    return;
                }

                //Move player like normal:

                //Handle passing/landing on GO
                var targetBoardPosition = gameState.CurrentPlayer.BoardPosition + diceRoll.Dice.Sum();

                if (targetBoardPosition >= gameState.Tiles.Count())
                {
                    _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' collected a salary of ${Constants.GO_TILE_SALARY}!");
                    gameState.CurrentPlayer.Money += Constants.GO_TILE_SALARY;
                    targetBoardPosition -= gameState.Tiles.Count();
                }

                gameState.CurrentPlayer.BoardPosition = targetBoardPosition;

                //Handle passing/landing on GO
                if (gameState.CurrentPlayer.BoardPosition >= gameState.Tiles.Count())
                {
                    _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' collected a salary of ${Constants.GO_TILE_SALARY}!");
                    gameState.CurrentPlayer.Money += Constants.GO_TILE_SALARY;
                    gameState.CurrentPlayer.BoardPosition -= gameState.Tiles.Count();
                }

                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' landed on {gameState.CurrentTile.Name}!");
                OnGameStateUpdated(EventArgs.Empty);
                gameState.CurrentTile.LandedOnAction(gameState, _gameLog);
            }
        }

        public void FinishPlayerTurn(Guid playerID, GameState gameState)
        {
            if (playerID != gameState.CurrentPlayer.ID)
            {
                _gameLog.Log($"Player [{playerID}] was stopped from going out of turn.");
                return;
            }
            if (gameState.CurrentPlayer.CurrentDiceRoll == null)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot stop his turn without first rolling the dice.");
                return;
            }
            if (gameState.WaitForBuyOrAuctionStart || gameState.AuctionInProgress)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' must buy or auction the current property '{gameState.CurrentTile.Name}' before finishing his turn.");
                return;
            }
            if (gameState.CurrentPlayer.CurrentDiceRoll.IsDouble && !gameState.CurrentPlayer.IsInJail)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' must roll again before finishing his turn.");
                return;
            }
            //if (gameState.CurrentPlayer.Money < 0)
            //{
            //    _gameLog.Log($"Player [{gameState.CurrentPlayer.ID}] {gameState.CurrentPlayer.Name} must sell buildings, mortgage properties, or declare bankruptcy!");
            //    return;
            //}

            _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' finished his turn.");

            SwitchToNextPlayer(gameState);
            foreach (var player in gameState.Players)
            {
                player.CurrentDiceRoll = null;
            }
            gameState.ChanceDeck.CurrentPlayerCardText = null;
            gameState.CommunityChestDeck.CurrentPlayerCardText = null;

            OnGameStateUpdated(EventArgs.Empty);
        }

        public void BuyOutOfJail(Guid playerID, GameState gameState)
        {
            if (playerID != gameState.CurrentPlayer.ID)
            {
                _gameLog.Log($"Player [{playerID}] was stopped from rolling out of his turn.");
                return;
            }
            if (!gameState.CurrentPlayer.IsInJail)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot buy out of jail because he/she is not in jail!");
                return;
            }
            if (gameState.CurrentPlayer.Money < Constants.GET_OUT_OF_JAIL_FEE)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot buy out of jail because he/she does not have enough money!");
                return;
            }
            if (gameState.CurrentPlayer.CurrentDiceRoll != null)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot buy out of jail on this turn.");
                return;
            }

            gameState.CurrentPlayer.Money -= Constants.GET_OUT_OF_JAIL_FEE;
            gameState.CurrentPlayer.IsInJail = false;

            _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' paid ${Constants.GET_OUT_OF_JAIL_FEE} to get out of jail!");

            RollDiceAndMovePlayer(gameState.CurrentPlayer.ID, gameState);
        }

        public void UseGetOutOfJailFreeCard(Guid playerID, GameState gameState)
        {
            if (playerID != gameState.CurrentPlayer.ID)
            {
                _gameLog.Log($"Player [{playerID}] was stopped from rolling out of his turn.");
                return;
            }
            if (!gameState.CurrentPlayer.IsInJail)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot use get out of jail card because he/she is not in jail!");
                return;
            }
            if (!gameState.CurrentPlayer.HasGetOutOfJailFreeCard)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot use get out of jail card because he/she does not have enough one!");
                return;
            }
            if (gameState.CurrentPlayer.CurrentDiceRoll != null)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot get out of jail on this turn.");
                return;
            }

            gameState.CurrentPlayer.HasGetOutOfJailFreeCard = false;
            gameState.CurrentPlayer.IsInJail = false;

            _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' used a get out of jail free card to get out of jail!");

            RollDiceAndMovePlayer(gameState.CurrentPlayer.ID, gameState);
        }

        private void SwitchToNextPlayer(GameState gameState, bool incrementListIndex = true)
        {
            if (gameState.GameFinished)
                return;

            if (incrementListIndex)
                ++gameState.CurrentPlayerListIndex;

            gameState.Auction = null;
            gameState.WaitForBuyOrAuctionStart = false;
            _gameLog.Log($"Current player turn: {gameState.CurrentPlayer.Name}!");
            if (gameState.Players.Take(2).Count() == 1)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' won!");
            }
        }

        public void BuyProperty(Guid playerID, GameState gameState)
        {
            if (playerID != gameState.CurrentPlayer.ID)
            {
                _gameLog.Log($"Player [{playerID}] was stopped from going out of turn.");
                return;
            }

            var propertyTile = gameState.CurrentTile as PropertyTile;
            if (!gameState.WaitForBuyOrAuctionStart || propertyTile == null)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot buy right now.");
                return;
            }
            if (propertyTile.OwnerPlayerID != null)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot but the property because someone already owns that property.");
                return;
            }
            if (gameState.CurrentPlayer.Money < propertyTile.Cost)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot afford the property (player money: ${gameState.CurrentPlayer.Money}, cost: ${propertyTile.Cost}).");
                return;
            }

            gameState.CurrentPlayer.Money -= propertyTile.Cost;
            propertyTile.OwnerPlayerID = gameState.CurrentPlayer.ID;
            gameState.WaitForBuyOrAuctionStart = false;
            _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' bought property '{propertyTile.Name}'!");
        }

        public void StartAuctionOnProperty(Guid playerID, GameState gameState)
        {
            if (playerID != gameState.CurrentPlayer.ID)
            {
                _gameLog.Log($"Player [{playerID}] was stopped from going out of turn.");
                return;
            }

            var propertyTile = gameState.CurrentTile as PropertyTile;
            if (!gameState.WaitForBuyOrAuctionStart || gameState.AuctionInProgress || propertyTile == null)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot auction right now.");
                return;
            }
            if (gameState.Players.All(x => x.Money == 0))
            {
                _gameLog.Log($"No one has money, so this auction cannot be started!");
                return;
            }

            gameState.WaitForBuyOrAuctionStart = false;
            gameState.Auction = new Auction()
            {
                AuctionParticipants = gameState.Players
                    .Where(x => x.Money > 0)
                    .Select(x => new AuctionParticipant(x)).ToList()
            };

            _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' started a blind auction on property '{propertyTile.Name}'! All players with money must bet.");

            if (gameState.Auction.AuctionParticipants.Take(2).Count() == 1)
            {
                var winningParticipant = gameState.Auction.AuctionParticipants.First();
                winningParticipant.AuctionBet = 1;
                _gameLog.Log($"Player '{winningParticipant.Name}' won the auction and bought property '{propertyTile.Name}' for ${winningParticipant.AuctionBet}!");
                gameState.Players.First(x => x.ID == winningParticipant.ID).Money -= (int)winningParticipant.AuctionBet;
                propertyTile.OwnerPlayerID = winningParticipant.ID;
                gameState.Auction = null;
            }
        }

        public void BetOnAuction(Guid playerID, GameState gameState, int betAmount)
        {
            var propertyTile = gameState.CurrentTile as PropertyTile;
            var player = gameState.Players.FirstOrDefault(x => x.ID == playerID) ??
                gameState.Auction?.AuctionParticipants.FirstOrDefault(x => x.ID == playerID);
            if (player == null)
            {
                _gameLog.Log($"Player [{playerID}] cannot be found!");
                return;
            }
            if (!gameState.AuctionInProgress || propertyTile == null)
            {
                _gameLog.Log($"Player '{player.Name}' cannot bet because there is no auction in progress!");
                return;
            }
            if (!gameState.Auction.AuctionParticipants.Any(x => x.ID == playerID))
            {
                _gameLog.Log($"Player '{player.Name}' cannot bet because he/she is not participating in the auction.");
                return;
            }
            if (gameState.Auction.AuctionParticipants.First(x => x.ID == playerID).AuctionBet != null)
            {
                _gameLog.Log($"Player '{player.Name}' has already placed a bet and cannot bet again.");
                return;
            }

            var bettingPlayer = gameState.Auction.AuctionParticipants.First(p => p.ID == playerID);
            bettingPlayer.AuctionBet = betAmount;
            bettingPlayer.BetPosition = ++gameState.Auction.TotalBets;

            _gameLog.Log($"Player '{bettingPlayer.Name}' just placed an auction bet for property '{propertyTile.Name}'.");

            if (gameState.Auction.AuctionParticipants.All(x => x.AuctionBet != null))
            {
                if (gameState.Auction.AuctionParticipants.All(x => x.AuctionBet == 0))
                {
                    //pass on property:
                    _gameLog.Log($"Nobody won the auction for property '{propertyTile.Name}'.");
                }
                else
                {
                    //determine winner:
                    var auctionGroups = gameState.Auction.AuctionParticipants
                        .GroupBy(x => x.AuctionBet)
                        .OrderByDescending(x => x.Key);

                    var auctionWinner = auctionGroups.First().OrderBy(x => x.BetPosition).First();

                    if (auctionGroups.First().Count() > 1)
                    {
                        _gameLog.Log($"There was a tie bet for the auction on property '{propertyTile.Name}'.");
                    }

                    _gameLog.Log($"Player '{auctionWinner.Name}' wins property '{propertyTile.Name}' for ${auctionWinner.AuctionBet}!");
                    gameState.Players.First(x => x.ID == auctionWinner.ID).Money -= (int)auctionWinner.AuctionBet;
                    propertyTile.OwnerPlayerID = auctionWinner.ID;
                }

                gameState.Auction = null;
            }
        }

        public void InstantMonopoly(Guid playerID, GameState gameState)
        {
            if (!gameState.Players.Any(x => x.ID == playerID))
            {
                _gameLog.Log($"Player [{playerID}] can't cheat if I don't know who you are! Help me out here!");
                return;
            }
            var player = gameState.Players.First(x => x.ID == playerID);

            player.Money = 10000;
            var orangeProperties = gameState.Tiles
                .Where(x => x is ColorPropertyTile)
                .Select(x => x as ColorPropertyTile)
                .Where(x => x.Color == Constants.PropertyColor.Orange)
                .ToList();

            orangeProperties[0].OwnerPlayerID = player.ID;
            orangeProperties[1].OwnerPlayerID = player.ID;
            orangeProperties[2].OwnerPlayerID = player.ID;

            _gameLog.Log($"Player [{playerID}] {player.Name} just got a monopoly! (heh heh heh)");

        }

        public void BuyAndBuildHouse(Guid playerID, GameState gameState, int tileIndex)
        {
            if (playerID != gameState.CurrentPlayer.ID)
            {
                _gameLog.Log($"Player [{playerID}] was stopped from going out of turn.");
                return;
            }
            if (tileIndex >= gameState.Tiles.Count() || tileIndex < 0)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' can't build a house because no matching tile was found.");
                return;
            }
            if (!(gameState.Tiles[tileIndex] is ColorPropertyTile colorPropertyTile))
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot build a house because that tile is not a colored property tile!");
                return;
            }
            if (colorPropertyTile.OwnerPlayerID != playerID)
            {
                _gameLog.Log($"Player ['{gameState.CurrentPlayer.Name}' cannot build because he/she does not own this property!");
                return;
            }
            if (colorPropertyTile.IsMortgaged)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot build because this property is mortgaged!");
                return;
            }
            if (colorPropertyTile.BuildingCount >= Constants.MAX_BUILDINGS_ON_PROPERTY)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot build because this property has reached max potential!");
                return;
            }
            if (gameState.CurrentPlayer.Money < colorPropertyTile.BuildingCost)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot build because he/she does not have enough money! (cost to build: ${colorPropertyTile.BuildingCost})");
                return;
            }

            var sameColoredProperties = gameState.Tiles
                .Where(t => t is ColorPropertyTile)
                .Select(t => t as ColorPropertyTile)
                .Where(t => t.Color == colorPropertyTile.Color);
            if (!sameColoredProperties.All(p => p.OwnerPlayerID == playerID))
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot build because he/she does not own all the properties of the same color!");
                return;
            }
            if (sameColoredProperties.Any(p => Math.Abs(colorPropertyTile.BuildingCount + 1 - p.BuildingCount) > 1))
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot build on this property because buildings must be built evenly on properties of the same color.");
                return;
            }
            if (sameColoredProperties.Any(p => p.IsMortgaged))
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot build on this property because one or more of the other properties of the same color are mortgaged.");
                return;
            }

            gameState.CurrentPlayer.Money -= colorPropertyTile.BuildingCost;
            colorPropertyTile.BuildingCount++;

            _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' just built on property '{colorPropertyTile.Name}'!");
        }

        public void MortgageProperty(Guid playerID, GameState gameState, int tileIndex)
        {
            if (playerID != gameState.CurrentPlayer.ID)
            {
                _gameLog.Log($"Player [{playerID}] was stopped from going out of turn.");
                return;
            }
            if (tileIndex >= gameState.Tiles.Count() || tileIndex < 0)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' tried to mortgage a property that doesn't exist! Is he trying to cheat? Get out of here Hugh!");
                return;
            }
            if (!(gameState.Tiles[tileIndex] is PropertyTile propertyTile))
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' can only mortgage property tiles!");
                return;
            }
            if (propertyTile.OwnerPlayerID != playerID)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot mortgage because he/she does not own this property!");
                return;
            }
            if (propertyTile is ColorPropertyTile && (propertyTile as ColorPropertyTile).BuildingCount > 0)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot mortgage a property with buildings on it.");
                return;
            }
            if (propertyTile.IsMortgaged)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot mortgage this property because it is already mortgaged!");
                return;
            }
            if (propertyTile is ColorPropertyTile)
            {
                var sameColoredProperties = gameState.Tiles
                    .Where(t => t is ColorPropertyTile)
                    .Select(t => t as ColorPropertyTile)
                    .Where(t => t.Color == (propertyTile as ColorPropertyTile).Color);
                if (sameColoredProperties.Any(p => p.BuildingCount > 0))
                {
                    _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot mortgage this property because other properties of the same color have house(s) on them.");
                    return;
                }
            }

            gameState.CurrentPlayer.Money += propertyTile.MortgageValue;
            propertyTile.IsMortgaged = true;
        }

        public void RedeemProperty(Guid playerID, GameState gameState, int tileIndex)
        {
            if (playerID != gameState.CurrentPlayer.ID)
            {
                _gameLog.Log($"Player [{playerID}] was stopped from going out of turn.");
                return;
            }
            if (tileIndex >= gameState.Tiles.Count() || tileIndex < 0)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' tried to redeem a property that doesn't exist! Is he trying to cheat? Get out of here Hugh!");
                return;
            }
            if (!(gameState.Tiles[tileIndex] is PropertyTile propertyTile))
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' can only redeem property tiles!");
                return;
            }
            if (propertyTile.OwnerPlayerID != playerID)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot redeem because he/she does not own this property!");
                return;
            }
            if (!propertyTile.IsMortgaged)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot redeem this property because it is not mortgaged!");
                return;
            }
            if (gameState.CurrentPlayer.Money < propertyTile.RedeemValue)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot redeem because he/she does not have enough money! (cost to redeem: ${propertyTile.RedeemValue})");
                return;
            }

            gameState.CurrentPlayer.Money -= propertyTile.RedeemValue;
            propertyTile.IsMortgaged = false;
        }

        public void SellHouse(Guid playerID, GameState gameState, int tileIndex)
        {
            if (playerID != gameState.CurrentPlayer.ID)
            {
                _gameLog.Log($"Player [{playerID}] was stopped from going out of turn.");
                return;
            }
            if (tileIndex >= gameState.Tiles.Count() || tileIndex < 0)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' tried to sell a house on a property that doesn't exist! Is he trying to cheat? Get out of here Hugh!");
                return;
            }
            if (!(gameState.Tiles[tileIndex] is ColorPropertyTile colorPropertyTile))
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' can only sell a house on colored property tiles!");
                return;
            }
            if (colorPropertyTile.OwnerPlayerID != playerID)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot sell a house on this property because he/she does not own this property!");
                return;
            }
            if (colorPropertyTile.BuildingCount <= 0)
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot sell a house because there are no houses on this property!");
                return;
            }
            var sameColoredProperties = gameState.Tiles
                .Where(t => t is ColorPropertyTile)
                .Select(t => t as ColorPropertyTile)
                .Where(t => t.Color == colorPropertyTile.Color);
            if (sameColoredProperties.Any(p => Math.Abs(colorPropertyTile.BuildingCount - 1 - p.BuildingCount) > 1))
            {
                _gameLog.Log($"Player '{gameState.CurrentPlayer.Name}' cannot sell this property because buildings must be built evenly on properties of the same color.");
                return;
            }

            gameState.CurrentPlayer.Money += (int)Math.Round(colorPropertyTile.BuildingCost * 0.5);
            colorPropertyTile.BuildingCount--;
        }

        public void DeclareBankruptcy(Guid playerID, GameState gameState)
        {
            var player = gameState.Players.FirstOrDefault(p => p.ID == playerID);
            if (player == null)
            {
                _gameLog.Log($"Player [{playerID}] could not be found!");
                return;
            }

            var playerOwnedPropertyTiles = gameState.Tiles
                .Where(x => x is PropertyTile)
                .Select(x => x as PropertyTile)
                .Where(x => x.OwnerPlayerID == playerID)
                .ToList();

            foreach (var property in playerOwnedPropertyTiles)
            {
                property.OwnerPlayerID = null;
                property.IsMortgaged = false;
                if (property is ColorPropertyTile colorPropertyTile)
                {
                    colorPropertyTile.BuildingCount = 0;
                }
            }

            _gameLog.Log($"Player '{player.Name}' just declared bankruptcy!");

            var playerIdx = gameState.Players.IndexOf(player);
            var isCurrentPlayerTurn = playerID == gameState.CurrentPlayer.ID;
            var storedCurrentPlayerListIndex = gameState.CurrentPlayerListIndex;

            if (isCurrentPlayerTurn)
                SwitchToNextPlayer(gameState, playerIdx < gameState.Players.Count()-1);
            if (playerIdx <= storedCurrentPlayerListIndex)
                gameState.CurrentPlayerListIndex--;

            gameState.Players.Remove(player);
            if (!gameState.Players.Any())
                gameState.GameFinished = true;
        }

        public void OfferTrade(Guid playerA_ID, GameState gameState, Guid playerB_ID, List<int> tileIndexesA, List<int> tileIndexesB, int moneyAB)
        {
            var playerA = gameState.Players.FirstOrDefault(p => p.ID == playerA_ID);
            var playerB = gameState.Players.FirstOrDefault(p => p.ID == playerB_ID);
            if (playerB == null || playerA == null)
            {
                _gameLog.Log($"Cannot find both trade parties. Aborting trade.");
                return;
            }

            if (gameState.TradeOffers.Any(x => x.PlayerA.ID == playerA_ID && x.PlayerB.ID == playerB_ID))
            {
                _gameLog.Log($"Player '{playerA.Name}' is already offering a trade to '{playerB.Name}' and cannot send another trade at this time.");
                return;
            }

            var propertiesA = gameState.Tiles
                .Where(t => t is PropertyTile)
                .Where(t => tileIndexesA?.Contains(gameState.Tiles.IndexOf(t)) == true)
                .Select(t => t as PropertyTile)
                .Where(t => t.OwnerPlayerID == playerA_ID)
                .ToList();
            var propertiesB = gameState.Tiles
                .Where(t => t is PropertyTile)
                .Where(t => tileIndexesB?.Contains(gameState.Tiles.IndexOf(t)) == true)
                .Select(t => t as PropertyTile)
                .Where(t => t.OwnerPlayerID == playerB_ID)
                .ToList();

            if (moneyAB == 0 && (!propertiesA.Any() || !propertiesB.Any()))
            {
                _gameLog.Log($"Player '{playerA.Name}' offered an invalid trade! There must be an exchange of goods.");
                return;
            }
            if (moneyAB != 0 && !propertiesA.Any() && !propertiesB.Any())
            {
                _gameLog.Log($"Player '{playerA.Name}' offered an invalid trade! There must be an exchange of goods.");
                return;
            }
            if (moneyAB > 0 && propertiesA.Any() && !propertiesB.Any())
            {
                _gameLog.Log($"Player '{playerA.Name}' offered an invalid trade! Both players must receive goods.");
                return;
            }
            if (moneyAB < 0 && !propertiesA.Any() && propertiesB.Any())
            {
                _gameLog.Log($"Player '{playerA.Name}' offered an invalid trade! Both players must receive goods.");
                return;
            }

            var tradeOffer = new TradeOffer(playerA, playerB, propertiesA, propertiesB, moneyAB);
            gameState.TradeOffers.Add(tradeOffer);
        }

        public void AcceptTradeOffer(Guid playerB_ID, GameState gameState, Guid tradeOfferID)
        {
            var tradeOffer = gameState.TradeOffers.FirstOrDefault(x => x.ID == tradeOfferID);
            if (tradeOffer == null)
            {
                _gameLog.Log($"Cannot accept a trade that does not exist!");
                return;
            }
            if (tradeOffer.PlayerA == null || !gameState.Players.Any(p => p.ID == tradeOffer.PlayerA.ID))
            {
                _gameLog.Log($"Cannot accept this trade because the initiating player is no longer in the game.");
                return;
            }
            if (playerB_ID != tradeOffer.PlayerB.ID)
            {
                _gameLog.Log($"Cannot accept this trade because it doesn't involve him/her.");
                return;
            }

            var playerA = tradeOffer.PlayerA;
            var playerB = tradeOffer.PlayerB;

            playerA.Money -= tradeOffer.MoneyAB;
            playerB.Money += tradeOffer.MoneyAB;
            
            foreach(var property in tradeOffer.PlayerAProperties)
            {
                property.OwnerPlayerID = playerB.ID;
            }
            foreach (var property in tradeOffer.PlayerBProperties)
            {
                property.OwnerPlayerID = playerA.ID;
            }

            gameState.TradeOffers.Remove(tradeOffer);

            var tradeOffersWithSameProperties = gameState.TradeOffers
                .Where(t => t.PlayerAProperties.Any(p1a => tradeOffer.PlayerAProperties.Any(p2a => p2a.ID == p1a.ID))
                    || t.PlayerBProperties.Any(p1b => tradeOffer.PlayerBProperties.Any(p2b => p2b.ID == p1b.ID)));
            foreach(var trade in tradeOffersWithSameProperties)
            {
                gameState.TradeOffers.Remove(trade);
            }
        }

        public void RejectTradeOffer(Guid playerB_ID, GameState gameState, Guid tradeOfferID)
        {
            var tradeOffer = gameState.TradeOffers.FirstOrDefault(x => x.ID == tradeOfferID);
            if (tradeOffer == null)
            {
                _gameLog.Log($"Player '{playerB_ID}' {gameState.CurrentPlayer.Name} cannot accept a trade that does not exist!");
                return;
            }
            if (tradeOffer.PlayerA == null || !gameState.Players.Any(p => p.ID == tradeOffer.PlayerA.ID))
            {
                _gameLog.Log($"Player '{playerB_ID}' {gameState.CurrentPlayer.Name} cannot accept this trade because the initiating player is no longer in the game.");
                return;
            }
            if (playerB_ID != tradeOffer.PlayerB.ID)
            {
                _gameLog.Log($"Player '{playerB_ID}' {gameState.CurrentPlayer.Name} cannot accept this trade because it doesn't involve him/her.");
                return;
            }

            gameState.TradeOffers.Remove(tradeOffer);
        }

        private List<Tile> GetTiles()
        {
            var tiles = new List<Tile>()
            {
                new GoTile() {},
                new ColorPropertyTile()
                {
                    Name = "Mediterranean Avenue",
                    Cost = 60,
                    RentAmountPerBuilding = new int[] { 2, 10, 30, 90, 160, 250 },
                    Color = Constants.PropertyColor.Brown
                },
                new CommunityChestTile() {},
                new ColorPropertyTile()
                {
                    Name = "Baltic Avenue",
                    Cost = 60,
                    RentAmountPerBuilding = new int[] { 4, 20, 60, 180, 320, 450 },
                    Color = Constants.PropertyColor.Brown
                },
                new TaxTile()
                {
                    Name = "Income Tax",
                    Cost = 200
                },
                new RailroadPropertyTile()
                {
                    Name = "Reading Railroad",
                    Cost = 200,
                },
                new ColorPropertyTile()
                {
                    Name = "Oriental Avenue",
                    Cost = 100,
                    RentAmountPerBuilding = new int[] { 6, 30, 90, 270, 400, 550 },
                    Color = Constants.PropertyColor.LightBlue
                },
                new ChanceTile() {},
                new ColorPropertyTile()
                {
                    Name = "Vermont Avenue",
                    Cost = 100,
                    RentAmountPerBuilding = new int[] { 6, 30, 90, 270, 400, 550 },
                    Color = Constants.PropertyColor.LightBlue
                },
                new ColorPropertyTile()
                {
                    Name = "Connecticut Avenue",
                    Cost = 120,
                    RentAmountPerBuilding = new int[] { 8, 40, 100, 300, 450, 600 },
                    Color = Constants.PropertyColor.LightBlue
                },
                new JailTile() {},
                new ColorPropertyTile()
                {
                    Name = "St. Charles Place",
                    Cost = 140,
                    RentAmountPerBuilding = new int[] { 10, 50, 150, 450, 625, 750 },
                    Color = Constants.PropertyColor.Pink
                },
                new UtilityPropertyTile()
                {
                    Name = "Electric Company",
                    Cost = 150,
                },
                new ColorPropertyTile()
                {
                    Name = "States Avenue",
                    Cost = 140,
                    RentAmountPerBuilding = new int[] { 10, 50, 150, 450, 625, 750 },
                    Color = Constants.PropertyColor.Pink
                },
                new ColorPropertyTile()
                {
                    Name = "Virginia Avenue",
                    Cost = 160,
                    RentAmountPerBuilding = new int[] { 12, 60, 180, 500, 700, 900 },
                    Color = Constants.PropertyColor.Pink
                },
                new RailroadPropertyTile()
                {
                    Name = "Pennsylvania Railroad",
                    Cost = 200,
                },
                new ColorPropertyTile()
                {
                    Name = "St. James Place",
                    Cost = 180,
                    RentAmountPerBuilding = new int[] { 14, 70, 200, 550, 750, 950 },
                    Color = Constants.PropertyColor.Orange
                },
                new CommunityChestTile() {},
                new ColorPropertyTile()
                {
                    Name = "Tennessee Avenue",
                    Cost = 180,
                    RentAmountPerBuilding = new int[] { 14, 70, 200, 550, 750, 950 },
                    Color = Constants.PropertyColor.Orange
                },
                new ColorPropertyTile()
                {
                    Name = "New York Avenue",
                    Cost = 200,
                    RentAmountPerBuilding = new int[] { 16, 80, 220, 600, 800, 1000 },
                    Color = Constants.PropertyColor.Orange
                },
                new FreeParkingTile() {},
                new ColorPropertyTile()
                {
                    Name = "Kentucky Avenue",
                    Cost = 220,
                    RentAmountPerBuilding = new int[] { 18, 90, 250, 700, 875, 1050 },
                    Color = Constants.PropertyColor.Red
                },
                new ChanceTile() {},
                new ColorPropertyTile()
                {
                    Name = "Indiana Avenue",
                    Cost = 220,
                    RentAmountPerBuilding = new int[] { 18, 90, 250, 700, 875, 1050 },
                    Color = Constants.PropertyColor.Red
                },
                new ColorPropertyTile()
                {
                    Name = "Illinois Avenue",
                    Cost = 240,
                    RentAmountPerBuilding = new int[] { 20, 100, 300, 750, 925, 1100 },
                    Color = Constants.PropertyColor.Red
                },
                new RailroadPropertyTile()
                {
                    Name = "B. & O. Railroad",
                    Cost = 200,
                },
                new ColorPropertyTile()
                {
                    Name = "Atlantic Avenue",
                    Cost = 260,
                    RentAmountPerBuilding = new int[] { 22, 110, 330, 800, 975, 1150 },
                    Color = Constants.PropertyColor.Yellow
                },
                new ColorPropertyTile()
                {
                    Name = "Ventnor Avenue",
                    Cost = 260,
                    RentAmountPerBuilding = new int[] { 22, 110, 330, 800, 975, 1150 },
                    Color = Constants.PropertyColor.Yellow
                },
                new UtilityPropertyTile()
                {
                    Name = "Water Works",
                    Cost = 150,
                },
                new ColorPropertyTile()
                {
                    Name = "Marven Gardens",
                    Cost = 280,
                    RentAmountPerBuilding = new int[] { 24, 120, 360, 850, 1025, 1200 },
                    Color = Constants.PropertyColor.Yellow
                },
                new GoToJail() {},
                new ColorPropertyTile()
                {
                    Name = "Pacific Avenue",
                    Cost = 300,
                    RentAmountPerBuilding = new int[] { 26, 130, 390, 900, 1100, 1275 },
                    Color = Constants.PropertyColor.Green
                },
                new ColorPropertyTile()
                {
                    Name = "North Carolina Avenue",
                    Cost = 300,
                    RentAmountPerBuilding = new int[] { 26, 130, 390, 900, 1100, 1275 },
                    Color = Constants.PropertyColor.Green
                },
                new CommunityChestTile() {},
                new ColorPropertyTile()
                {
                    Name = "Pennsylvania Avenue",
                    Cost = 320,
                    RentAmountPerBuilding = new int[] { 28, 150, 450, 1000, 1200, 1400 },
                    Color = Constants.PropertyColor.Green
                },
                new RailroadPropertyTile()
                {
                    Name = "Shortline",
                    Cost = 200,
                },
                new ChanceTile() {},
                new ColorPropertyTile()
                {
                    Name = "Park Place",
                    Cost = 350,
                    RentAmountPerBuilding = new int[] { 35, 175, 500, 1100, 1300, 1500 },
                    Color = Constants.PropertyColor.DarkBlue
                },
                new TaxTile()
                {
                    Cost = 100,
                    Name = "Luxury Tax"
                },
                new ColorPropertyTile()
                {
                    Name = "Boardwalk",
                    Cost = 400,
                    RentAmountPerBuilding = new int[] { 50, 200, 600, 1400, 1700, 2000 },
                    Color = Constants.PropertyColor.DarkBlue
                }
            };

            return tiles;
        }
    }
}