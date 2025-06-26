using StarcUp.Business.MemoryService;
using StarcUp.Common.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarcUp.Business.MemoryService
{
    /// <summary>
    /// 메모리 작업의 미들웨어 역할을 하는 서비스 인터페이스
    /// - MemoryReader를 사용하여 복합적인 비즈니스 로직 제공
    /// - null/error 체크, 로깅, 캐싱, 재시도 등의 미들웨어 기능
    /// - 상위 레벨 서비스들이 사용하기 쉬운 고수준 API 제공
    /// </summary>
    public interface IMemoryService : IDisposable
    {
        event EventHandler<ProcessEventArgs> ProcessConnect;
        event EventHandler<ProcessEventArgs> ProcessDisconnect;

        bool IsConnected { get; }
        int ConnectedProcessId { get; }

        bool ConnectToProcess(int processId);
        void Disconnect();

        int ReadInt(nint address);
        float ReadFloat(nint address);
        double ReadDouble(nint address);
        byte ReadByte(nint address);
        short ReadShort(nint address);
        long ReadLong(nint address);
        bool ReadBool(nint address);
        nint ReadPointer(nint address);
        string ReadString(nint address, int maxLength = 256, Encoding encoding = null);

        T ReadStructure<T>(nint address) where T : struct;
        T[] ReadStructureArray<T>(nint address, int count) where T : struct;

        nint GetPebAddress();
        List<TebInfo> GetTebAddresses();
        nint GetStackStart(int threadIndex = 0);
        nint GetStackTop(int threadIndex = 0);

        bool FindModule(string moduleName, out ModuleInfo moduleInfo);
        ModuleInfo GetKernel32Module();
        ModuleInfo GetUser32Module();

        bool IsValidAddress(nint address);
        bool IsInModuleRange(nint address, string moduleName);

        void RefreshTebCache();
        void RefreshModuleCache();
        void RefreshAllCache();
    }
}