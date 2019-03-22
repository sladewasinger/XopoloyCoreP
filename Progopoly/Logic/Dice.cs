using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Progopoly.Logic
{
    public class Dice
    {
        private int _sides = 6;
        public int CurrentValue { get; set; }

        public Dice(int diceSides=6)
        {
            _sides = diceSides;
        }
    }
}
