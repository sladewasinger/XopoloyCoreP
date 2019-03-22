using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Xopoly.Models;
using Xopoly.Utilities;

namespace Xopoly.Repositories
{
    public static class LobbyRepository
    {
        public static ConcurrentDictionary<string, Lobby> LobbyDict { get; private set; } = new ConcurrentDictionary<string, Lobby>();

        public static Lobby CreateLobby(Player lobbyOwner, string lobbyName, bool isPublic)
        {
            var lobby = new Lobby
            {
                ID = LobbyUtilities.GenerateUniqueLobbyID(),
                MaxPlayers = 6,
                Owner = lobbyOwner,
                Players = new List<Player>() {
                    lobbyOwner
                },
                Open = true,
                Public = isPublic,
                Name = lobbyName
            };

            LobbyDict.AddOrUpdate(lobby.ID, lobby, (id, oldLobby) => lobby);

            return lobby;
        }

        public static bool JoinLobby(Lobby lobby, Player player, bool spectate = false)
        {
            if (lobby == null || lobby.Players.Any(p => p.ComputerUserID == player.ComputerUserID))
            {
                return false;
            }

            if (!spectate && (!lobby.Open || lobby.Players.Count() >= lobby.MaxPlayers))
            {
                return false;
            }

            lobby.Players.Add(player);
            player.IsSpectator = spectate;

            return true;
        }

        public static void DisconnectPlayerFromLobby(Lobby lobby, Player player)
        {
            if (lobby == null || player == null)
            {
                return;
            }

            if (lobby.XopolyController != null)
            {
                lobby.XopolyController.DeclareBankruptcy(player.GameID);
            }

            LobbyUtilities.RemovePlayerFromLobby(player, lobby);

            if (!lobby.Players.Any())
            {
                LobbyDict.TryRemove(lobby.ID, out lobby);
            }
            else if (lobby.Owner.ComputerUserID == player.ComputerUserID)
            {
                lobby.Owner = lobby.Players.First();
            }
        }

        public static Lobby GetLobbyById(string lobbyID)
        {
            LobbyDict.TryGetValue(lobbyID, out Lobby lobby);
            return lobby;
        }
    }
}