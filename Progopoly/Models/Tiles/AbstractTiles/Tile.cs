using System;
using static Progopoly.Models.Constants;

namespace Progopoly.Models.Tiles
{
    public abstract class Tile
    {
        public virtual string Name { get; set; }
        public virtual TileType Type { get; set; }
        public abstract void LandedOnAction(GameState gameState, IGameLog gameLog);
        public int ID { get; set; }
    }
}