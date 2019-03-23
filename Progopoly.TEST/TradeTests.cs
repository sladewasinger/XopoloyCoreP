using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using Rhino.Mocks;
using Progopoly.Models;
using Progopoly.Logic;
using Progopoly.Models.Tiles;
using System.Collections.Generic;
using System.Linq;

namespace Progopoly.TEST
{
    [TestClass]
    public class TradeTests
    {
        private Engine _engine;
        private IDiceRoller _diceRoller;
        private GameLog _gameLog;

        [TestInitialize]
        public void TestInitialize()
        {
            _diceRoller = MockRepository.Mock<IDiceRoller>();
            _gameLog = new GameLog();
            _engine = new Engine(_diceRoller, _gameLog);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            PrintLogToCurrentTest();
        }

        private void PrintLogToCurrentTest()
        {
            foreach (var entry in _gameLog.DumpLog())
            {
                Debug.WriteLine(entry);
            }
        }

        [TestMethod]
        public void TradeOffer_Shows_Up_Correctly_In_GameState()
        {
            var playerA = _engine.CreateNewPlayer("PlayerA");
            var playerB = _engine.CreateNewPlayer("PlayerB");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 1 } })
                    .Repeat.Once();

            var railroad = gameState.Tiles.First(x => x is RailroadPropertyTile) as RailroadPropertyTile;
            railroad.OwnerPlayerID = playerA.ID;

            _engine.OfferTrade(playerA.ID, gameState, playerB.ID, new List<int>() { gameState.Tiles.IndexOf(railroad) }, null, -69);

            Assert.AreEqual(-69, gameState.TradeOffers.First().MoneyAB);
            Assert.AreEqual(playerA.ID, gameState.TradeOffers.First().PlayerA.ID);
            Assert.AreEqual(playerB.ID, gameState.TradeOffers.First().PlayerB.ID);
        }

        [TestMethod]
        public void AcceptTradeOffer_Exchanges_One_Tile_For_Money_Correctly()
        {
            var playerA = _engine.CreateNewPlayer("PlayerA");
            var playerB = _engine.CreateNewPlayer("PlayerB");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 9, 1 } })
                    .Repeat.Once();

            var railroad = gameState.Tiles.First(x => x is RailroadPropertyTile) as RailroadPropertyTile;
            railroad.OwnerPlayerID = playerA.ID;

            var tradeOffer = new TradeOffer(playerA, playerB, new List<PropertyTile>() { railroad }, null, -69);
            gameState.TradeOffers.Add(tradeOffer);
            gameState.CurrentPlayerListIndex = gameState.Players.IndexOf(playerB);

            _engine.AcceptTradeOffer(playerB.ID, gameState, gameState.TradeOffers.First().ID);

            Assert.AreEqual(Constants.STARTING_MONEY + 69, gameState.Players[0].Money);
            Assert.AreEqual(Constants.STARTING_MONEY - 69, gameState.Players[1].Money);

            Assert.AreEqual(railroad.OwnerPlayerID, playerB.ID);
        }

        [TestMethod]
        public void AcceptTradeOffer_Exchanges_No_Money_Tiles_For_Tiles_Correctly()
        {
            var playerA = _engine.CreateNewPlayer("PlayerA");
            var playerB = _engine.CreateNewPlayer("PlayerB");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 9, 1 } })
                    .Repeat.Once();

            var railroad = gameState.Tiles.First(x => x is RailroadPropertyTile) as RailroadPropertyTile;
            railroad.OwnerPlayerID = playerA.ID;

            var orangeProperties = gameState.Tiles
                .Where(x => x is ColorPropertyTile)
                .Select(x => x as ColorPropertyTile)
                .Where(x => x.Color == Constants.PropertyColor.Orange)
                .ToList();
            orangeProperties[0].OwnerPlayerID = playerB.ID;
            orangeProperties[1].OwnerPlayerID = playerB.ID;
            orangeProperties[2].OwnerPlayerID = playerB.ID;

            var tradeOffer = new TradeOffer(playerA, playerB, new List<PropertyTile>() { railroad }, orangeProperties.Select(x => x as PropertyTile).ToList(), 0);
            gameState.TradeOffers.Add(tradeOffer);
            gameState.CurrentPlayerListIndex = gameState.Players.IndexOf(playerB);

            _engine.AcceptTradeOffer(playerB.ID, gameState, gameState.TradeOffers.First().ID);

            Assert.AreEqual(Constants.STARTING_MONEY, gameState.Players[0].Money);
            Assert.AreEqual(Constants.STARTING_MONEY, gameState.Players[1].Money);

            Assert.AreEqual(railroad.OwnerPlayerID, playerB.ID);

            Assert.AreEqual(orangeProperties[0].OwnerPlayerID, playerA.ID);
            Assert.AreEqual(orangeProperties[1].OwnerPlayerID, playerA.ID);
            Assert.AreEqual(orangeProperties[2].OwnerPlayerID, playerA.ID);
        }

        [TestMethod]
        public void AcceptTradeOffer_Exchanges_Some_Money_And_Tiles_For_Tiles_Correctly()
        {
            var playerA = _engine.CreateNewPlayer("PlayerA");
            var playerB = _engine.CreateNewPlayer("PlayerB");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 9, 1 } })
                    .Repeat.Once();

            var railroad = gameState.Tiles.First(x => x is RailroadPropertyTile) as RailroadPropertyTile;
            railroad.OwnerPlayerID = playerA.ID;

            var orangeProperties = gameState.Tiles
                .Where(x => x is ColorPropertyTile)
                .Select(x => x as ColorPropertyTile)
                .Where(x => x.Color == Constants.PropertyColor.Orange)
                .ToList();
            orangeProperties[0].OwnerPlayerID = playerB.ID;
            orangeProperties[1].OwnerPlayerID = playerB.ID;
            orangeProperties[2].OwnerPlayerID = playerB.ID;

            var tradeOffer = new TradeOffer(playerA, playerB, new List<PropertyTile>() { railroad }, orangeProperties.Select(x => x as PropertyTile).ToList(), 900);
            gameState.TradeOffers.Add(tradeOffer);
            gameState.CurrentPlayerListIndex = gameState.Players.IndexOf(playerB);

            _engine.AcceptTradeOffer(playerB.ID, gameState, gameState.TradeOffers.First().ID);

            Assert.AreEqual(Constants.STARTING_MONEY - 900, gameState.Players[0].Money);
            Assert.AreEqual(Constants.STARTING_MONEY + 900, gameState.Players[1].Money);

            Assert.AreEqual(railroad.OwnerPlayerID, playerB.ID);

            Assert.AreEqual(orangeProperties[0].OwnerPlayerID, playerA.ID);
            Assert.AreEqual(orangeProperties[1].OwnerPlayerID, playerA.ID);
            Assert.AreEqual(orangeProperties[2].OwnerPlayerID, playerA.ID);
        }

        [TestMethod]
        public void TradeOffer_Rejected_For_No_Money_One_Tile()
        {
            var playerA = _engine.CreateNewPlayer("PlayerA");
            var playerB = _engine.CreateNewPlayer("PlayerB");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 1 } })
                    .Repeat.Once();

            var railroad = gameState.Tiles.First(x => x is RailroadPropertyTile) as RailroadPropertyTile;
            railroad.OwnerPlayerID = playerA.ID;

            _engine.OfferTrade(playerA.ID, gameState, playerB.ID, new List<int>() { gameState.Tiles.IndexOf(railroad) }, null, 0);

            Assert.IsFalse(gameState.TradeOffers.Any());
        }

        [TestMethod]
        public void TradeOffer_Rejected_For_Money_No_Tiles()
        {
            var playerA = _engine.CreateNewPlayer("PlayerA");
            var playerB = _engine.CreateNewPlayer("PlayerB");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 1 } })
                    .Repeat.Once();

            var railroad = gameState.Tiles.First(x => x is RailroadPropertyTile) as RailroadPropertyTile;
            railroad.OwnerPlayerID = playerA.ID;

            _engine.OfferTrade(playerA.ID, gameState, playerB.ID, null, null, 100);

            Assert.IsFalse(gameState.TradeOffers.Any());
        }

        [TestMethod]
        public void TradeOffer_Rejected_For_One_Sided_Trade_PlayerA()
        {
            var playerA = _engine.CreateNewPlayer("PlayerA");
            var playerB = _engine.CreateNewPlayer("PlayerB");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 1 } })
                    .Repeat.Once();

            var railroad = gameState.Tiles.First(x => x is RailroadPropertyTile) as RailroadPropertyTile;
            railroad.OwnerPlayerID = playerA.ID;

            _engine.OfferTrade(playerA.ID, gameState, playerB.ID, new List<int>() { gameState.Tiles.IndexOf(railroad) }, null, 100);

            Assert.IsFalse(gameState.TradeOffers.Any());
        }

        [TestMethod]
        public void TradeOffer_Rejected_For_One_Sided_Trade_PlayerB()
        {
            var playerA = _engine.CreateNewPlayer("PlayerA");
            var playerB = _engine.CreateNewPlayer("PlayerB");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 1 } })
                    .Repeat.Once();

            var railroad = gameState.Tiles.First(x => x is RailroadPropertyTile) as RailroadPropertyTile;
            railroad.OwnerPlayerID = playerB.ID;

            _engine.OfferTrade(playerA.ID, gameState, playerB.ID, null, new List<int>() { gameState.Tiles.IndexOf(railroad) }, -100);

            Assert.IsFalse(gameState.TradeOffers.Any());
        }
    }
}
