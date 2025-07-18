using System;
using System.Runtime.InteropServices;

namespace StarcUp.Business.Units.Runtime.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct UnitRaw
    {
        // 연결 리스트 포인터 (0x00-0x0F)
        public nint prevPointer;            // 0x00: 이전 유닛 포인터 (8바이트)
        public nint nextPointer;            // 0x08: 다음 유닛 포인터 (8바이트)

        // 생명력 (0x10-0x17)
        public uint health;                 // 0x10: 현재 HP (4바이트)
        public uint reserved1;              // 0x14: 패딩 (4바이트)

        // 스프라이트 포인터 (0x18-0x1F)
        public nint spritePointer;          // 0x18: 스프라이트 포인터 (8바이트)

        // 목적지 좌표 (0x20-0x23)
        public ushort destX;                // 0x20: 목적지 X 좌표 (2바이트)
        public ushort destY;                // 0x22: 목적지 Y 좌표 (2바이트)

        // 패딩 영역 (0x24-0x3F) - 28바이트
        public fixed byte padding1[28];

        // 현재 위치 (0x40-0x43)
        public ushort currentX;             // 0x40: 현재 X 좌표 (2바이트)
        public ushort currentY;             // 0x42: 현재 Y 좌표 (2바이트)

        // 패딩 영역 (0x44-0x4F) - 12바이트
        public fixed byte padding2[12];

        // 이동 속도 (0x50-0x5F)
        public uint maxVelocityX;           // 0x50: X축 최대 속도 (4바이트)
        public uint maxVelocityY;           // 0x54: Y축 최대 속도 (4바이트)
        public int velocityDeltaX;          // 0x58: X축 증감 (4바이트)
        public int velocityDeltaY;          // 0x5C: Y축 증감 (4바이트)

        // 패딩 영역 (0x60-0x67) - 8바이트
        public fixed byte padding3[8];

        // 플레이어 및 액션 정보 (0x68-0x6A)
        public byte playerIndex;            // 0x68: 플레이어 인덱스 (1바이트)
        public byte actionIndex;            // 0x69: 액션 인덱스 (1바이트)
        public byte actionState;            // 0x6A: 액션 상태 (1바이트)

        // 패딩 영역 (0x6B-0x70) - 6바이트
        public fixed byte padding4[6];

        // 공격 쿨다운 (0x71-0x72)
        public byte attackCooldown;         // 0x71: 공격 쿨다운 (1바이트)
        public byte attackCooldownRelated;  // 0x72: 공격 쿨다운 관련 (1바이트)

        // 패딩 영역 (0x73-0x87) - 21바이트
        public fixed byte padding5[21];

        // 쉴드 및 유닛 타입 (0x88-0x8F)
        public uint shield;                 // 0x88: 현재 쉴드 (4바이트)
        public ushort unitType;             // 0x8C: 유닛 타입 (2바이트)
        public ushort reserved2;            // 0x8E: 패딩 (2바이트)

        // 아군 연결 리스트 포인터 (0x90-0x9F)
        public nint prevAllyPointer;        // 0x90: 이전 아군 포인터 (8바이트)
        public nint nextAllyPointer;        // 0x98: 다음 아군 포인터 (8바이트)

        // 패딩 영역 (0xA0-0xDB) - 60바이트
        public fixed byte padding6[60];

        // 생산 큐 (0xDC-0xE7) - 12바이트
        public fixed ushort productionQueue[5];    // 0xDC-0xE5: 생산 큐[0-4] (5개 * 2바이트 = 10바이트)
        public ushort reserved3;            // 0xE6: 패딩 (2바이트)

        // 큐 인덱스 (0xE8)
        public byte productionQueueIndex;   // 0xE8: 생산 큐 인덱스 (1바이트)

        // 패딩 영역 (0xE9-0x111) - 41바이트
        public fixed byte padding7[41];

        // 타이머 (0x112-0x113)
        public ushort timer;                // 0x112: 타이머 (2바이트)

        // 패딩 (0x114)
        public byte reserved4;              // 0x114: 패딩 (1바이트)

        // 업그레이드 정보 (0x115, 0x119)
        public byte currentUpgrade;         // 0x115: 현재 업그레이드 (1바이트)

        // 패딩 (0x116-0x118) - 3바이트
        public fixed byte padding8[3];

        public byte currentUpgradeLevel;    // 0x119: 현재 업그레이드 레벨 (1바이트)

        // 패딩 (0x11A-0x12A) - 17바이트
        public fixed byte padding9[17];

        // 자원 채취 상태 (0x12B)
        public byte gatheringState;         // 0x12B: 자원 채취 상태 (1바이트)

        // 나머지 데이터 (0x12C-0x1E7) - 188바이트
        public fixed byte remainingData[188];

        public bool IsValid => unitType != 0 && playerIndex < 12;

        public bool IsAlive => health > 0;

        public bool IsBuilding => unitType >= 106 && unitType <= 202;

        public bool IsWorker => unitType == 7 || unitType == 41 || unitType == 45;

        public override bool Equals(object? obj)
        {
            if (obj is UnitRaw other)
            {
                return unitType == other.unitType &&
                       health == other.health &&
                       currentX == other.currentX &&
                       currentY == other.currentY &&
                       playerIndex == other.playerIndex &&
                       actionIndex == other.actionIndex;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(unitType, health, currentX, currentY, playerIndex, actionIndex);
        }

        public override string ToString()
        {
            return $"Unit[Type={unitType}, Player={playerIndex}, HP={health}, Pos=({currentX},{currentY})]";
        }
    }
}