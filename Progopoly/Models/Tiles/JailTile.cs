using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static Progopoly.Models.Constants;

namespace Progopoly.Models.Tiles
{
    public class JailTile : Tile
    {
        public override string Name
        {
            get
            {
                return "Jail";
            }
        }

        public override TileType Type
        {
            get
            {
                return TileType.Jail;
            }
        }

        public override void LandedOnAction(GameState gameState, IGameLog gameLog)
        {
            gameLog.Log($"Player {gameState.CurrentPlayer.Name} is just visiting jail.");
        }
    }
}