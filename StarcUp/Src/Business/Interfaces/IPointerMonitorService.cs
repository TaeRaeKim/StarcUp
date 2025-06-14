﻿using StarcUp.Business.Models;
using StarcUp.Common.Events;
using System;

namespace StarcUp.Business.Interfaces
{
    /// <summary>
    /// 포인터 모니터링 서비스 인터페이스
    /// </summary>
    public interface IPointerMonitorService : IDisposable
    {
        event EventHandler<PointerEventArgs> ValueChanged;

        bool IsMonitoring { get; }
        PointerValue CurrentValue { get; }

        void StartMonitoring(int processId);
        void StopMonitoring();
    }
}