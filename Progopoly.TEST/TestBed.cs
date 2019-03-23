using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Progopoly.Logic;
using System.Collections.Generic;
using Progopoly.Models;
using Rhino.Mocks;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using Progopoly.Models.Tiles;

namespace Progopoly.TEST
{
    [TestClass]
    public class TestBed
    {
        Engine _engine;
        IDiceRoller _diceRoller;
        GameLog _gameLog;
        Player austin;
        Player tim;
        GameState gameState;

        [TestInitialize]
        public void TestInitialize()
        {
            _diceRoller = MockRepository.Mock<IDiceRoller>();
            _gameLog = new GameLog();
            _engine = new Engine(_diceRoller, _gameLog);

            tim = _engine.CreateNewPlayer("Tim");
            austin = _engine.CreateNewPlayer("Austin");
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

            Debug.WriteLine($"{austin.Name} - {austin.ID} - money: ${austin.Money}");
            Debug.WriteLine($"{tim.Name}  - {tim.ID} - money: ${tim.Money}");
            Debug.WriteLine($"Owned properties: {string.Join(",", gameState.Tiles.Where(t => (t is PropertyTile) && (t as PropertyTile)?.OwnerPlayerID != Guid.Empty).Select(t => new { t.Name, Owner = (t as PropertyTile).OwnerPlayerID }))}");
        }

        [TestMethod]
        public void SimulateGameA()
        {
            gameState = _engine.CreateInitialGameState(new List<Player>() { tim, austin });
            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 1, 3 } })
                .Repeat.Once();

            _engine.RollDiceAndMovePlayer(tim.ID, gameState);
            _engine.FinishPlayerTurn(tim.ID, gameState);

            _diceRoller.Stub(x => x.Roll(2))
                .Return(new DiceRoll() { Dice = new int[] { 3, 1 } });

            _engine.RollDiceAndMovePlayer(austin.ID, gameState);
            _engine.FinishPlayerTurn(austin.ID, gameState);
        
            Assert.AreEqual(4, gameState.Players[1].BoardPosition);

            _engine.StartAuctionOnProperty(austin.ID, gameState);
            _engine.BetOnAuction(austin.ID, gameState, 11);
            _engine.BetOnAuction(tim.ID, gameState, 11);
            _engine.BetOnAuction(tim.ID, gameState, 30);
            _engine.BetOnAuction(austin.ID, gameState, 700);
            _engine.BetOnAuction(tim.ID, gameState, 991);


            _engine.RollDiceAndMovePlayer(tim.ID, gameState);
            _engine.FinishPlayerTurn(tim.ID, gameState);
            _engine.RollDiceAndMovePlayer(austin.ID, gameState);
            _engine.FinishPlayerTurn(austin.ID, gameState);
            _engine.FinishPlayerTurn(tim.ID, gameState);
        }
    }
}
