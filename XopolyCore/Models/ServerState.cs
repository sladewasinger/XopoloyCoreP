using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Xopoly.Models
{
    public class ServerState
    {
        public List<Lobby> Lobbies { get; set; }
        public List<Player> Players { get; set; }
    }
}