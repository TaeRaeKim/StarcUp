using System;
using System.Collections.Generic;

namespace StarcUp.Business.UnitManager
{
    public interface IUnitManager : IDisposable
    {
        void SetUnitArrayBaseAddress(nint baseAddress);
        bool LoadAllUnits();
        bool RefreshUnits();

        IEnumerable<Unit> GetAllUnits();
        IEnumerable<Unit> GetPlayerUnits(byte playerId);
        IEnumerable<Unit> GetUnitsByType(ushort unitType);
        IEnumerable<Unit> GetUnitsNearPosition(ushort x, ushort y, int radius);
        IEnumerable<(int Index, Unit Current, Unit Previous)> GetChangedUnits();

        int GetActiveUnitCount();
        bool IsUnitValid(Unit unit);
    }
}