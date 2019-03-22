using System;
using System.Linq;
using Xopoly.Models;
using Xopoly.Repositories;

namespace Xopoly.Utilities
{
    public static class LobbyUtilities
    {
        private static Random _random = new Random();
        private static readonly string alphaNumeric = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        public static string GenerateUniqueLobbyID()
        {
            var lobbyID = string.Empty;

            do
            {
                lobbyID = new string(Enumerable.Repeat(alphaNumeric, 3).Select(s => s[_random.Next(s.Length)]).ToArray());
            } while (LobbyRepository.LobbyDict.Keys.Any(id => id == lobbyID));

            return lobbyID;
        }

        public static Lobby FindLobbyByOwner(Player player)
        {
            return LobbyRepository.LobbyDict.FirstOrDefault(kvp => kvp.Value.Owner.ComputerUserID == player.ComputerUserID).Value;
        }

        public static Lobby FindLobbyContainingPlayer(Player player)
        {
            return LobbyRepository.LobbyDict.Values.FirstOrDefault(l => l.Players.Any(p => p.ComputerUserID == player.ComputerUserID));
        }

        public static void RemovePlayerFromLobby(Player player, Lobby lobby)
        {
            var index = lobby.Players.FindIndex(p => p.ComputerUserID == player.ComputerUserID);
            lobby.Players.RemoveAt(index);
        }
    }
}