using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xopoly.Logic;
using Xopoly.Models;
using Xopoly.Repositories;
using Xopoly.Utilities;

namespace XopolyCore.Hubs
{
    public class LobbyHub : Hub
    {
        private readonly IHubContext<LobbyHub> _hubContext;

        public LobbyHub(IHubContext<LobbyHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var player = PlayerRepository.GetPlayerByConnectionID(Context.ConnectionId);
            if (player != null && player.ConnectionIDs.Take(2).Count() <= 1)
            {
                var lobby = LobbyUtilities.FindLobbyContainingPlayer(player);
                LobbyRepository.DisconnectPlayerFromLobby(lobby, player);
                PlayerRepository.RemovePlayer(player.ComputerUserID);
                player.Username = null;
                player.ComputerUserID = null;

                UpdateClientStates();
                Clients.Caller.SendAsync("updateState", GenerateClientState(player));
                SendGameStateAndLog(lobby);
            }

            return base.OnDisconnectedAsync(exception);
        }

        public override Task OnConnectedAsync()
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player != null && !player.ConnectionIDs.Any(x => x == Context.ConnectionId))
            {
                player.ConnectionIDs.Add(Context.ConnectionId);
            }

            return base.OnConnectedAsync();
        }


        public void RegisterPlayer(string username)
        {
            try
            {
                //NOTE:
                //This identity will be NULL if IIS/server is setup with Anonymous authentication enabled.
                //All authentication methods, other than windows authentication, must be disabled in IIS.
                //Set windows authentication to true in IIS to get computerUserId
                string computerUserID = Context.ConnectionId;

                if (string.IsNullOrEmpty(computerUserID) || string.IsNullOrWhiteSpace(username))
                {
                    throw new HubException("ComputerUserID or username is empty!");
                }
                if (PlayerRepository.GetPlayerByComputerUserID(computerUserID) != null)
                {
                    throw new HubException("Player already registered!");
                }

                var player = PlayerRepository.CreatePlayer(Context.ConnectionId, computerUserID, username);
                UpdateClientStates();
            }
            catch (Exception ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public void DisconnectPlayer()
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player != null)
            {
                var lobby = LobbyUtilities.FindLobbyContainingPlayer(player);
                LobbyRepository.DisconnectPlayerFromLobby(lobby, player);
                PlayerRepository.RemovePlayer(player.ComputerUserID);
                player.Username = null;
                player.ComputerUserID = null;

                UpdateClientStates();
                Clients.Caller.SendAsync("updateState", GenerateClientState(player));
            }
        }

        public void CreateLobby(string lobbyName, bool isPublic)
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player == null || LobbyUtilities.FindLobbyContainingPlayer(player) != null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(lobbyName))
            {
                return;
            }

            var lobby = LobbyRepository.CreateLobby(player, lobbyName, isPublic);

            UpdateClientStates();
        }

        public void DisconnectFromLobby()
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player == null)
            {
                return;
            }

            var lobby = LobbyUtilities.FindLobbyContainingPlayer(player);
            if (lobby == null)
            {
                return;
            }

            LobbyRepository.DisconnectPlayerFromLobby(lobby, player);
            UpdateClientStates();
            SendGameStateAndLog(lobby);
        }

        public bool JoinLobby(string lobbyID)
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player == null || LobbyRepository.LobbyDict.Values.Any(x => x.Players.Any(p => p.ComputerUserID == player.ComputerUserID)))
            {
                return false;
            }

            var lobby = LobbyRepository.GetLobbyById(lobbyID);
            var success = LobbyRepository.JoinLobby(lobby, player);

            if (success)
            {
                UpdateClientStates();
            }

            return success;
        }

        public bool SpectateLobby(string lobbyID)
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player == null || LobbyRepository.LobbyDict.Values.Any(x => x.Players.Any(p => p.ComputerUserID == player.ComputerUserID)))
            {
                return false;
            }

            var lobby = LobbyRepository.GetLobbyById(lobbyID);
            var success = LobbyRepository.JoinLobby(lobby, player, true);

            if (success)
            {
                UpdateClientStates();
                SendGameStateAndLog(lobby);
            }

            return success;
        }

        public void RequestStateUpdate()
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            Clients.Caller.SendAsync("updateState", GenerateClientState(player));
        }

        public void StartGame()
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player == null)
            {
                return;
            }

            var lobby = LobbyUtilities.FindLobbyByOwner(player);
            if (lobby == null)
            {
                return;
            }

            lobby.Open = false;

            lobby.XopolyController = new XopolyController(SendGameStateAndLog, lobby, _hubContext);
            lobby.XopolyController.SetupPlayers(lobby.Players.Where(x => !x.IsSpectator).ToList());
            lobby.XopolyController.StartGame();

            lobby.XopolyController.GameLog.Log($"{player.Username} started a game!");

            UpdateClientStates();
            SendGameStateAndLog(lobby);
        }

        public void RollDice()
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player == null)
            {
                return;
            }

            var lobby = LobbyUtilities.FindLobbyContainingPlayer(player);
            if (lobby == null || lobby.XopolyController == null)
            {
                return;
            }

            lobby.XopolyController.CallMethod(methodArgs: player.GameID);
            //lobby.XopolyController.RollDice(player.GameID);
            //SendGameStateAndLog(lobby);
        }

        public void EndTurn()
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player == null)
            {
                return;
            }

            var lobby = LobbyUtilities.FindLobbyContainingPlayer(player);
            if (lobby == null || lobby.XopolyController == null)
            {
                return;
            }

            lobby.XopolyController.EndTurn(player.GameID);

            SendGameStateAndLog(lobby);
        }

        public void BuyProperty()
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player == null)
            {
                return;
            }

            var lobby = LobbyUtilities.FindLobbyContainingPlayer(player);
            if (lobby == null || lobby.XopolyController == null)
            {
                return;
            }

            lobby.XopolyController.BuyProperty(player.GameID);
            SendGameStateAndLog(lobby);
        }

        public void StartAuctionOnProperty()
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player == null)
            {
                return;
            }

            var lobby = LobbyUtilities.FindLobbyContainingPlayer(player);
            if (lobby == null || lobby.XopolyController == null)
            {
                return;
            }

            lobby.XopolyController.StartAuctionOnProperty(player.GameID);
            SendGameStateAndLog(lobby);
        }

        public void BetOnAuction(int amount)
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player == null)
            {
                return;
            }

            var lobby = LobbyUtilities.FindLobbyContainingPlayer(player);
            if (lobby == null || lobby.XopolyController == null)
            {
                return;
            }

            lobby.XopolyController.BetOnAuction(player.GameID, amount);
            SendGameStateAndLog(lobby);
        }

        public void BuildHouse(int tileIndex)
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player == null)
            {
                return;
            }

            var lobby = LobbyUtilities.FindLobbyContainingPlayer(player);
            if (lobby == null || lobby.XopolyController == null)
            {
                return;
            }

            lobby.XopolyController.BuyHouse(player.GameID, tileIndex);
            SendGameStateAndLog(lobby);
        }

        public void MortgageProperty(int tileIndex)
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player == null)
            {
                return;
            }

            var lobby = LobbyUtilities.FindLobbyContainingPlayer(player);
            if (lobby == null || lobby.XopolyController == null)
            {
                return;
            }

            lobby.XopolyController.MortgageProperty(player.GameID, tileIndex);
            SendGameStateAndLog(lobby);
        }

        public void RedeemProperty(int tileIndex)
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player == null)
            {
                return;
            }

            var lobby = LobbyUtilities.FindLobbyContainingPlayer(player);
            if (lobby == null || lobby.XopolyController == null)
            {
                return;
            }

            lobby.XopolyController.RedeemProperty(player.GameID, tileIndex);
            SendGameStateAndLog(lobby);
        }

        public void SellHouse(int tileIndex)
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player == null)
            {
                return;
            }

            var lobby = LobbyUtilities.FindLobbyContainingPlayer(player);
            if (lobby == null || lobby.XopolyController == null)
            {
                return;
            }

            lobby.XopolyController.SellHouse(player.GameID, tileIndex);
            SendGameStateAndLog(lobby);
        }

        public void OfferTrade(Guid targetPlayerID, List<int> tileIndexesA, List<int> tileIndexesB, int moneyA, int moneyB)
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player == null)
            {
                return;
            }

            var lobby = LobbyUtilities.FindLobbyContainingPlayer(player);
            if (lobby == null || lobby.XopolyController == null)
            {
                return;
            }

            lobby.XopolyController.OfferTrade(player.GameID, targetPlayerID, tileIndexesA, tileIndexesB, moneyA, moneyB);
            SendGameStateAndLog(lobby);
        }

        public void AcceptTrade(Guid tradeOfferID)
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player == null)
            {
                return;
            }

            var lobby = LobbyUtilities.FindLobbyContainingPlayer(player);
            if (lobby == null || lobby.XopolyController == null)
            {
                return;
            }

            lobby.XopolyController.AcceptTrade(player.GameID, tradeOfferID);
            SendGameStateAndLog(lobby);
        }

        public void RejectTrade(Guid tradeOfferID)
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player == null)
            {
                return;
            }

            var lobby = LobbyUtilities.FindLobbyContainingPlayer(player);
            if (lobby == null || lobby.XopolyController == null)
            {
                return;
            }

            lobby.XopolyController.RejectTrade(player.GameID, tradeOfferID);
            SendGameStateAndLog(lobby);
        }

        public void InstantMonopoly()
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player == null)
            {
                return;
            }

            var lobby = LobbyUtilities.FindLobbyContainingPlayer(player);
            if (lobby == null || lobby.XopolyController == null)
            {
                return;
            }

            lobby.XopolyController.InstantMonopoly(player.GameID);
            SendGameStateAndLog(lobby);
        }

        public void DeclareBankruptcy()
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player == null)
            {
                return;
            }

            var lobby = LobbyUtilities.FindLobbyContainingPlayer(player);
            if (lobby == null || lobby.XopolyController == null)
            {
                return;
            }

            lobby.XopolyController.DeclareBankruptcy(player.GameID);
            player.IsSpectator = true;

            SendGameStateAndLog(lobby);
        }

        public void BuyOutOfJail()
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player == null)
            {
                return;
            }

            var lobby = LobbyUtilities.FindLobbyContainingPlayer(player);
            if (lobby == null || lobby.XopolyController == null)
            {
                return;
            }

            lobby.XopolyController.BuyOutOfJail(player.GameID);
            SendGameStateAndLog(lobby);
        }

        public void UseGOOJFC()
        {
            var player = PlayerRepository.GetPlayerByComputerUserID(Context.ConnectionId);
            if (player == null)
            {
                return;
            }

            var lobby = LobbyUtilities.FindLobbyContainingPlayer(player);
            if (lobby == null || lobby.XopolyController == null)
            {
                return;
            }

            lobby.XopolyController.UseGetOutOfJailFreeCard(player.GameID);
            SendGameStateAndLog(lobby);
        }

        private void SendGameStateAndLog(Lobby lobby, IHubContext<LobbyHub> hubContext = null)
        {
            if (lobby == null || lobby.XopolyController == null)
            {
                return;
            }

            hubContext = hubContext ?? _hubContext;

            hubContext.Clients.Clients(lobby.Players.SelectMany(x => x.ConnectionIDs).ToList()).SendAsync("updateGameState", lobby.XopolyController.GameState);
            hubContext.Clients.Clients(lobby.Players.SelectMany(x => x.ConnectionIDs).ToList()).SendAsync("updateGameLog", lobby.XopolyController.GameLog.DumpLog().Reverse());
        }

        private void UpdateClientStates()
        {
            foreach (var player in PlayerRepository.PlayerList.Values)
            {
                ClientState clientState = GenerateClientState(player);

                foreach (var connectionID in player.ConnectionIDs)
                {
                    Clients.Client(connectionID).SendAsync("updateState", clientState);
                }
            }
        }

        private ClientState GenerateClientState(Player player)
        {
            ClientState clientState = new ClientState()
            {
                Lobbies = LobbyRepository.LobbyDict.Values.Where(l => l.Public || l.Players.Any(p => p.ComputerUserID == player.ComputerUserID)).ToList(),
                Players = PlayerRepository.PlayerList.Values.ToList(),
                Player = player
            };

            return clientState;
        }
    }
}