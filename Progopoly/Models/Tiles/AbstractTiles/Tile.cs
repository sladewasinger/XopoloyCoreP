using Newtonsoft.Json;
using System;
using static Progopoly.Models.Constants;

namespace Progopoly.Models.Tiles
{
    public abstract class Tile
    {
        public virtual string Name { get; set; }
        public virtual TileType Type { get; set; }
        public int ID { get; set; }
        public abstract void LandedOnAction(GameState gameState, IGameLog gameLog);
        [JsonIgnore]
        public Action<EventArgs> OnGameStateUpdated;
    }
}