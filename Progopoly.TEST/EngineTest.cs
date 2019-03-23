using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Progopoly.Logic;
using Progopoly.Models;
using Progopoly.Utilities;
using System.Collections.Generic;
using System.Linq;
using Rhino.Mocks;
using Progopoly.Models.Tiles;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Progopoly.TEST
{
    [TestClass]
    public class EngineTest
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

        [TestMethod]
        public void EngineCreatesProperNewPlayer()
        {
            var player = _engine.CreateNewPlayer("Bad-Luck-Tim");
            Assert.AreEqual(0, player.BoardPosition);
            Assert.IsTrue(player.CurrentDiceRoll == null);
            Assert.IsFalse(player.ID == Guid.Empty);
            Assert.AreEqual("Bad-Luck-Tim", player.Name);
        }

        [TestMethod]
        public void Test1PlayerDiceRoll()
        {
            var tim = _engine.CreateNewPlayer("Bad-Luck-Tim");
            var gameState = _engine.CreateInitialGameState(new List<Player>() { tim });
            var initialBoardPosition = tim.BoardPosition;

            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 1, 2 } });

            _engine.RollDiceAndMovePlayer(tim.ID, gameState);

            var newBoardPosition = gameState.Players.First().BoardPosition;
            Assert.AreEqual(initialBoardPosition + 3, newBoardPosition);
        }

        [TestMethod]
        public void Test1PlayerDiceRollBoardWrap()
        {
            var tim = _engine.CreateNewPlayer("Bad-Luck-Tim");
            var gameState = _engine.CreateInitialGameState(new List<Player>() { tim });
            gameState.Players[0].BoardPosition = gameState.Tiles.Count() - 1;
            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 1, 3 } });

            _engine.RollDiceAndMovePlayer(tim.ID, gameState);

            var newBoardPosition = gameState.Players.First().BoardPosition;
            Assert.AreEqual(3, newBoardPosition);
        }

        [TestMethod]
        public void Test1PlayerDiceRollBoardWrapLandOnGo()
        {
            var tim = _engine.CreateNewPlayer("Bad-Luck-Tim");
            var gameState = _engine.CreateInitialGameState(new List<Player>() { tim });
            gameState.Players[0].BoardPosition = gameState.Tiles.Count() - 3;
            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 1, 2 } });

            _engine.RollDiceAndMovePlayer(tim.ID, gameState);

            var newBoardPosition = gameState.Players.First().BoardPosition;
            Assert.AreEqual(0, newBoardPosition);
        }

        [TestMethod]
        public void Test1PlayerDoubleDiceRoll()
        {
            var tim = _engine.CreateNewPlayer("Bad-Luck-Tim");
            var gameState = _engine.CreateInitialGameState(new List<Player>() { tim });
            gameState.Players[0].BoardPosition = 0;
            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 6, 6 } });

            _engine.RollDiceAndMovePlayer(tim.ID, gameState);

            var newBoardPosition = gameState.Players.First().BoardPosition;
            Assert.AreEqual(Math.Min(12, Math.Abs(12 - gameState.Tiles.Count()) /*Written with a min for when I expand the board, the test won't break.*/), newBoardPosition);
        }

        [TestMethod]
        public void Test1PlayerDoubleDiceRoll_2()
        {
            var tim = _engine.CreateNewPlayer("Bad-Luck-Tim");
            var gameState = _engine.CreateInitialGameState(new List<Player>() { tim });
            gameState.Players[0].BoardPosition = gameState.Tiles.Count() - 1;
            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 6, 6 } });

            _engine.RollDiceAndMovePlayer(tim.ID, gameState);

            var newBoardPosition = gameState.Players.First().BoardPosition;
            Assert.AreEqual(11, newBoardPosition);
        }

        [TestMethod]
        public void TestAuctionWith2Players_PlayerA_PlayerB_Place_Same_Bet_First_One_To_Bet_Wins()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 2 } })
                    .Repeat.Once();

            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);
            _engine.StartAuctionOnProperty(playerA.ID, gameState);
            _engine.BetOnAuction(playerB.ID, gameState, 11);
            _engine.BetOnAuction(playerA.ID, gameState, 11);

            Assert.AreEqual(gameState.Players[1].ID, ((PropertyTile)gameState.Tiles[3]).OwnerPlayerID);
        }

        [TestMethod]
        public void TestAuctionWith2Players_PlayerA_PlayerB_Place_Different_Bets()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 2 } })
                    .Repeat.Once();

            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);
            _engine.StartAuctionOnProperty(playerA.ID, gameState);
            _engine.BetOnAuction(playerB.ID, gameState, 20);
            _engine.BetOnAuction(playerA.ID, gameState, 11);

            Assert.AreEqual(gameState.Players[1].ID, ((PropertyTile)gameState.Tiles[3]).OwnerPlayerID);
            Assert.AreEqual(Constants.STARTING_MONEY - 20, gameState.Players[1].Money);
            Assert.AreEqual(Constants.STARTING_MONEY, gameState.Players[0].Money);
        }

        [TestMethod]
        public void TestAuctionWith2Players_PlayerA_PlayerB_Place_0_Bets_No_One_Wins_Property()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 2 } })
                    .Repeat.Once();

            gameState.Players[0].Money = 100;
            gameState.Players[1].Money = 1;

            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);
            _engine.StartAuctionOnProperty(playerA.ID, gameState);
            _engine.BetOnAuction(playerA.ID, gameState, 0);
            _engine.BetOnAuction(playerB.ID, gameState, 0);

            Assert.AreEqual(null, ((PropertyTile)gameState.Tiles[3]).OwnerPlayerID);
        }

        [TestMethod]
        public void TestAuctionWith2Players_PlayeB_Has_No_Money()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 2 } })
                    .Repeat.Once();

            gameState.Players[1].Money = 0;

            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);
            _engine.StartAuctionOnProperty(playerA.ID, gameState);

            Assert.AreEqual(gameState.Players[0].ID, ((PropertyTile)gameState.Tiles[3]).OwnerPlayerID);
            Assert.AreEqual(Constants.STARTING_MONEY - 1, gameState.Players[0].Money);
        }

        [TestMethod]
        public void CannotBuildOnPropertyWithMaxProperties()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA });

            var orangeTiles = gameState.Tiles
                .Where(x => x is ColorPropertyTile)
                .Select(x => x as ColorPropertyTile)
                .Where(x => x.Color == Constants.PropertyColor.Orange)
                .ToList();

            orangeTiles[0].OwnerPlayerID = playerA.ID;
            orangeTiles[1].OwnerPlayerID = playerA.ID;
            orangeTiles[2].OwnerPlayerID = playerA.ID;

            orangeTiles[0].BuildingCount = Constants.MAX_BUILDINGS_ON_PROPERTY - 1;
            orangeTiles[1].BuildingCount = Constants.MAX_BUILDINGS_ON_PROPERTY - 1;
            orangeTiles[2].BuildingCount = Constants.MAX_BUILDINGS_ON_PROPERTY;

            _engine.BuyAndBuildHouse(playerA.ID, gameState, gameState.Tiles.IndexOf(orangeTiles[2]));

            Assert.AreEqual(Constants.STARTING_MONEY, gameState.Players[0].Money);
            Assert.AreEqual(orangeTiles[2].BuildingCount, Constants.MAX_BUILDINGS_ON_PROPERTY);
        }

        [TestMethod]
        public void CanBuildOnPropertyWithOneLessThanMaxProperties()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA });

            var orangeTiles = gameState.Tiles
                .Where(x => x is ColorPropertyTile)
                .Select(x => x as ColorPropertyTile)
                .Where(x => x.Color == Constants.PropertyColor.Orange)
                .ToList();

            orangeTiles[0].OwnerPlayerID = playerA.ID;
            orangeTiles[1].OwnerPlayerID = playerA.ID;
            orangeTiles[2].OwnerPlayerID = playerA.ID;

            orangeTiles[0].BuildingCount = Constants.MAX_BUILDINGS_ON_PROPERTY - 1;
            orangeTiles[1].BuildingCount = Constants.MAX_BUILDINGS_ON_PROPERTY - 1;
            orangeTiles[2].BuildingCount = Constants.MAX_BUILDINGS_ON_PROPERTY - 1;

            _engine.BuyAndBuildHouse(playerA.ID, gameState, gameState.Tiles.IndexOf(orangeTiles[2]));

            Assert.AreEqual(Constants.STARTING_MONEY - orangeTiles[2].BuildingCost, gameState.Players[0].Money);
            Assert.AreEqual(orangeTiles[2].BuildingCount, Constants.MAX_BUILDINGS_ON_PROPERTY);
        }


        [TestMethod]
        public void CannotBuildUnevenlyOnPropertiesOfSameColor()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA });

            var orangeTiles = gameState.Tiles
                .Where(x => x is ColorPropertyTile)
                .Select(x => x as ColorPropertyTile)
                .Where(x => x.Color == Constants.PropertyColor.Orange)
                .ToList();

            orangeTiles[0].OwnerPlayerID = playerA.ID;
            orangeTiles[1].OwnerPlayerID = playerA.ID;
            orangeTiles[2].OwnerPlayerID = playerA.ID;

            orangeTiles[0].BuildingCount = 1;
            orangeTiles[1].BuildingCount = 1;
            orangeTiles[2].BuildingCount = 2;

            _engine.BuyAndBuildHouse(playerA.ID, gameState, gameState.Tiles.IndexOf(orangeTiles[2]));

            Assert.AreEqual(Constants.STARTING_MONEY, gameState.Players[0].Money);
            Assert.AreEqual(orangeTiles[2].BuildingCount, 2);
        }

        [TestMethod]
        public void CannotBuildUnevenlyOnPropertiesOfSameColor_2()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA });

            var orangeTiles = gameState.Tiles
                .Where(x => x is ColorPropertyTile)
                .Select(x => x as ColorPropertyTile)
                .Where(x => x.Color == Constants.PropertyColor.Orange)
                .ToList();

            orangeTiles[0].OwnerPlayerID = playerA.ID;
            orangeTiles[1].OwnerPlayerID = playerA.ID;
            orangeTiles[2].OwnerPlayerID = playerA.ID;

            orangeTiles[0].BuildingCount = 2;
            orangeTiles[1].BuildingCount = 1;
            orangeTiles[2].BuildingCount = 2;

            _engine.BuyAndBuildHouse(playerA.ID, gameState, gameState.Tiles.IndexOf(orangeTiles[2]));

            Assert.AreEqual(Constants.STARTING_MONEY, gameState.Players[0].Money);
            Assert.AreEqual(orangeTiles[2].BuildingCount, 2);
        }

        [TestMethod]
        public void CanBuildEvenlyOnPropertiesOfSameColor()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA });

            var orangeTiles = gameState.Tiles
                .Where(x => x is ColorPropertyTile)
                .Select(x => x as ColorPropertyTile)
                .Where(x => x.Color == Constants.PropertyColor.Orange)
                .ToList();

            orangeTiles[0].OwnerPlayerID = playerA.ID;
            orangeTiles[1].OwnerPlayerID = playerA.ID;
            orangeTiles[2].OwnerPlayerID = playerA.ID;

            orangeTiles[0].BuildingCount = 2;
            orangeTiles[1].BuildingCount = 2;
            orangeTiles[2].BuildingCount = 1;

            _engine.BuyAndBuildHouse(playerA.ID, gameState, gameState.Tiles.IndexOf(orangeTiles[2]));

            Assert.AreEqual(Constants.STARTING_MONEY - orangeTiles[2].BuildingCost, gameState.Players[0].Money);
            Assert.AreEqual(orangeTiles[2].BuildingCount, 2);
            Assert.AreEqual(orangeTiles[2].RentAmountPerBuilding[2], orangeTiles[2].Rent);
        }

        [TestMethod]
        public void PlayerGoesToJailForOverspeeding_Rolling_Doubles_Thrice()
        {
            var playerA = _engine.CreateNewPlayer("Austin");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 2, 2 } })
                    .Repeat.Times(1);
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 3, 3 } })
                    .Repeat.Times(1);
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 5, 5 } })
                    .Repeat.Times(1);

            var jail = gameState.Tiles.First(x => x is JailTile) as JailTile;

            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);
            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);
            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);

            Assert.IsTrue(gameState.Players[0].IsInJail);
            Assert.AreEqual(gameState.Tiles.IndexOf(jail), gameState.Players[0].BoardPosition);
        }

        [TestMethod]
        public void PlayerCannotEndTurnWithoutRollingAgainAfterDoubles()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 5, 5 } })
                    .Repeat.Times(3);

            var jail = gameState.Tiles.First(x => x is JailTile) as JailTile;

            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);
            _engine.FinishPlayerTurn(playerA.ID, gameState);

            Assert.AreEqual(playerA.ID, gameState.CurrentPlayer.ID);
        }

        [TestMethod]
        public void PlayerCanEndTurnAfterOverSpeeding()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 5, 5 } })
                    .Repeat.Times(3);

            var jail = gameState.Tiles.First(x => x is JailTile) as JailTile;

            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);
            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);
            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);
            _engine.FinishPlayerTurn(playerA.ID, gameState);

            Assert.AreEqual(playerB.ID, gameState.CurrentPlayer.ID);
            Assert.IsTrue(gameState.Players[0].IsInJail);
        }

        [TestMethod]
        public void PlayersTurnEndsAfterOverSpeeding()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 5, 5 } })
                    .Repeat.Times(3);

            var jail = gameState.Tiles.First(x => x is JailTile) as JailTile;

            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);
            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);
            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);
            _engine.BuyOutOfJail(playerA.ID, gameState);

            Assert.AreEqual(playerB.ID, gameState.CurrentPlayer.ID);
            Assert.IsTrue(gameState.Players[0].IsInJail);
        }

        [TestMethod]
        public void PlayerCanBuyOutOfJail()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 1 } })
                    .Repeat.Times(3);

            var jail = gameState.Tiles.First(x => x is JailTile) as JailTile;

            playerA.IsInJail = true;
            playerA.BoardPosition = gameState.Tiles.IndexOf(jail);

            _engine.BuyOutOfJail(playerA.ID, gameState);

            Assert.AreNotEqual(gameState.Tiles.IndexOf(jail), gameState.Players[0].BoardPosition);
            Assert.IsFalse(gameState.Players[0].IsInJail);
            Assert.AreEqual(Constants.STARTING_MONEY - Constants.GET_OUT_OF_JAIL_FEE, gameState.Players[0].Money);
        }

        [TestMethod]
        public void PlayerCannotBuyOutOfJailWhenJustArrived()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 1 } })
                    .Repeat.Times(3);

            var jail = gameState.Tiles.First(x => x is JailTile) as JailTile;

            gameState.CurrentPlayer.CurrentDiceRoll = _diceRoller.Roll(2);
            playerA.IsInJail = true;
            playerA.BoardPosition = gameState.Tiles.IndexOf(jail);

            _engine.BuyOutOfJail(playerA.ID, gameState);

            Assert.AreEqual(gameState.Tiles.IndexOf(jail), gameState.Players[0].BoardPosition);
            Assert.IsTrue(gameState.Players[0].IsInJail);
            Assert.AreEqual(Constants.STARTING_MONEY, gameState.Players[0].Money);
        }

        [TestMethod]
        public void PlayerCanRollADoubleOnceToGetOutOfJailFree()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                    .Return(new DiceRoll() { Dice = new int[] { 1, 1 } })
                    .Repeat.Times(3);

            var jail = gameState.Tiles.First(x => x is JailTile) as JailTile;

            playerA.IsInJail = true;
            playerA.BoardPosition = gameState.Tiles.IndexOf(jail);

            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);

            Assert.AreNotEqual(gameState.Tiles.IndexOf(jail), gameState.Players[0].BoardPosition);
            Assert.IsFalse(gameState.Players[0].IsInJail);
            Assert.AreEqual(Constants.STARTING_MONEY, gameState.Players[0].Money);
        }

        [TestMethod]
        public void PlayerCanRollADoubleUpToThreeTimesToGetOutOfJailFree()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });

            var jail = gameState.Tiles.First(x => x is JailTile) as JailTile;

            playerA.IsInJail = true;
            playerA.BoardPosition = gameState.Tiles.IndexOf(jail);

            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 1, 2 } })
                .Repeat.Once();
            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);
            _engine.FinishPlayerTurn(playerA.ID, gameState);

            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 3, 1 } })
                .Repeat.Once();
            _engine.RollDiceAndMovePlayer(playerB.ID, gameState);
            _engine.FinishPlayerTurn(playerB.ID, gameState);

            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 3, 1 } })
                .Repeat.Once();
            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);
            _engine.FinishPlayerTurn(playerA.ID, gameState);

            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 5, 1 } })
                .Repeat.Once();
            _engine.RollDiceAndMovePlayer(playerB.ID, gameState);
            _engine.FinishPlayerTurn(playerB.ID, gameState);

            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 3, 3 } })
                .Repeat.Once();

            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);

            Assert.AreNotEqual(gameState.Tiles.IndexOf(jail), gameState.Players[0].BoardPosition);
            Assert.IsFalse(gameState.Players[0].IsInJail);
            Assert.AreEqual(Constants.STARTING_MONEY, gameState.Players[0].Money);
        }

        [TestMethod]
        public void PlayerAutomaticallyPaysFeeToGetOutOfJailAfterThreeUnsuccessfulDoubleRollAttempts()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });

            var jail = gameState.Tiles.First(x => x is JailTile) as JailTile;

            playerA.IsInJail = true;
            playerA.BoardPosition = gameState.Tiles.IndexOf(jail);

            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 1, 2 } })
                .Repeat.Once();
            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);
            _engine.FinishPlayerTurn(playerA.ID, gameState);

            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 3, 1 } })
                .Repeat.Once();
            _engine.RollDiceAndMovePlayer(playerB.ID, gameState);
            _engine.FinishPlayerTurn(playerB.ID, gameState);

            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 3, 1 } })
                .Repeat.Once();
            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);
            _engine.FinishPlayerTurn(playerA.ID, gameState);

            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 5, 1 } })
                .Repeat.Once();
            _engine.RollDiceAndMovePlayer(playerB.ID, gameState);
            _engine.FinishPlayerTurn(playerB.ID, gameState);

            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 6, 5 } })
                .Repeat.Once();
            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);

            Assert.AreNotEqual(gameState.Tiles.IndexOf(jail), gameState.Players[0].BoardPosition);
            Assert.IsFalse(gameState.Players[0].IsInJail);
            Assert.AreEqual(Constants.STARTING_MONEY - Constants.GET_OUT_OF_JAIL_FEE, gameState.Players[0].Money);
        }

        [TestMethod]
        public void Bankrupcty_CurrentPlayer_SwitchesToNextPlayer_2_People_BankruptcyOutOfTurn()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 3, 1 } })
                .Repeat.Once();

            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);
            _engine.FinishPlayerTurn(playerA.ID, gameState);

            Assert.AreEqual(1, gameState.CurrentPlayerListIndex);

            _engine.DeclareBankruptcy(playerA.ID, gameState);

            Assert.AreEqual(0, gameState.CurrentPlayerListIndex);
            Assert.AreEqual(playerB.ID, gameState.CurrentPlayer.ID);
        }

        [TestMethod]
        public void Bankrupcty_CurrentPlayer_SwitchesToNextPlayer_2_People_BankruptcyOnTurn()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB });
            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 3, 1 } })
                .Repeat.Once();

            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);
            _engine.DeclareBankruptcy(playerA.ID, gameState);

            Assert.AreEqual(0, gameState.CurrentPlayerListIndex);
            Assert.AreEqual(playerB.ID, gameState.CurrentPlayer.ID);
        }

        [TestMethod]
        public void Bankrupcty_CurrentPlayer_StaysOnPlayer_3_People_BankruptcyOutOfTurn()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");
            var playerC = _engine.CreateNewPlayer("Tim");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB, playerC });
            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 3, 1 } })
                .Repeat.Once();

            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);

            _engine.DeclareBankruptcy(playerB.ID, gameState);

            Assert.AreEqual(0, gameState.CurrentPlayerListIndex);
            Assert.AreEqual(playerA.ID, gameState.CurrentPlayer.ID);
        }

        [TestMethod]
        public void Bankrupcty_CurrentPlayer_StaysOnPlayer_3_People_BankruptcyOnTurn()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");
            var playerC = _engine.CreateNewPlayer("Tim");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB, playerC });
            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 3, 1 } })
                .Repeat.Once();

            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);
            _engine.DeclareBankruptcy(playerA.ID, gameState);

            Assert.AreEqual(0, gameState.CurrentPlayerListIndex);
            Assert.AreEqual(playerB.ID, gameState.CurrentPlayer.ID);
        }


        [TestMethod]
        public void Bankrupcty_CurrentPlayer_StaysOnPlayer_3_People_BankruptcyOnTurn_2ndPerson()
        {
            var playerA = _engine.CreateNewPlayer("Austin");
            var playerB = _engine.CreateNewPlayer("Elias");
            var playerC = _engine.CreateNewPlayer("Tim");

            var gameState = _engine.CreateInitialGameState(new List<Player>() { playerA, playerB, playerC });
            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 3,1 } })
                .Repeat.Once();

            _engine.RollDiceAndMovePlayer(playerA.ID, gameState);
            _engine.FinishPlayerTurn(playerA.ID, gameState);

            Assert.AreEqual(1, gameState.CurrentPlayerListIndex);
            _engine.DeclareBankruptcy(playerB.ID, gameState);

            Assert.AreEqual(1, gameState.CurrentPlayerListIndex);
            Assert.AreEqual(playerC.ID, gameState.CurrentPlayer.ID);
        }

        private void PrintLogToCurrentTest()
        {
            foreach (var entry in _gameLog.DumpLog())
            {
                Debug.WriteLine(entry);
            }
        }
    }
}
