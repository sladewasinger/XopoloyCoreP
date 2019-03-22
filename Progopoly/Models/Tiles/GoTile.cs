using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static Progopoly.Models.Constants;

namespace Progopoly.Models.Tiles
{
    public class GoTile : Tile
    {
        public override string Name
        {
            get
            {
                return "GO";
            }
        }

        public override TileType Type
        {
            get
            {
                return TileType.Go;
            }
        }

        public override void LandedOnAction(GameState gameState, IGameLog gameLog)
        {
        }
    }
}