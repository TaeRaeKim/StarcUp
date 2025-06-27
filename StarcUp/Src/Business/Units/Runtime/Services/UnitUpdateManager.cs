using StarcUp.Business.Units.Runtime.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace StarcUp.Business.Units.Runtime.Services
{
    /// <summary>
    /// 여러 플레이어의 유닛 업데이트 서비스를 관리하는 매니저
    /// </summary>
    public class UnitUpdateManager : IDisposable
    {
        private readonly IUnitService _unitService;
        private readonly ConcurrentDictionary<byte, UnitUpdateService> _updateServices;
        private readonly ConcurrentDictionary<byte, List<Unit>> _latestUnits;
        private bool _disposed;

        public event EventHandler<UnitsUpdatedEventArgs> UnitsUpdated;

        public UnitUpdateManager(IUnitService unitService)
        {
            _unitService = unitService ?? throw new ArgumentNullException(nameof(unitService));
            _updateServices = new ConcurrentDictionary<byte, UnitUpdateService>();
            _latestUnits = new ConcurrentDictionary<byte, List<Unit>>();
            
        }

        /// <summary>
        /// 특정 플레이어의 유닛 업데이트 서비스 시작
        /// </summary>
        /// <param name="playerId">플레이어 ID (0-7)</param>
        public void StartPlayerUnitUpdates(byte playerId)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitUpdateManager));

            if (_updateServices.ContainsKey(playerId))
            {
                return;
            }

            var updateService = new UnitUpdateService(_unitService, playerId);
            updateService.UnitsUpdated += OnUnitsUpdated;
            
            if (_updateServices.TryAdd(playerId, updateService))
            {
                updateService.Start();
            }
            else
            {
                updateService.Dispose();
            }
        }

        /// <summary>
        /// 특정 플레이어의 유닛 업데이트 서비스 중지
        /// </summary>
        /// <param name="playerId">플레이어 ID</param>
        public void StopPlayerUnitUpdates(byte playerId)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitUpdateManager));

            if (_updateServices.TryRemove(playerId, out var updateService))
            {
                updateService.UnitsUpdated -= OnUnitsUpdated;
                updateService.Dispose();
                _latestUnits.TryRemove(playerId, out _);
            }
        }

        /// <summary>
        /// 현재 플레이어 (일반적으로 0번)의 업데이트 시작
        /// </summary>
        public void StartCurrentPlayerUpdates()
        {
            StartPlayerUnitUpdates(0);
        }

        /// <summary>
        /// 모든 플레이어의 업데이트 서비스 시작
        /// </summary>
        public void StartAllPlayerUpdates()
        {
            for (byte playerId = 0; playerId < 8; playerId++)
            {
                StartPlayerUnitUpdates(playerId);
            }
        }

        /// <summary>
        /// 모든 업데이트 서비스 중지
        /// </summary>
        public void StopAllUpdates()
        {
            var playerIds = _updateServices.Keys.ToList();
            foreach (var playerId in playerIds)
            {
                StopPlayerUnitUpdates(playerId);
            }
        }

        /// <summary>
        /// 특정 플레이어의 최신 유닛 목록 가져오기
        /// </summary>
        /// <param name="playerId">플레이어 ID</param>
        /// <returns>최신 유닛 목록</returns>
        public List<Unit> GetLatestPlayerUnits(byte playerId)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitUpdateManager));

            return _latestUnits.TryGetValue(playerId, out var units) ? units : new List<Unit>();
        }

        /// <summary>
        /// 현재 플레이어의 최신 유닛 목록 가져오기
        /// </summary>
        /// <returns>현재 플레이어의 유닛 목록</returns>
        public List<Unit> GetCurrentPlayerUnits()
        {
            return GetLatestPlayerUnits(0);
        }

        /// <summary>
        /// 업데이트 중인 플레이어 목록 반환
        /// </summary>
        /// <returns>업데이트 중인 플레이어 ID 목록</returns>
        public List<byte> GetActivePlayerIds()
        {
            return _updateServices.Keys.ToList();
        }

        /// <summary>
        /// 특정 플레이어의 업데이트 서비스가 실행 중인지 확인
        /// </summary>
        /// <param name="playerId">플레이어 ID</param>
        /// <returns>실행 중이면 true</returns>
        public bool IsPlayerUpdateActive(byte playerId)
        {
            return _updateServices.ContainsKey(playerId);
        }

        private void OnUnitsUpdated(object sender, UnitsUpdatedEventArgs e)
        {
            // 최신 유닛 목록 업데이트
            _latestUnits.AddOrUpdate(e.PlayerId, e.Units, (key, oldValue) => e.Units);
            
            // 이벤트 전파
            UnitsUpdated?.Invoke(this, e);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            StopAllUpdates();
            _disposed = true;
            
        }
    }
}