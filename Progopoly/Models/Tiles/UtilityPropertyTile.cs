using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Progopoly.Models.Constants;

namespace Progopoly.Models.Tiles
{
    public class UtilityPropertyTile : PropertyTile
    {
        public override TileType Type
        {
            get
            {
                return TileType.Utilities;
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
                var ownedUtilities = gameState.Tiles
                    .Where(x => x is UtilityPropertyTile)
                    .Select(x => x as UtilityPropertyTile)
                    .Where(x => x.OwnerPlayerID == OwnerPlayerID);

                var multiplier = 7;
                if (ownedUtilities.Count() > 1)
                    multiplier = 14;

                var cost = multiplier * gameState.CurrentPlayer.CurrentDiceRoll.Dice.Sum();
                var owner = gameState.Players.First(x => x.ID == OwnerPlayerID);

                gameState.CurrentPlayer.Money -= cost;
                owner.Money += cost;

                gameLog.Log($"Player {gameState.CurrentPlayer.Name} just paid {owner.Name} ${cost} in utilities!");
            }
            OnGameStateUpdated(EventArgs.Empty);
        }
    }
}
