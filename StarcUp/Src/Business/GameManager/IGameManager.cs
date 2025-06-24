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
