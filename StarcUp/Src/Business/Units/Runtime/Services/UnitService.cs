using StarcUp.Business.Units.Runtime.Adapters;
using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StarcUp.Business.Units.Runtime.Services
{
    public class UnitService : IUnitService
    {
        private readonly IUnitMemoryAdapter _memoryAdapter;
        private bool _disposed;

        public UnitService(IUnitMemoryAdapter memoryAdapter)
        {
            _memoryAdapter = memoryAdapter ?? throw new ArgumentNullException(nameof(memoryAdapter));
            Console.WriteLine("UnitService 초기화 완료");
        }

        public void SetUnitArrayBaseAddress(nint baseAddress)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitService));

            _memoryAdapter.SetUnitArrayBaseAddress(baseAddress);
        }

        public bool InitializeUnitArrayAddress()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitService));

            return _memoryAdapter.InitializeUnitArrayAddress();
        }

        public void InvalidateAddressCache()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitService));

            _memoryAdapter.InvalidateAddressCache();
        }

        public bool LoadAllUnits()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitService));

            return _memoryAdapter.LoadAllUnits();
        }

        public bool RefreshUnits()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitService));

            return _memoryAdapter.RefreshUnits();
        }

        public IEnumerable<Unit> GetAllUnits()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitService));

            return _memoryAdapter.GetAllRawUnits()
                .Select(Unit.FromRaw)
                .Where(IsUnitValid);
        }

        public IEnumerable<Unit> GetPlayerUnits(byte playerId)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitService));

            return _memoryAdapter.GetPlayerRawUnits(playerId)
                .Select(Unit.FromRaw)
                .Where(IsUnitValid);
        }

        public IEnumerable<Unit> GetUnitsByType(UnitType unitType)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitService));

            return _memoryAdapter.GetRawUnitsByType((ushort)unitType)
                .Select(Unit.FromRaw)
                .Where(IsUnitValid);
        }

        public IEnumerable<Unit> GetUnitsNearPosition(ushort x, ushort y, int radius)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitService));

            return _memoryAdapter.GetRawUnitsNearPosition(x, y, radius)
                .Select(Unit.FromRaw)
                .Where(IsUnitValid);
        }

        public IEnumerable<Unit> GetAliveUnits()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitService));

            return GetAllUnits().Where(u => u.IsAlive);
        }

        public IEnumerable<Unit> GetBuildingUnits()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitService));

            return GetAllUnits().Where(u => u.IsBuilding);
        }

        public IEnumerable<Unit> GetWorkerUnits()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitService));

            return GetAllUnits().Where(u => u.IsWorker);
        }

        public IEnumerable<Unit> GetHeroUnits()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitService));

            return GetAllUnits().Where(u => u.IsHero);
        }

        public int GetActiveUnitCount()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitService));

            return _memoryAdapter.GetActiveUnitCount();
        }

        public bool IsUnitValid(Unit unit)
        {
            return unit.IsValid;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _memoryAdapter?.Dispose();
            _disposed = true;
            Console.WriteLine("UnitService 리소스 정리 완료");
        }
    }
}