using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Progopoly.Models
{
    public class Auction
    {
        public List<AuctionParticipant> AuctionParticipants { get; set; }
        public int TotalBets { get; set; }
    }
}
