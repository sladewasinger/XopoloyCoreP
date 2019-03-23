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
    public class UtilitiesTests
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
        public void Utility_Charges_7_Times_Dice_Roll_When_1_Is_Owned()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 1 } })
                    .Repeat.Once();

            var utilities = gameState.Tiles.Where(x => x is UtilityPropertyTile).Select(x => x as UtilityPropertyTile).ToList();
            utilities[0].OwnerPlayerID = playerA.ID;

            gameState.CurrentPlayerListIndex = 1;
            gameState.Players[1].BoardPosition = gameState.Tiles.IndexOf(utilities[0]) - 2;
            _engine.RollDiceAndMovePlayer(playerB.ID, gameState);

            Assert.AreEqual(Constants.STARTING_MONEY - (2*7), gameState.Players[1].Money);
        }

        [TestMethod]
        public void Utility_Charges_14_Times_Dice_Roll_When_Both_Are_Owned()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 1 } })
                    .Repeat.Once();

            var utilities = gameState.Tiles.Where(x => x is UtilityPropertyTile).Select(x => x as UtilityPropertyTile).ToList();
            utilities[0].OwnerPlayerID = playerA.ID;
            utilities[1].OwnerPlayerID = playerA.ID;

            gameState.CurrentPlayerListIndex = 1;
            gameState.Players[1].BoardPosition = gameState.Tiles.IndexOf(utilities[0]) - 2;
            _engine.RollDiceAndMovePlayer(playerB.ID, gameState);

            Assert.AreEqual(Constants.STARTING_MONEY - (2 * 14), gameState.Players[1].Money);
        }
    }
}
