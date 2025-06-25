namespace StarcUp.Src.Business.Game
{
    public interface IGameManager
    {
        LocalGameData LocalGameData { get; }

        Player[] Players { get; }
        nint[] StartUnitAddressFromIndex { get; }
        nint StartUnitAddress { get; }
        void GameInit();
        void GameExit();

    }
}
