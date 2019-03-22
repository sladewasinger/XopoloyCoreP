using Progopoly.Models.Tiles;
using System;
using System.Collections.Generic;

namespace Progopoly.Models
{
    public class TradeOffer
    {
        public Guid ID { get; set; }
        public Player PlayerA { get; set; }
        public Player PlayerB { get; set; }
        public List<PropertyTile> PlayerAProperties { get; set; }
        public List<PropertyTile> PlayerBProperties { get; set; }
        public int MoneyAB { get; set; }

        public TradeOffer(Player playerA, Player playerB, List<PropertyTile> playerAProperties, List<PropertyTile> playerBProperties, int moneyAB)
        {
            ID = Guid.NewGuid();
            PlayerA = playerA;
            PlayerB = playerB;
            PlayerAProperties = playerAProperties ?? new List<PropertyTile>();
            PlayerBProperties = playerBProperties ?? new List<PropertyTile>();
            MoneyAB = moneyAB;
        }
    }
}
