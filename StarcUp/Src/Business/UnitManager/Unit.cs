using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace StarcUp.Src.Business.UnitManager
{
    public struct Unit
    {
        // 연결 리스트 포인터 (0x00-0x0F)
        public nint prevPointer;            // 0x00: 이전 유닛 포인터
        public nint nextPointer;            // 0x08: 다음 유닛 포인터

        // 생명력 (0x10-0x17)
        public uint health;                   // 0x10: 현재 HP (4바이트)
        public uint reserved1;                // 0x14: 패딩

        // 스프라이트 포인터 (0x18-0x1F)
        public nint spritePointer;          // 0x18: 스프라이트 포인터

        // 목적지 좌표 (0x20-0x23)
        public ushort destX;                  // 0x20: 목적지 X 좌표
        public ushort destY;                  // 0x22: 목적지 Y 좌표

        // 패딩 영역 (0x24-0x3F)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)]
        public byte[] padding1;               // 0x24-0x3F: 패딩

        // 현재 위치 (0x40-0x43)
        public ushort currentX;               // 0x40: 현재 X 좌표
        public ushort currentY;               // 0x42: 현재 Y 좌표

        // 패딩 영역 (0x44-0x4F)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] padding2;               // 0x44-0x4F: 패딩

        // 이동 속도 (0x50-0x5F)
        public uint maxVelocityX;             // 0x50: X축 최대 속도
        public uint maxVelocityY;             // 0x54: Y축 최대 속도
        public int velocityDeltaX;            // 0x58: X축 증감
        public int velocityDeltaY;            // 0x5C: Y축 증감

        // 패딩 영역 (0x60-0x67)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] padding3;               // 0x60-0x67: 패딩

        // 플레이어 및 액션 정보 (0x68-0x72)
        public byte playerIndex;              // 0x68: 플레이어 인덱스
        public byte actionIndex;              // 0x69: 액션 인덱스 (3=move, 6=move, 10=attack)
        public byte actionState;              // 0x6A: 액션 상태
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] padding4;               // 0x6B-0x70: 패딩

        // 공격 쿨다운 (0x71-0x72)
        public byte attackCooldown;           // 0x71: 공격 쿨다운
        public byte attackCooldownRelated;    // 0x72: 공격 쿨다운 관련

        // 패딩 영역 (0x73-0x87)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 21)]
        public byte[] padding5;               // 0x73-0x87: 패딩

        // 쉴드 및 유닛 타입 (0x88-0x8F)
        public uint shield;                   // 0x88: 현재 쉴드
        public ushort unitType;               // 0x8C: 유닛 타입
        public ushort reserved2;              // 0x8E: 패딩

        // 아군 연결 리스트 포인터 (0x90-0x9F)
        public nint prevAllyPointer;        // 0x90: 이전 아군 포인터
        public nint nextAllyPointer;        // 0x98: 다음 아군 포인터

        // 패딩 영역 (0xA0-0xDB)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
        public byte[] padding6;               // 0xA0-0xDB: 패딩

        // 생산 큐 (0xDC-0xE7)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public ushort[] productionQueue;      // 0xDC-0xE5: 생산 큐[0-4] (5개 * 2바이트 = 10바이트)
        public ushort reserved3;              // 0xE6: 패딩

        // 큐 인덱스 (0xE8)
        public byte productionQueueIndex;     // 0xE8: 생산 큐 인덱스

        // 패딩 영역 (0xE9-0x111)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 41)]
        public byte[] padding7;               // 0xE9-0x111: 패딩

        // 타이머 (0x112-0x113)
        public ushort timer;                  // 0x112: 타이머

        // 패딩 (0x114)
        public byte reserved4;                // 0x114: 패딩

        // 업그레이드 정보 (0x115, 0x119)
        public byte currentUpgrade;           // 0x115: 현재 업그레이드
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] padding8;               // 0x116-0x118: 패딩
        public byte currentUpgradeLevel;      // 0x119: 현재 업그레이드 레벨

        // 나머지 데이터 (0x11A-0x1E7)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 206)]
        public byte[] remainingData;          // 0x11A-0x1E7: 나머지 데이터 (206바이트)
    }
}
