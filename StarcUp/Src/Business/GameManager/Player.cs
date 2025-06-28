using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarcUp.Business.Units.Runtime.Models;

namespace StarcUp.Business.Game
{
    public class Player
    {
        public int PlayerIndex { get; set; }
        public int Mineral { get; set; }
        public int Gas { get; set; }
        public int SupplyUsed { get; set; }
        public int SupplyTotal { get; set; }
        public int FoodUsed => SupplyUsed / 2;
        public int FoodTotal => SupplyTotal / 2;
        public int FoodLeft => FoodTotal - FoodUsed;

        // 유닛 관련 필드들
        private readonly Unit[] _units;
        private readonly int _maxUnits;
        private int _unitCount;

        public Player()
        {
            _maxUnits = 3400; // 최대 유닛 수

            // 미리 할당된 배열로 메모리 재활용 (모든 Unit 인스턴스 미리 생성)
            _units = new Unit[_maxUnits];
            for (int i = 0; i < _maxUnits; i++)
                _units[i] = new Unit();
            _unitCount = 0;
        }

        public void Init()
        {
            Mineral = 0;
            Gas = 0;
            SupplyUsed = 0;
            SupplyTotal = 0;
        }

        // 유닛 관련 속성들 (외부 접근용)
        public int UnitCount => _unitCount;

        // 내부 유닛 배열 접근 (확장 메서드에서 사용)
        internal Unit[] GetPlayerUnits() => _units;
        internal int GetMaxUnits() => _maxUnits;
        internal void SetUnitCount(int count) => _unitCount = count;
    }

}
