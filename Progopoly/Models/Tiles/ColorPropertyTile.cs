using System;
using System.Linq;
using static Progopoly.Models.Constants;

namespace Progopoly.Models.Tiles
{
    public class ColorPropertyTile : PropertyTile
    {
        public PropertyColor Color { get; set; }
        public int BuildingCount { get; set; }
        public int Rent
        {
            get
            {
                return RentAmountPerBuilding[BuildingCount];
            }
        }
        public int[] RentAmountPerBuilding { get; set; } = new int[MAX_BUILDINGS_ON_PROPERTY + 1];
        public int BuildingCost
        {
            get
            {
                return PropertyColorBuildingCost[Color];
            }
        }

        public override TileType Type
        {
            get
            {
                return TileType.ColorProperty;
            }
        }

        public override void LandedOnAction(GameState gameState, IGameLog gameLog)
        {
            if (OwnerPlayerID == null)
            {
                if (gameState.Players.Any(p => p.Money > 0))
                    gameState.WaitForBuyOrAuctionStart = true;
            }
            else if (gameState.CurrentPlayer.ID != OwnerPlayerID && !IsMortgaged)
            {
                var sameColoredTiles = gameState.Tiles
                    .Where(x => x is ColorPropertyTile)
                    .Select(x => x as ColorPropertyTile)
                    .Where(x => x.Color == Color);
                var owner = gameState.Players.First(x => x.ID == OwnerPlayerID);

                ChargePlayer(gameState, owner);

                if (BuildingCount == 0 && sameColoredTiles.Where(x => x.OwnerPlayerID == OwnerPlayerID).Count() == sameColoredTiles.Count())
                {
                    ChargePlayer(gameState, owner); //Charge  double.
                }

                gameLog.Log($"Player {gameState.CurrentPlayer.Name} just paid {owner.Name} ${Rent} in rent!");
            }
            OnGameStateUpdated(EventArgs.Empty);
        }

        private void ChargePlayer(GameState gameState, Player owner)
        {
            gameState.CurrentPlayer.Money -= Rent;
            owner.Money += Rent;
        }
    }
}