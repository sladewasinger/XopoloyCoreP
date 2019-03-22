using Newtonsoft.Json;
using System.Collections.Generic;
using Xopoly.Logic;

namespace Xopoly.Models
{
    public class Lobby
    {
        public List<Player> Players { get; set; }
        public string ID { get; set; }
        public string Name { get; set; }
        public int MaxPlayers { get; set; }
        public bool Open { get; set; }
        public Player Owner { get; set; }
        public bool Public { get; set; }
        [JsonIgnore]
        public XopolyController XopolyController { get; set; }
    }
}