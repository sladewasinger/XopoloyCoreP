using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Xopoly.Models;

namespace Xopoly.Repositories
{
    public class PlayerRepository
    {
        public static ConcurrentDictionary<string, Player> PlayerList { get; private set; } = new ConcurrentDictionary<string, Player>();

        public static Player CreatePlayer(string connectionID, string computerUserID, string username)
        {
            var player = new Player()
            {
                ConnectionIDs = new List<string>() { connectionID },
                ComputerUserID = computerUserID,
                GameID = Guid.Empty,
                Username = username
            };

            PlayerList.AddOrUpdate(player.ComputerUserID, player, (key, oldPlayer) => player);

            return player;
        }

        public static Player GetPlayerByConnectionID(string connectionID)
        {
            return PlayerList.Values.FirstOrDefault(p => p.ConnectionIDs.Any(x => x == connectionID));
        }

        public static Player GetPlayerByComputerUserID(string computerUserID)
        {
            PlayerList.TryGetValue(computerUserID, out Player player);
            return player;
        }

        public static Player RemovePlayer(string computerUserID)
        {
            PlayerList.TryRemove(computerUserID, out Player player);
            return player;
        }
    }
}