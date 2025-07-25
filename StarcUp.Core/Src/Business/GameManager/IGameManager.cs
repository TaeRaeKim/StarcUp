using System;

namespace StarcUp.Business.Game
{
    public interface IGameManager : IDisposable
    {
        LocalGameData LocalGameData { get; }

        Player[] Players { get; }
        nint[] StartUnitAddressFromIndex { get; }
        nint StartUnitAddress { get; }
        void GameInit();
        void GameExit();
    }
}
