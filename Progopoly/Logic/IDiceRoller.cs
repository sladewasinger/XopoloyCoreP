using Progopoly.Models;

namespace Progopoly.Logic
{
    public interface IDiceRoller
    {
        DiceRoll Roll(int numDice);
    }
}