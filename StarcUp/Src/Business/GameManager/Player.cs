using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarcUp.Business.Game
{
    public struct Player
    {
        public int PlayerIndex { get; set; }
        public int Mineral { get; set; }
        public int Gas { get; set; }
        public int SupplyUsed { get; set; }
        public int SupplyTotal { get; set; }
        public int FoodUsed => SupplyUsed / 2;
        public int FoodTotal => SupplyTotal / 2;
        public int FoodLeft => FoodTotal - FoodUsed;

        public void Init()
        {
            Mineral = 0;
            Gas = 0;
            SupplyUsed = 0;
            SupplyTotal = 0;

        }
    }
}
