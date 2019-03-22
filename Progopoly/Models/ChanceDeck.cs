using Progopoly.Models.Tiles;
using System;
using System.Linq;

namespace Progopoly.Models
{
    public class ChanceDeck : Deck
    {
        public ChanceDeck(IGameLog gameLog) : base(gameLog) {}

        public string CurrentPlayerCardText { get; set; }

        [Card]
        public void AdvanceToGo(GameState gameState)
        {
            CurrentPlayerCardText = "Advance to \"Go\". (Collect $200)";
            gameState.CurrentPlayer.BoardPosition = 0;
            gameState.CurrentTile.LandedOnAction(gameState, _gameLog);
            gameState.CurrentPlayer.Money += 200;

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void AdvanceToIllinoisAve(GameState gameState)
        {
            CurrentPlayerCardText = "Advance to Illinois Ave. If you pass Go, collect $200.";

            var targetTileIndex = gameState.Tiles.IndexOf(
                gameState.Tiles.First(x => x is ColorPropertyTile && ((ColorPropertyTile)x).Name.Contains("Illinois Avenue")));
            if (gameState.CurrentPlayer.BoardPosition > targetTileIndex)
            {
                gameState.CurrentPlayer.Money += 200;
            }
            gameState.CurrentPlayer.BoardPosition = targetTileIndex;
            gameState.CurrentTile.LandedOnAction(gameState, _gameLog);

            _gameLog.Log(CurrentPlayerCardText);
        }


        [Card]
        public void AdvanceToStCharlesPlace(GameState gameState)
        {
            CurrentPlayerCardText = "Advance to St. Charles Place. If you pass Go, collect $200.";

            var targetTileIndex = gameState.Tiles.IndexOf(
                gameState.Tiles.First(x => x is ColorPropertyTile && ((ColorPropertyTile)x).Name.Contains("St. Charles Place")));
            if (gameState.CurrentPlayer.BoardPosition > targetTileIndex)
            {
                gameState.CurrentPlayer.Money += 200;
            }
            gameState.CurrentPlayer.BoardPosition = targetTileIndex;
            gameState.CurrentTile.LandedOnAction(gameState, _gameLog);

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void AdvanceToNearestUtility(GameState gameState)
        {
            CurrentPlayerCardText = "Advance token to nearest Utility.";

            var utilitiesIndexes = gameState.Tiles.Where(x => x is UtilityPropertyTile).Select(x => gameState.Tiles.IndexOf(x));
            var closestUtilityIndex = utilitiesIndexes.OrderBy(ui => Math.Abs(gameState.CurrentPlayer.BoardPosition - ui)).First();

            gameState.CurrentPlayer.BoardPosition = closestUtilityIndex;
            gameState.CurrentPlayer.WasDirectMovement = true;
            gameState.CurrentTile.LandedOnAction(gameState, _gameLog);

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void AdvanceToNearestRailroad(GameState gameState)
        {
            CurrentPlayerCardText = "Advance token to nearest Railroad.";

            var railroadIndexes = gameState.Tiles.Where(x => x is RailroadPropertyTile).Select(x => gameState.Tiles.IndexOf(x));
            var closestRailroadIndex = railroadIndexes.OrderBy(ui => Math.Abs(gameState.CurrentPlayer.BoardPosition - ui)).First();

            gameState.CurrentPlayer.BoardPosition = closestRailroadIndex;
            gameState.CurrentPlayer.WasDirectMovement = true;
            gameState.CurrentTile.LandedOnAction(gameState, _gameLog);

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void BankDividend(GameState gameState)
        {
            CurrentPlayerCardText = "Bank pays you dividend of $50.";

            gameState.CurrentPlayer.Money += 50;

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void GetOutOfJailFreeCard(GameState gameState)
        {
            CurrentPlayerCardText = "Get Out of Jail Free. – This card may be kept until needed or sold/traded.";
            gameState.CurrentPlayer.HasGetOutOfJailFreeCard = true;

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void GoBackThreeSpaces(GameState gameState)
        {
            CurrentPlayerCardText = "Go Back Three Spaces.";

            gameState.CurrentPlayer.BoardPosition -= 3; //I am assuming this will never be negative since chance tiles are 3+ out from GO.
            gameState.CurrentPlayer.WasDirectMovement = true;
            gameState.CurrentTile.LandedOnAction(gameState, _gameLog);

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void GoToJail(GameState gameState)
        {
            CurrentPlayerCardText = "Go to Jail. Go directly to jail. Do not pass Go, Do not collect $200.";
            gameState.CurrentPlayer.BoardPosition = gameState.Tiles.IndexOf(gameState.Tiles.First(x => x is JailTile) as JailTile);
            gameState.CurrentPlayer.WasDirectMovement = true;
            gameState.CurrentPlayer.IsInJail = true;

            _gameLog.Log(CurrentPlayerCardText);
        }


        [Card]
        public void GeneralRepairs(GameState gameState)
        {
            CurrentPlayerCardText = "Make general repairs on all your property: For each house pay $25, For each hotel $100.";

            var ownedColorPropertyTiles = gameState.Tiles
                .Where(x => x is ColorPropertyTile)
                .Select(x => x as ColorPropertyTile)
                .Where(x => x.OwnerPlayerID == gameState.CurrentPlayer.ID);

            foreach (var prop in ownedColorPropertyTiles)
            {
                if (prop.BuildingCount == Constants.MAX_BUILDINGS_ON_PROPERTY)
                {
                    gameState.CurrentPlayer.Money -= 100;
                }
                else if (prop.BuildingCount > 0)
                {
                    gameState.CurrentPlayer.Money -= 25;
                }
            }

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void PayPoorTax(GameState gameState)
        {
            CurrentPlayerCardText = "Pay poor tax of $15.";

            gameState.CurrentPlayer.Money -= 15;

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void AdvanceToReadingRailroad(GameState gameState)
        {
            CurrentPlayerCardText = "Take a trip to Reading Railroad. If you pass Go, collect $200.";

            var targetTileIndex = gameState.Tiles.IndexOf(
                gameState.Tiles.First(x => x is RailroadPropertyTile && ((RailroadPropertyTile)x).Name.Contains("Reading Railroad")));
            if (gameState.CurrentPlayer.BoardPosition > targetTileIndex)
            {
                gameState.CurrentPlayer.Money += 200;
            }
            gameState.CurrentPlayer.BoardPosition = targetTileIndex;
            gameState.CurrentTile.LandedOnAction(gameState, _gameLog);

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void TakeTripOnBoardwalk(GameState gameState)
        {
            CurrentPlayerCardText = "Take a walk on the Boardwalk. Advance token to Boardwalk.";

            var targetTileIndex = gameState.Tiles.IndexOf(
                gameState.Tiles.First(x => x is ColorPropertyTile && ((ColorPropertyTile)x).Name.Contains("Boardwalk")));

            gameState.CurrentPlayer.BoardPosition = targetTileIndex;
            gameState.CurrentTile.LandedOnAction(gameState, _gameLog);

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void Chairman(GameState gameState)
        {
            CurrentPlayerCardText = "You have been elected Chairman of the Board. Pay each player $50.";

            foreach (var player in gameState.Players.Where(x => x.ID != gameState.CurrentPlayer.ID))
            {
                gameState.CurrentPlayer.Money -= 50;
                player.Money += 50;
            }

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void BuildingLoanMatures(GameState gameState)
        {
            CurrentPlayerCardText = "Your building loan matures. Receive $150.";

            gameState.CurrentPlayer.Money += 150;

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void WonCrosswordCompetition(GameState gameState)
        {
            CurrentPlayerCardText = "You have won a crossword competition. Collect $100.";

            gameState.CurrentPlayer.Money += 100;

            _gameLog.Log(CurrentPlayerCardText);
        }
    }
}
