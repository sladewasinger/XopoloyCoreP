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
    public class RailroadTests
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
        public void Railroad_Charges_Base_Fee_When_One_Is_Owned()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 1 } })
                    .Repeat.Once();

            var railRoad = gameState.Tiles.First(x => x is RailroadPropertyTile) as RailroadPropertyTile;
            railRoad.OwnerPlayerID = playerA.ID;

            gameState.CurrentPlayerListIndex = 1;
            gameState.Players[1].BoardPosition = gameState.Tiles.IndexOf(railRoad) - 2;
            _engine.RollDiceAndMovePlayer(playerB.ID, gameState);
            
            Assert.AreEqual(Constants.STARTING_MONEY - Constants.SINGLE_RAILROAD_FEE, gameState.Players[1].Money);
        }

        [TestMethod]
        public void Railroad_Charges_Base_Fee_When_Two_Are_Owned()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 1 } })
                    .Repeat.Once();

            var railRoads = gameState.Tiles
                .Where(x => x is RailroadPropertyTile)
                .Select(x => x as RailroadPropertyTile)
                .ToList();
            railRoads[0].OwnerPlayerID = playerA.ID;
            railRoads[1].OwnerPlayerID = playerA.ID;


            var ownedRailRoadsCount = railRoads.Where(x => x.OwnerPlayerID == playerA.ID).Count();

            gameState.CurrentPlayerListIndex = 1;
            gameState.Players[1].BoardPosition = gameState.Tiles.IndexOf(railRoads[0]) - 2;
            _engine.RollDiceAndMovePlayer(playerB.ID, gameState);

            Assert.AreEqual(Constants.STARTING_MONEY - Constants.SINGLE_RAILROAD_FEE * Math.Pow(2, ownedRailRoadsCount-1), gameState.Players[1].Money);
        }
    }
}
