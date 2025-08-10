using StarcUp.Business.Units.Runtime.Adapters;
using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Types;
using StarcUp.Common.Logging;
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
            LoggerHelper.Info("초기화 완료");
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

        public bool UpdateUnits()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitService));

            return _memoryAdapter.UpdateUnits();
        }

        public IEnumerable<Unit> GetAllUnits()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitService));

            return _memoryAdapter.GetAllRawUnits()
                .Select(raw => Unit.FromRaw(raw, 0)) // 메모리 주소를 알 수 없으므로 0 사용
                .Where(IsUnitValid);
        }

        public IEnumerable<Unit> GetPlayerUnits(int playerId)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitService));

            return _memoryAdapter.GetPlayerRawUnits(playerId)
                .Select(raw => Unit.FromRaw(raw, 0)) // 메모리 주소를 알 수 없으므로 0 사용
                .Where(IsUnitValid);
        }

        public int GetPlayerUnitsToBuffer(int playerId, Unit[] buffer, int maxCount)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitService));

            // UnitMemoryAdapter에서 직접 Unit 배열로 변환하여 반환 (유효성 검사 포함)
            return _memoryAdapter.GetPlayerUnitsToBuffer(playerId, buffer, maxCount);
        }

        public IEnumerable<Unit> GetUnitsByType(UnitType unitType)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitService));

            return _memoryAdapter.GetRawUnitsByType((ushort)unitType)
                .Select(raw => Unit.FromRaw(raw, 0)) // 메모리 주소를 알 수 없으므로 0 사용
                .Where(IsUnitValid);
        }

        public IEnumerable<Unit> GetUnitsNearPosition(ushort x, ushort y, int radius)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitService));

            return _memoryAdapter.GetRawUnitsNearPosition(x, y, radius)
                .Select(raw => Unit.FromRaw(raw, 0)) // 메모리 주소를 알 수 없으므로 0 사용
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
            LoggerHelper.Info("리소스 정리 완료");
        }
    }
}