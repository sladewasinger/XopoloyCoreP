using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static Progopoly.Models.Constants;

namespace Progopoly.Models.Tiles
{
    public class CommunityChestTile : DrawCardTile
    {
        public override string Name
        {
            get
            {
                return "Community Chest";
            }
        }

        public override TileType Type
        {
            get
            {
                return TileType.CommunityChest;
            }
        }

        public override void LandedOnAction(GameState gameState, IGameLog gameLog)
        {
            gameState.CommunityChestDeck.DrawCard(gameState);
            OnGameStateUpdated(EventArgs.Empty);
        }
    }
}