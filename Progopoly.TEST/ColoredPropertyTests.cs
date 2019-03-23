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
    public class ColoredPropertyTests
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
        public void Brown_Colored_Property_Charges_Single_Rent_When_Not_All_Owned()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 1 } })
                    .Repeat.Once();

            var brownProperties = gameState.Tiles
                .Where(x => x is ColorPropertyTile)
                .Select(x => x as ColorPropertyTile)
                .Where(x => x.Color == Constants.PropertyColor.Brown)
                .ToList();
            brownProperties[0].OwnerPlayerID = playerA.ID;

            gameState.CurrentPlayerListIndex = 1;
            gameState.Players[1].BoardPosition = gameState.Tiles.IndexOf(brownProperties[0]) - 2;
            _engine.RollDiceAndMovePlayer(playerB.ID, gameState);

            Assert.AreEqual(Constants.STARTING_MONEY - brownProperties[0].Rent, gameState.Players[1].Money);
        }

        [TestMethod]
        public void Brown_Colored_Property_Charges_Double_Rent_When_All_Owned()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 1 } })
                    .Repeat.Once();

            var brownProperties = gameState.Tiles
                .Where(x => x is ColorPropertyTile)
                .Select(x => x as ColorPropertyTile)
                .Where(x => x.Color == Constants.PropertyColor.Brown)
                .ToList();
            brownProperties[0].OwnerPlayerID = playerA.ID;
            brownProperties[1].OwnerPlayerID = playerA.ID;

            gameState.CurrentPlayerListIndex = 1;
            gameState.Players[1].BoardPosition = gameState.Tiles.IndexOf(brownProperties[0]) - 2;
            _engine.RollDiceAndMovePlayer(playerB.ID, gameState);

            Assert.AreEqual(Constants.STARTING_MONEY - brownProperties[0].Rent*2, gameState.Players[1].Money);
        }

        [TestMethod]
        public void Orange_Colored_Property_Charges_Single_Rent_When_Two_Owned()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 1 } })
                    .Repeat.Once();

            var orangeProperties = gameState.Tiles
                .Where(x => x is ColorPropertyTile)
                .Select(x => x as ColorPropertyTile)
                .Where(x => x.Color == Constants.PropertyColor.Orange)
                .ToList();
            orangeProperties[0].OwnerPlayerID = playerA.ID;
            orangeProperties[1].OwnerPlayerID = playerA.ID;

            gameState.CurrentPlayerListIndex = 1;
            gameState.Players[1].BoardPosition = gameState.Tiles.IndexOf(orangeProperties[0]) - 2;
            _engine.RollDiceAndMovePlayer(playerB.ID, gameState);

            Assert.AreEqual(Constants.STARTING_MONEY - orangeProperties[0].Rent, gameState.Players[1].Money);
        }

        [TestMethod]
        public void Orange_Colored_Property_Charges_Double_Rent_When_Three_Owned()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 1 } })
                    .Repeat.Once();

            var orangeProperties = gameState.Tiles
                .Where(x => x is ColorPropertyTile)
                .Select(x => x as ColorPropertyTile)
                .Where(x => x.Color == Constants.PropertyColor.Orange)
                .ToList();
            orangeProperties[0].OwnerPlayerID = playerA.ID;
            orangeProperties[1].OwnerPlayerID = playerA.ID;
            orangeProperties[2].OwnerPlayerID = playerA.ID;

            gameState.CurrentPlayerListIndex = 1;
            gameState.Players[1].BoardPosition = gameState.Tiles.IndexOf(orangeProperties[0]) - 2;
            _engine.RollDiceAndMovePlayer(playerB.ID, gameState);

            Assert.AreEqual(Constants.STARTING_MONEY - orangeProperties[0].Rent * 2, gameState.Players[1].Money);
        }

        [TestMethod]
        public void Colored_Property_Charges_1_House_Rent_Correctly()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 1 } })
                    .Repeat.Once();

            var greenProperties = gameState.Tiles
                .Where(x => x is ColorPropertyTile)
                .Select(x => x as ColorPropertyTile)
                .Where(x => x.Color == Constants.PropertyColor.Green)
                .ToList();

            greenProperties[0].OwnerPlayerID = playerA.ID;
            greenProperties[1].OwnerPlayerID = playerA.ID;
            greenProperties[2].OwnerPlayerID = playerA.ID;

            greenProperties[0].BuildingCount = 1;

            gameState.CurrentPlayerListIndex = 1;
            gameState.Players[1].BoardPosition = gameState.Tiles.IndexOf(greenProperties[0]) - 2;
            _engine.RollDiceAndMovePlayer(playerB.ID, gameState);

            Assert.AreEqual(Constants.STARTING_MONEY - greenProperties[0].Rent, gameState.Players[1].Money);
        }

        [TestMethod]
        public void Colored_Property_Charges_2_House_Rent_Correctly()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 1 } })
                    .Repeat.Once();

            var greenProperties = gameState.Tiles
                .Where(x => x is ColorPropertyTile)
                .Select(x => x as ColorPropertyTile)
                .Where(x => x.Color == Constants.PropertyColor.Green)
                .ToList();

            greenProperties[0].OwnerPlayerID = playerA.ID;
            greenProperties[1].OwnerPlayerID = playerA.ID;
            greenProperties[2].OwnerPlayerID = playerA.ID;

            greenProperties[0].BuildingCount = 2;

            gameState.CurrentPlayerListIndex = 1;
            gameState.Players[1].BoardPosition = gameState.Tiles.IndexOf(greenProperties[0]) - 2;
            _engine.RollDiceAndMovePlayer(playerB.ID, gameState);

            Assert.AreEqual(Constants.STARTING_MONEY - greenProperties[0].Rent, gameState.Players[1].Money);
        }

        [TestMethod]
        public void Colored_Property_Charges_3_House_Rent_Correctly()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 1 } })
                    .Repeat.Once();

            var greenProperties = gameState.Tiles
                .Where(x => x is ColorPropertyTile)
                .Select(x => x as ColorPropertyTile)
                .Where(x => x.Color == Constants.PropertyColor.Green)
                .ToList();

            greenProperties[0].OwnerPlayerID = playerA.ID;
            greenProperties[1].OwnerPlayerID = playerA.ID;
            greenProperties[2].OwnerPlayerID = playerA.ID;

            greenProperties[0].BuildingCount = 3;

            gameState.CurrentPlayerListIndex = 1;
            gameState.Players[1].BoardPosition = gameState.Tiles.IndexOf(greenProperties[0]) - 2;
            _engine.RollDiceAndMovePlayer(playerB.ID, gameState);

            Assert.AreEqual(Constants.STARTING_MONEY - greenProperties[0].Rent, gameState.Players[1].Money);
        }

        [TestMethod]
        public void Colored_Property_Charges_4_House_Rent_Correctly()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 1 } })
                    .Repeat.Once();

            var greenProperties = gameState.Tiles
                .Where(x => x is ColorPropertyTile)
                .Select(x => x as ColorPropertyTile)
                .Where(x => x.Color == Constants.PropertyColor.Green)
                .ToList();

            greenProperties[0].OwnerPlayerID = playerA.ID;
            greenProperties[1].OwnerPlayerID = playerA.ID;
            greenProperties[2].OwnerPlayerID = playerA.ID;

            greenProperties[0].BuildingCount = 4;

            gameState.CurrentPlayerListIndex = 1;
            gameState.Players[1].BoardPosition = gameState.Tiles.IndexOf(greenProperties[0]) - 2;
            _engine.RollDiceAndMovePlayer(playerB.ID, gameState);

            Assert.AreEqual(Constants.STARTING_MONEY - greenProperties[0].Rent, gameState.Players[1].Money);
        }

        [TestMethod]
        public void Colored_Property_Charges_Hotel_Rent_Correctly()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 1 } })
                    .Repeat.Once();

            var greenProperties = gameState.Tiles
                .Where(x => x is ColorPropertyTile)
                .Select(x => x as ColorPropertyTile)
                .Where(x => x.Color == Constants.PropertyColor.Green)
                .ToList();

            greenProperties[0].OwnerPlayerID = playerA.ID;
            greenProperties[1].OwnerPlayerID = playerA.ID;
            greenProperties[2].OwnerPlayerID = playerA.ID;

            greenProperties[0].BuildingCount = 5;

            gameState.CurrentPlayerListIndex = 1;
            gameState.Players[1].BoardPosition = gameState.Tiles.IndexOf(greenProperties[0]) - 2;
            _engine.RollDiceAndMovePlayer(playerB.ID, gameState);

            Assert.AreEqual(Constants.STARTING_MONEY - greenProperties[0].Rent, gameState.Players[1].Money);
        }
    }
}
