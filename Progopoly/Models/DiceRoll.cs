namespace Progopoly.Models
{
    public class DiceRoll
    {
        public int[] Dice { get; set; } = new int[2];
        public bool IsDouble
        {
            get
            {
                return Dice[0] == Dice[1];
            }
        }

        public override string ToString()
        {
            return $"[{Dice[0]}],[{Dice[1]}] ";
        }
    }
}
