using Progopoly.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static Progopoly.Models.Constants;

namespace Progopoly.Models
{
    public class Player
    {
        private int _boardPosition;

        public Guid ID { get; set; }
        public string Name { get; set; }
        public int Money { get; set; }
        public int BoardPosition
        {
            get
            {
                return _boardPosition;
            }
            set
            {
                WasDirectMovement = false;
                PrevBoardPosition = _boardPosition;
                _boardPosition = value;
            }
        }
        public int PrevBoardPosition { get; set; }
        public bool WasDirectMovement { get; set; }
        public int NumDoublesRolledInARow { get; set; }
        public DiceRoll CurrentDiceRoll { get; set; }
        public int TurnOrder { get; set; }
        public bool IsInJail { get; set; }
        public int RollsWhileInJail { get; set; }
        public PlayerColor Color { get; set; }
        public bool HasGetOutOfJailFreeCard { get; set; }
        public int CurrentTurnElapsedSeconds { get; set; }
    }
}