using StarcUp.Business.InGameStateMonitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarcUp.Business.Game
{
    public class GameManager : IGameManager
    {

        iInGameDetector _pointerMonitorService;

        public GameManager(iInGameDetector pointerMonitorService)
        {
            _pointerMonitorService = pointerMonitorService;
        }

        public LocalGameData LocalGameData { get; private set; } = new LocalGameData();
        public Player[] Players { get; private set; } = new Player[8]
        {
            new Player { PlayerIndex = 0 },
            new Player { PlayerIndex = 1 },
            new Player { PlayerIndex = 2 },
            new Player { PlayerIndex = 3 },
            new Player { PlayerIndex = 4 },
            new Player { PlayerIndex = 5 },
            new Player { PlayerIndex = 6 },
            new Player { PlayerIndex = 7 }
        };
        public nint[] StartUnitAddressFromIndex { get; private set; } = Array.Empty<nint>();
        public nint StartUnitAddress { get; private set; } = 0;
        public void GameInit()
        {
            // 객체 초기화
            Array.ForEach(Players, player => player.Init());
            
            // 메모리 매핑 및 모니터링

            // 오버레이 활성화
        }
        public void GameExit()
        {
            // 오버레이 비활성화

            // 모니터링 종료

        }
    }
}
