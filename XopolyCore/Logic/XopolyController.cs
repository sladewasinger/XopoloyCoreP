using GameModels = Progopoly.Models;
using System.Collections.Generic;
using System;
using System.Timers;
using Xopoly.Models;
using Progopoly.Logic;

namespace Xopoly.Logic
{
    public class XopolyController
    {
        private Engine _engine;
        private List<GameModels.Player> _players = new List<GameModels.Player>();
        public GameModels.GameState GameState { get; private set; }
        public GameModels.IGameLog GameLog { get; set; }
        public Timer PlayerTimeoutTimer { get; set; }
        public Action UpdateClientsAction { get; set; }


        public XopolyController(Action updateClientsCallback)
        {
            GameLog = new ServerGameLog();
            _engine = new Engine(new DiceRoller(), GameLog);
            PlayerTimeoutTimer = new Timer(90 * 1000)
            {
                AutoReset = true
            };
            PlayerTimeoutTimer.Elapsed += (source, e) => PlayerTimedOut(source, e);
            UpdateClientsAction = updateClientsCallback;
        }


        private void PlayerTimedOut(object source, ElapsedEventArgs e)
        {
            var cpID = GameState.CurrentPlayer.ID;

            GameLog.Log("*YAWN* Somebody is taking too long. I'm gonna play for you this turn.");

            RollDice(cpID);
            RollDice(cpID);
            StartAuctionOnProperty(cpID);
            BetOnAuction(cpID, 1);
            RollDice(cpID);
            EndTurn(cpID);

            UpdateClientsAction();

            return;
        }

        public void StartGame()
        {
            GameState = _engine.CreateInitialGameState(_players);
            PlayerTimeoutTimer.Enabled = true;
            PlayerTimeoutTimer.Start();
        }

        public void SetupPlayers(List<Player> players)
        {
            foreach(var player in players)
            {
                var gamePlayer = _engine.CreateNewPlayer(player.Username);
                player.GameID = gamePlayer.ID;
                _players.Add(gamePlayer);
            }
        }

        public void RollDice(Guid playerGameID)
        {
            _engine.RollDiceAndMovePlayer(playerGameID, GameState);
        }

        public void EndTurn(Guid playerGameID)
        {
            PlayerTimeoutTimer.Stop();
            _engine.FinishPlayerTurn(playerGameID, GameState);
            PlayerTimeoutTimer.Start();
        }

        public void BuyProperty(Guid playerGameID)
        {
            _engine.BuyProperty(playerGameID, GameState);
        }

        public void StartAuctionOnProperty(Guid playerGameID)
        {
            _engine.StartAuctionOnProperty(playerGameID, GameState);
        }

        public void BetOnAuction(Guid playerGameID, int amount)
        {
            _engine.BetOnAuction(playerGameID, GameState, amount);
        }

        public void BuyHouse(Guid playerGameID, int tileIndex)
        {
            _engine.BuyAndBuildHouse(playerGameID, GameState, tileIndex);
        }

        public void InstantMonopoly(Guid playerGameID)
        {
            _engine.InstantMonopoly(playerGameID, GameState);
        }

        public void DeclareBankruptcy(Guid playerGameID)
        {
            _engine.DeclareBankruptcy(playerGameID, GameState);
        }

        public void BuyOutOfJail(Guid playerGameID)
        {
            _engine.BuyOutOfJail(playerGameID, GameState);
        }

        public void MortgageProperty(Guid playerGameID, int tileIndex)
        {
            _engine.MortgageProperty(playerGameID, GameState, tileIndex);
        }

        public void RedeemProperty(Guid playerGameID, int tileIndex)
        {
            _engine.RedeemProperty(playerGameID, GameState, tileIndex);
        }

        public void SellHouse(Guid playerGameID, int tileIndex)
        {
            _engine.SellHouse(playerGameID, GameState, tileIndex);
        }

        public void OfferTrade(Guid playerGameID, Guid targetPlayerID, List<int> tileIndexesA, List<int> tileIndexesB, int moneyA, int moneyB)
        {
            _engine.OfferTrade(playerGameID, GameState, targetPlayerID, tileIndexesA, tileIndexesB, moneyA - moneyB);
        }

        public void AcceptTrade(Guid playerGameID, Guid tradeOfferID)
        {
            _engine.AcceptTradeOffer(playerGameID, GameState, tradeOfferID);
        }

        public void RejectTrade(Guid playerGameID, Guid tradeOfferID)
        {
            _engine.RejectTradeOffer(playerGameID, GameState, tradeOfferID);
        }
    }
}