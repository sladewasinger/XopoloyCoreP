using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Progopoly.Models
{
    public class AuctionParticipant : Player
    {
        [JsonIgnore]
        public int? AuctionBet { get; set; }
        public bool HasPlacedBet
        {
            get
            {
                return AuctionBet != null;
            }
        }
        public int BetPosition { get; set; }

        public AuctionParticipant() {}
        public AuctionParticipant(Player player)
        {
            AuctionBet = null;
            ID = player.ID;
            Name = player.Name;
            Money = player.Money;
            BoardPosition = player.BoardPosition;
            NumDoublesRolledInARow = player.NumDoublesRolledInARow;
            CurrentDiceRoll = player.CurrentDiceRoll;
            IsInJail = player.IsInJail;
            RollsWhileInJail = player.RollsWhileInJail;
        }
    }
}
