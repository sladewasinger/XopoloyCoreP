using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Progopoly.Models.Constants;

namespace Progopoly.Models.Tiles
{
    public class FreeParkingTile : Tile
    {
        public override string Name
        {
            get
            {
                return "Free Parking";
            }
        }

        public override TileType Type
        {
            get
            {
                return TileType.FreeParking;
            }
        }

        public override void LandedOnAction(GameState gameState, IGameLog gameLog)
        {
            gameLog.Log($"Player {gameState.CurrentPlayer.Name} decided to park his car in a random parking lot and sit there for no discernible reason.");
        }
    }
}
