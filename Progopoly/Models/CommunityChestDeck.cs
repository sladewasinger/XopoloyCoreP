using Progopoly.Models.Tiles;
using System.Linq;

namespace Progopoly.Models
{
    public class CommunityChestDeck : Deck
    {
        public CommunityChestDeck(IGameLog gameLog) : base(gameLog) {}

        public string CurrentPlayerCardText { get; set; }

        [Card]
        public void AdvanceToGo(GameState gameState)
        {
            CurrentPlayerCardText = "Advance to \"Go\". (Collect $200)";
            gameState.CurrentPlayer.BoardPosition = 0;
            gameState.CurrentPlayer.Money += 200;

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void BankError(GameState gameState)
        {
            CurrentPlayerCardText = "Bank error in your favor. Collect $200.";
            gameState.CurrentPlayer.Money += 200;

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void DoctorsFee(GameState gameState)
        {
            CurrentPlayerCardText = "Doctor's fees. Pay $50";
            gameState.CurrentPlayer.Money -= 50;

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void StockSale(GameState gameState)
        {
            CurrentPlayerCardText = "From sale of stock you get $50.";
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
        public void GoToJail(GameState gameState)
        {
            CurrentPlayerCardText = "Go to Jail. Go directly to jail. Do not pass Go, Do not collect $200.";
            gameState.CurrentPlayer.BoardPosition = gameState.Tiles.IndexOf(gameState.Tiles.First(x => x is JailTile) as JailTile);
            gameState.CurrentPlayer.WasDirectMovement = true;
            gameState.CurrentPlayer.IsInJail = true;

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void GrandOperaNight(GameState gameState)
        {
            CurrentPlayerCardText = "Grand Opera Night. Collect $50 from every player for opening night seats.";

            foreach (var player in gameState.Players.Where(x => x.ID != gameState.CurrentPlayer.ID))
            {
                player.Money -= 50;
                gameState.CurrentPlayer.Money += 50;
            }

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void Holiday(GameState gameState)
        {
            CurrentPlayerCardText = "Holiday Fund matures. Receive $100";
            gameState.CurrentPlayer.Money += 100;

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void IncomeTaxRefund(GameState gameState)
        {
            CurrentPlayerCardText = "Income tax refund. Collect $20.";
            gameState.CurrentPlayer.Money += 20;

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void Birthday(GameState gameState)
        {
            CurrentPlayerCardText = "It is your birthday. Collect $10 from every player.";

            foreach (var player in gameState.Players.Where(x => x.ID != gameState.CurrentPlayer.ID))
            {
                player.Money -= 10;
                gameState.CurrentPlayer.Money += 10;
            }

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void LifeInsurance(GameState gameState)
        {
            CurrentPlayerCardText = "Life insurance matures – Collect $100";
            gameState.CurrentPlayer.Money += 100;

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void HospitalFees(GameState gameState)
        {
            CurrentPlayerCardText = "Hospital Fees. Pay $50.";
            gameState.CurrentPlayer.Money -= 50;

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void SchoolFees(GameState gameState)
        {
            CurrentPlayerCardText = "School fees. Pay $50.";
            gameState.CurrentPlayer.Money -= 50;

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void ReceiveConsultancyFee(GameState gameState)
        {
            CurrentPlayerCardText = "Receive $25 consultancy fee.";
            gameState.CurrentPlayer.Money += 25;

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void StreetRepairs(GameState gameState)
        {
            CurrentPlayerCardText = "You are assessed for street repairs: Pay $40 per house and $115 per hotel you own.";

            var ownedColorPropertyTiles = gameState.Tiles
                .Where(x => x is ColorPropertyTile)
                .Select(x => x as ColorPropertyTile)
                .Where(x => x.OwnerPlayerID == gameState.CurrentPlayer.ID);

            foreach (var prop in ownedColorPropertyTiles)
            {
                if (prop.BuildingCount == Constants.MAX_BUILDINGS_ON_PROPERTY)
                {
                    gameState.CurrentPlayer.Money -= 115;
                }
                else if (prop.BuildingCount > 0)
                {
                    gameState.CurrentPlayer.Money -= 40;
                }
            }

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void BeautyContest(GameState gameState)
        {
            CurrentPlayerCardText = "You have won second prize in a beauty contest. Collect $10.";
            gameState.CurrentPlayer.Money += 10;

            _gameLog.Log(CurrentPlayerCardText);
        }

        [Card]
        public void InheritMoney(GameState gameState)
        {
            CurrentPlayerCardText = "You inherit $100.";
            gameState.CurrentPlayer.Money += 100;

            _gameLog.Log(CurrentPlayerCardText);
        }
    }
}
