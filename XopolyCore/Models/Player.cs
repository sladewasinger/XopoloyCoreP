using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Xopoly.Models
{
    public class Player
    {
        public string Username { get; set; }
        public Guid GameID { get; set; }
        [JsonIgnore]
        public List<string> ConnectionIDs { get; set; }
        public string ComputerUserID { get; set; }
        public bool IsSpectator { get; set; }
    }
}