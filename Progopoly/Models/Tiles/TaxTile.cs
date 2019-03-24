using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static Progopoly.Models.Constants;

namespace Progopoly.Models.Tiles
{
    public class TaxTile : Tile
    {
        public override TileType Type { get; set; } = TileType.Tax;

        public int Cost { get; set; }

        public override void LandedOnAction(GameState gameState, IGameLog gameLog)
        {
            gameState.CurrentPlayer.Money -= Cost;
            gameLog.Log($"Player {gameState.CurrentPlayer.Name} just paid ${Cost} in taxes!");
            OnGameStateUpdated(EventArgs.Empty);
        }
    }
}