using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarcUp.Src.Business.Monitoring.Model;

namespace StarcUp.Src.Business.Game
{
    public interface IGameManager
    {
        LocalGameData LocalGameData { get; }
        Player[] Players { get; }
        IntPtr[] StartUnitAddressFromIndex { get; }
        IntPtr StartUnitAddress { get; }
        void GameInit();
        void GameExit();

    }
}
