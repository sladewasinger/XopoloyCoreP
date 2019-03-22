using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Progopoly.Models.Constants;

namespace Progopoly.Models.Tiles
{
    public class RailroadPropertyTile : PropertyTile
    {
        public override TileType Type
        {
            get
            {
                return TileType.Railroad;
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
                var ownedRailroads = gameState.Tiles
                    .Where(x => x is RailroadPropertyTile)
                    .Select(x => x as RailroadPropertyTile)
                    .Where(x => x.OwnerPlayerID == OwnerPlayerID);

                var cost = Constants.SINGLE_RAILROAD_FEE * (int)Math.Pow(2, ownedRailroads.Count() - 1);
                var owner = gameState.Players.First(x => x.ID == OwnerPlayerID);

                gameState.CurrentPlayer.Money -= cost;
                owner.Money += cost;

                gameLog.Log($"Player {gameState.CurrentPlayer.Name} just paid {owner.Name} ${cost} to ride the train!");
            }
        }
    }
}
