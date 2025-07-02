using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarcUp.Business.Game
{
    public struct LocalGameData
    {
        public int GameTime { get; set; } // 게임 시간 (초 단위)
        public int LocalPlayerIndex { get; set; } // 로컬 플레이어 인덱스

    }
}
