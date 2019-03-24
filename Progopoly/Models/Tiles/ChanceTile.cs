using System;
using static Progopoly.Models.Constants;

namespace Progopoly.Models.Tiles
{
    public class ChanceTile : DrawCardTile
    {
        public override string Name
        {
            get
            {
                return "Chance";
            }
        }

        public override TileType Type
        {
            get
            {
                return TileType.Chance;
            }
        }

        public override void LandedOnAction(GameState gameState, IGameLog gameLog)
        {
            gameState.ChanceDeck.DrawCard(gameState);
            OnGameStateUpdated(EventArgs.Empty);
        }
    }
}