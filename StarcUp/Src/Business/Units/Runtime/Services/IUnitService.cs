using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Types;
using System;
using System.Collections.Generic;

namespace StarcUp.Business.Units.Runtime.Services
{
    public interface IUnitService : IDisposable
    {
        void SetUnitArrayBaseAddress(nint baseAddress);
        bool InitializeUnitArrayAddress();
        void InvalidateAddressCache();
        bool LoadAllUnits();
        bool RefreshUnits();

        IEnumerable<Unit> GetAllUnits();
        IEnumerable<Unit> GetPlayerUnits(int playerId);
        int GetPlayerUnitsToBuffer(int playerId, Unit[] buffer, int maxCount);
        IEnumerable<Unit> GetUnitsByType(UnitType unitType);
        IEnumerable<Unit> GetUnitsNearPosition(ushort x, ushort y, int radius);
        IEnumerable<Unit> GetAliveUnits();
        IEnumerable<Unit> GetBuildingUnits();
        IEnumerable<Unit> GetWorkerUnits();
        IEnumerable<Unit> GetHeroUnits();

        int GetActiveUnitCount();
        bool IsUnitValid(Unit unit);
    }
}