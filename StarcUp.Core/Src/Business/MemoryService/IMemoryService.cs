using StarcUp.Business.MemoryService;
using StarcUp.Business.Units.Types;
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

        bool ReadMemoryIntoBuffer(nint address, byte[] buffer, int size);
        bool ReadStructureArrayIntoBuffer<T>(nint address, T[] buffer, int count) where T : unmanaged;

        nint GetPebAddress();
        List<TebInfo> GetTebAddresses();
        /// <summary>
        /// 지정된 스레드의 스택 상단 주소를 가져옵니다 (TEB + 0x08, 높은 주소)
        /// </summary>
        /// <param name="threadIndex">스레드 인덱스 (0부터 시작)</param>
        /// <returns>스택 상단 주소 (StackBase)</returns>
        nint GetStackTop(int threadIndex = 0);

        /// <summary>
        /// 지정된 스레드의 스택 하단 주소를 가져옵니다 (TEB + 0x10, 낮은 주소)
        /// </summary>
        /// <param name="threadIndex">스레드 인덱스 (0부터 시작)</param>
        /// <returns>스택 하단 주소 (StackLimit)</returns>
        nint GetStackBottom(int threadIndex = 0);

        /// <summary>
        /// 치트엔진 방식으로 스레드 스택 주소를 계산합니다 (kernel32 기반 검색)
        /// GameTime 등 특정 메모리 해킹 용도로 사용됩니다.
        /// </summary>
        /// <param name="threadIndex">스레드 인덱스 (0부터 시작)</param>
        /// <returns>치트엔진 방식 스택 주소</returns>
        nint GetThreadStackAddress(int threadIndex = 0);

        /// <summary>
        /// 캐싱된 베이스 포인터를 가져옵니다.
        /// 이 값은 ConnectToProcess 시점에 한 번 계산되어 캐싱됩니다.
        /// (ThreadStack0 - baseOffset에서 ReadPointer한 값)
        /// </summary>
        /// <returns>캐싱된 베이스 포인터</returns>
        nint GetBasePointer();

        bool FindModule(string moduleName, out ModuleInfo moduleInfo);
        ModuleInfo FindModule(string targetModuleName);
        ModuleInfo GetKernel32Module();
        ModuleInfo GetUser32Module();
        ModuleInfo GetStarCraftModule();

        bool IsValidAddress(nint address);
        bool IsInModuleRange(nint address, string moduleName);

        void RefreshTebCache();
        void RefreshModuleCache();
        void RefreshAllCache();

        void DebugAllModules();

        void DebugAllModulesCheatEngineStyle();
        
        void FindModulesByPattern(string searchPattern);

        int ReadLocalPlayerIndex();
        int ReadGameTime();
        
        // 플레이어 정보 관련 메서드
        RaceType ReadPlayerRace(int playerIndex);
        
        // 인구수 관련 메서드 (종족별)
        int ReadSupplyUsed(int playerIndex, RaceType race);
        int ReadSupplyMax(int playerIndex, RaceType race);
    }
}