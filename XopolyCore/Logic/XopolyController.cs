using GameModels = Progopoly.Models;
using System.Collections.Generic;
using System;
using System.Timers;
using Xopoly.Models;
using Progopoly.Logic;
using System.Linq;
using Progopoly.Models.Tiles;
using Microsoft.AspNetCore.SignalR;
using XopolyCore.Hubs;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Xopoly.Logic
{
    public class XopolyController
    {
        private Engine _engine;
        private List<GameModels.Player> _players = new List<GameModels.Player>();
        public GameModels.GameState GameState { get; private set; }
        public GameModels.IGameLog GameLog { get; set; }
        public Timer PlayerTimeoutTimer { get; set; }
        public bool TimerPaused { get; set; }
        public Action<Lobby, IHubContext<LobbyHub>> UpdateClientsGameState { get; set; }
        public Lobby ParentLobby { get; set; }
        public IHubContext<LobbyHub> LobbyHubContext { get; set; }
        public Dictionary<string, Action<object[]>> GameMethods;
        public object _gameMethodLock = new object();

        private class GameMethod : Attribute { }

        public XopolyController(Action<Lobby, IHubContext<LobbyHub>> updateClientsCallback, Lobby lobby, IHubContext<LobbyHub> hubContext)
        {
            ParentLobby = lobby;
            LobbyHubContext = hubContext;

            GameLog = new ServerGameLog();
            _engine = new Engine(new DiceRoller(), GameLog);

            PlayerTimeoutTimer = new Timer(1000)
            {
                AutoReset = true,
                Enabled = true
            };
            PlayerTimeoutTimer.Elapsed += (source, e) => PlayerTimerTick(source, e);
            UpdateClientsGameState = updateClientsCallback;

            GameMethods = GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => Attribute.GetCustomAttribute(m, typeof(GameMethod)) != null)
                .Select(m => new KeyValuePair<string, Action<object[]>>(m.Name, (Action<object[]>)Delegate.CreateDelegate(typeof(Action<object[]>), this, m)))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private void UnPauseAuction(object sender, EventArgs e)
        {
            if (TimerPaused && (GameState.Auction == null || !GameState.AuctionInProgress))
            {
                TimerPaused = false;
            }
        }

        private void AutoAuction(object source, ElapsedEventArgs e, GameModels.AuctionParticipant auctionPlayer)
        {
            if (auctionPlayer == null || auctionPlayer.AuctionBet != null)
                return;

            GameLog.Log($"*YAWN* You're taking too long to bet, '{auctionPlayer.Name}'! DUMB-AI 2.0 is now making an educated bet for you...");
            BetOnAuction(auctionPlayer.ID, Math.Min(auctionPlayer.Money, (GameState.CurrentTile as PropertyTile)?.MortgageValue ?? 0));
            UpdateClientsGameState(ParentLobby, LobbyHubContext);
        }

        public void CallMethod([CallerMemberName] string methodName = "", params object[] methodArgs)
        {
            lock (_gameMethodLock)
            {
                GameMethods[methodName].Invoke(methodArgs);
            }
        }

        private void SetTimeoutsForAuctionParticipants()
        {
            if (!GameState.AuctionInProgress || GameState.Auction == null)
                return;

            GameLog.Log($"You have 45 seconds to place your bets, or I'll bet for you!");
            foreach (var player in GameState.Auction.AuctionParticipants)
            {
                var timer = new Timer(45 * 1000)
                {
                    AutoReset = false,
                    Enabled = true
                };
                timer.Elapsed += (source, e) => AutoAuction(source, e, player);
                timer.Start();
            }
            UpdateClientsGameState(ParentLobby, LobbyHubContext);
        }

        private void PlayerTimerTick(object source, ElapsedEventArgs e)
        {
            if (GameState.GameFinished || GameState.CurrentPlayer == null)
            {
                PlayerTimeoutTimer.AutoReset = false;
                PlayerTimeoutTimer.Stop();
                return;
            }
            var cpID = GameState.CurrentPlayer.ID;

            if (!TimerPaused)
            {
                ++GameState.CurrentPlayer.CurrentTurnElapsedSeconds;
            }

            if (GameState.CurrentPlayer.CurrentTurnElapsedSeconds < GameState.TurnTimeoutSeconds)
            {
                UpdateClientsGameState(ParentLobby, LobbyHubContext);
                return;
            }

            GameState.CurrentPlayer.CurrentTurnElapsedSeconds = 0;
            GameLog.Log("*YAWN* Somebody is taking too long. DUMB-AI 2.0 now has control for this turn...");

            //Play for the player:
            if (!GameState.WaitForBuyOrAuctionStart)
                RollDice(cpID);
            if (GameState.WaitForBuyOrAuctionStart)
            {
                StartAuctionOnProperty(cpID);
                BetOnAuction(cpID, Math.Min(GameState.CurrentPlayer.Money, (GameState.CurrentTile as PropertyTile)?.MortgageValue ?? 0));
            }

            if (GameState.CurrentPlayer.CurrentDiceRoll == null || GameState.CurrentPlayer.CurrentDiceRoll.IsDouble)
            {
                RollDice(cpID);
                RollDice(cpID);
            }

            if (GameState.CurrentPlayer.Money < 0)
            {
                var ownedProperties = GameState.Tiles
                    .Where(x => x is GameModels.Tiles.PropertyTile)
                    .Select(x => x as GameModels.Tiles.PropertyTile)
                    .Where(x => x.OwnerPlayerID == cpID)
                    .Where(x => !x.IsMortgaged);

                var liquidatedAssetsValue = ownedProperties.Sum(prop =>
                {
                    var tempValue = prop.MortgageValue;

                    if (prop is GameModels.Tiles.ColorPropertyTile colorProperty)
                    {
                        tempValue += colorProperty.BuildingCount * (int)Math.Round(colorProperty.BuildingCost * 0.5);
                    }

                    return tempValue;
                });

                if (liquidatedAssetsValue + GameState.CurrentPlayer.Money >= 0)
                {
                    GameLog.Log($"Good news! Even though you're in the red, AND you timed out (BM, btw) I'll still go ahead and sell your assets for you.");
                    foreach (var prop in ownedProperties.OrderBy(x => (x as GameModels.Tiles.ColorPropertyTile)?.BuildingCount))
                    {
                        if (GameState.CurrentPlayer.Money >= 0)
                            break;
                        if (prop is GameModels.Tiles.ColorPropertyTile colorProperty && colorProperty.BuildingCount > 0)
                        {
                            var similarColoredProperties = ownedProperties
                                .Where(x => x is GameModels.Tiles.ColorPropertyTile)
                                .Select(x => x as GameModels.Tiles.ColorPropertyTile)
                                .Where(x => x.Color == colorProperty.Color);
                            for (int i = 0; i < 16
                                && similarColoredProperties.Sum(x => x.BuildingCount) > 0
                                && GameState.CurrentPlayer.Money < 0; i++)
                            {
                                var tileID = similarColoredProperties.OrderByDescending(x => x.BuildingCount).First(); //auto pick the largest amount of houses
                                SellHouse(cpID, GameState.Tiles.IndexOf(tileID));
                            }
                        }
                        else
                        {
                            MortgageProperty(cpID, GameState.Tiles.IndexOf(prop));
                        }
                    }
                }
                else
                {
                    GameLog.Log($"Sorry '{GameState.CurrentPlayer.Name}'! I did the calculations and you don't have enough assets to dig yourself out of this one.");
                    GameLog.Log($"SEE YOU LATER NERD!");
                    DeclareBankruptcy(cpID);
                }
            }

            EndTurn(cpID);

            UpdateClientsGameState(ParentLobby, LobbyHubContext);
        }

        private void SendGameStateUpdateToClients(object sender, EventArgs e)
        {
            UpdateClientsGameState(ParentLobby, LobbyHubContext);
        }

        public void StartGame()
        {
            _engine.GameStateUpdated += SendGameStateUpdateToClients;
            _engine.GameStateUpdated += UnPauseAuction;
            GameState = _engine.CreateInitialGameState(_players);          

            PlayerTimeoutTimer.Enabled = true;
            PlayerTimeoutTimer.Start();
        }

        public void SetupPlayers(List<Player> players)
        {
            foreach (var player in players)
            {
                var gamePlayer = _engine.CreateNewPlayer(player.Username);
                player.GameID = gamePlayer.ID;
                _players.Add(gamePlayer);
            }
        }

        [GameMethod]
        public void RollDice(params object[] args)
        {
            Guid playerGameID = (Guid)args[0];
            _engine.RollDiceAndMovePlayer(playerGameID, GameState);
        }

        public void EndTurn(Guid playerGameID)
        {
            GameState.CurrentPlayer.CurrentTurnElapsedSeconds = 0;
            _engine.FinishPlayerTurn(playerGameID, GameState);
        }

        public void BuyProperty(Guid playerGameID)
        {
            _engine.BuyProperty(playerGameID, GameState);
        }

        public void StartAuctionOnProperty(Guid playerGameID)
        {
            TimerPaused = true;
            _engine.StartAuctionOnProperty(playerGameID, GameState);
            SetTimeoutsForAuctionParticipants();
        }

        public void BetOnAuction(Guid playerGameID, int amount)
        {
            _engine.BetOnAuction(playerGameID, GameState, amount);
        }

        public void BuyHouse(Guid playerGameID, int tileIndex)
        {
            _engine.BuyAndBuildHouse(playerGameID, GameState, tileIndex);
        }

        public void InstantMonopoly(Guid playerGameID)
        {
            _engine.InstantMonopoly(playerGameID, GameState);
        }

        public void DeclareBankruptcy(Guid playerGameID)
        {
            GameState.CurrentPlayer.CurrentTurnElapsedSeconds = 0;
            _engine.DeclareBankruptcy(playerGameID, GameState);
        }

        public void BuyOutOfJail(Guid playerGameID)
        {
            _engine.BuyOutOfJail(playerGameID, GameState);
        }

        public void UseGetOutOfJailFreeCard(Guid playerGameID)
        {
            _engine.UseGetOutOfJailFreeCard(playerGameID, GameState);
        }

        public void MortgageProperty(Guid playerGameID, int tileIndex)
        {
            _engine.MortgageProperty(playerGameID, GameState, tileIndex);
        }

        public void RedeemProperty(Guid playerGameID, int tileIndex)
        {
            _engine.RedeemProperty(playerGameID, GameState, tileIndex);
        }

        public void SellHouse(Guid playerGameID, int tileIndex)
        {
            _engine.SellHouse(playerGameID, GameState, tileIndex);
        }

        public void OfferTrade(Guid playerGameID, Guid targetPlayerID, List<int> tileIndexesA, List<int> tileIndexesB, int moneyA, int moneyB)
        {
            _engine.OfferTrade(playerGameID, GameState, targetPlayerID, tileIndexesA, tileIndexesB, moneyA - moneyB);
        }

        public void AcceptTrade(Guid playerGameID, Guid tradeOfferID)
        {
            _engine.AcceptTradeOffer(playerGameID, GameState, tradeOfferID);
        }

        public void RejectTrade(Guid playerGameID, Guid tradeOfferID)
        {
            _engine.RejectTradeOffer(playerGameID, GameState, tradeOfferID);
        }
    }
}