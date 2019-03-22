using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Progopoly.Models.Constants;

namespace Progopoly.Models.Tiles
{
    public class GoToJail : Tile
    {
        public override string Name
        {
            get
            {
                return "Go To Jail";
            }
        }

        public override TileType Type
        {
            get
            {
                return TileType.GoToJail;
            }
        }

        public override void LandedOnAction(GameState gameState, IGameLog gameLog)
        {
            gameState.CurrentPlayer.BoardPosition = gameState.Tiles.IndexOf(gameState.Tiles.First(x => x is JailTile));
            gameState.CurrentPlayer.WasDirectMovement = true;
            gameState.CurrentPlayer.IsInJail = true;
            gameLog.Log($"Player {gameState.CurrentPlayer.Name} got sent to jail!");
        }
    }
}
