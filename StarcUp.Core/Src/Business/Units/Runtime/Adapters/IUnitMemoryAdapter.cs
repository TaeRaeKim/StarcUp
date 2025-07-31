using StarcUp.Business.Units.Runtime.Models;
using System;
using System.Collections.Generic;

namespace StarcUp.Business.Units.Runtime.Adapters
{
    public interface IUnitMemoryAdapter : IDisposable
    {
        void SetUnitArrayBaseAddress(nint baseAddress);
        bool InitializeUnitArrayAddress();
        void InvalidateAddressCache();
        bool LoadAllUnits();
        bool UpdateUnits();
        
        IEnumerable<UnitRaw> GetAllRawUnits();
        IEnumerable<UnitRaw> GetPlayerRawUnits(int playerId);
        IEnumerable<UnitRaw> GetPlayerUnits(int playerId);
        int GetPlayerUnitsToBuffer(int playerId, Unit[] buffer, int maxCount);
        IEnumerable<UnitRaw> GetRawUnitsByType(ushort unitType);
        IEnumerable<UnitRaw> GetRawUnitsNearPosition(ushort x, ushort y, int radius);
        IEnumerable<(int Index, UnitRaw Current, UnitRaw Previous)> GetChangedRawUnits();
        
        int GetActiveUnitCount();
        bool IsRawUnitValid(UnitRaw unit);
    }
}