using System;
using static Progopoly.Models.Constants;

namespace Progopoly.Models.Tiles
{
    public abstract class PropertyTile : Tile
    {
        public int Cost { get; set; }
        public Guid? OwnerPlayerID { get; set; }
        public bool IsMortgaged { get; set; }

        public int MortgageValue
        {
            get
            {
                return Convert.ToInt32(Math.Round(Cost * 0.5));
            }
        }

        public int RedeemValue
        {
            get
            {
                return Convert.ToInt32(Math.Round(MortgageValue * 1.1));
            }
        }
    }
}