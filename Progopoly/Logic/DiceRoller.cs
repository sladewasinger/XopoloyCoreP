using Progopoly.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Progopoly.Logic
{
    public class DiceRoller : IDiceRoller
    {
        private int? _seed;
        private static Random _random;

        public int[] MyProperty { get; set; }

        public DiceRoller(int? seed = null)
        {
            _seed = seed;
            _random = _seed != null ? new Random((int)_seed) : new Random();
        }

        public DiceRoll Roll(int numDice)
        {
            var diceRoll = new DiceRoll();

            for (int i = 0; i < numDice; i++)
                diceRoll.Dice[i] = _random.Next(6) + 1;

            return diceRoll;
        }
    }
}