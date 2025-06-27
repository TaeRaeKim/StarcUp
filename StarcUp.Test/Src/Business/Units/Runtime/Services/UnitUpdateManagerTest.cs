using Moq;
using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Runtime.Services;
using StarcUp.Business.Units.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace StarcUp.Test.Business.Units.Runtime.Services
{
    public class UnitUpdateManagerTest
    {
        private Mock<IUnitService> _mockUnitService;
        private List<Unit> _player0Units;
        private List<Unit> _player1Units;

        public UnitUpdateManagerTest()
        {
            _mockUnitService = new Mock<IUnitService>();
            
            // Player 0의 테스트 유닛
            _player0Units = new List<Unit>
            {
                new Unit 
                { 
                    UnitType = UnitType.TerranMarine, 
                    PlayerIndex = 0, 
                    Health = 40, 
                    CurrentX = 100, 
                    CurrentY = 200 
                },
                new Unit 
                { 
                    UnitType = UnitType.TerranFirebat, 
                    PlayerIndex = 0, 
                    Health = 50, 
                    CurrentX = 150, 
                    CurrentY = 250 
                }
            };

            // Player 1의 테스트 유닛
            _player1Units = new List<Unit>
            {
                new Unit 
                { 
                    UnitType = UnitType.ProtossZealot, 
                    PlayerIndex = 1, 
                    Health = 100, 
                    CurrentX = 300, 
                    CurrentY = 400 
                }
            };

            // Mock 설정
            _mockUnitService.Setup(x => x.GetPlayerUnits(0))
                           .Returns(_player0Units);
            _mockUnitService.Setup(x => x.GetPlayerUnitsToBuffer(0, It.IsAny<Unit[]>(), It.IsAny<int>()))
                           .Returns((byte playerId, Unit[] buffer, int maxCount) => 
                           {
                               var units = _player0Units.Take(maxCount).ToArray();
                               for (int i = 0; i < units.Length && i < buffer.Length; i++)
                               {
                                   buffer[i] = units[i];
                               }
                               return units.Length;
                           });
            
            _mockUnitService.Setup(x => x.GetPlayerUnits(1))
                           .Returns(_player1Units);
            _mockUnitService.Setup(x => x.GetPlayerUnitsToBuffer(1, It.IsAny<Unit[]>(), It.IsAny<int>()))
                           .Returns((byte playerId, Unit[] buffer, int maxCount) => 
                           {
                               var units = _player1Units.Take(maxCount).ToArray();
                               for (int i = 0; i < units.Length && i < buffer.Length; i++)
                               {
                                   buffer[i] = units[i];
                               }
                               return units.Length;
                           });

            // 다른 플레이어들은 빈 목록 반환
            for (byte i = 2; i < 8; i++)
            {
                _mockUnitService.Setup(x => x.GetPlayerUnits(i))
                               .Returns(new List<Unit>());
                _mockUnitService.Setup(x => x.GetPlayerUnitsToBuffer(i, It.IsAny<Unit[]>(), It.IsAny<int>()))
                               .Returns(0);
            }
        }

        [Fact]
        public void Constructor_ValidUnitService_ShouldInitialize()
        {
            // Act
            using var manager = new UnitUpdateManager(_mockUnitService.Object);

            // Assert
            Assert.NotNull(manager);
            Assert.Equal(0, manager.GetActivePlayerIds().Count);
            Console.WriteLine("✅ UnitUpdateManager 생성자 테스트 통과");
        }

        [Fact]
        public void Constructor_NullUnitService_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new UnitUpdateManager(null));
            Console.WriteLine("✅ null UnitService 예외 처리 테스트 통과");
        }

        [Fact]
        public async Task StartCurrentPlayerUpdates_ShouldStartPlayer0()
        {
            // Arrange
            using var manager = new UnitUpdateManager(_mockUnitService.Object);
            var updateReceived = false;

            manager.UnitsUpdated += (sender, args) =>
            {
                if (args.PlayerId == 0)
                    updateReceived = true;
            };

            // Act
            manager.StartCurrentPlayerUpdates();
            await Task.Delay(150);

            // Assert
            Assert.True(updateReceived);
            Assert.True(manager.IsPlayerUpdateActive(0));
            Assert.Equal(1, manager.GetActivePlayerIds().Count);

            var currentUnits = manager.GetCurrentPlayerUnits();
            Assert.Equal(2, currentUnits.Count);

            manager.StopPlayerUnitUpdates(0);
            Console.WriteLine("✅ 현재 플레이어 업데이트 시작 테스트 통과");
        }

        [Fact]
        public async Task StartPlayerUnitUpdates_SpecificPlayer_ShouldWork()
        {
            // Arrange
            using var manager = new UnitUpdateManager(_mockUnitService.Object);
            var player1UpdateReceived = false;

            manager.UnitsUpdated += (sender, args) =>
            {
                if (args.PlayerId == 1)
                    player1UpdateReceived = true;
            };

            // Act
            manager.StartPlayerUnitUpdates(1);
            await Task.Delay(150);

            // Assert
            Assert.True(player1UpdateReceived);
            Assert.True(manager.IsPlayerUpdateActive(1));

            var player1Units = manager.GetLatestPlayerUnits(1);
            Assert.Equal(1, player1Units.Count);
            Assert.Equal(UnitType.ProtossZealot, player1Units[0].UnitType);

            manager.StopPlayerUnitUpdates(1);
            Console.WriteLine("✅ 특정 플레이어 업데이트 시작 테스트 통과");
        }

        [Fact]
        public async Task StartAllPlayerUpdates_ShouldStartAllPlayers()
        {
            // Arrange
            using var manager = new UnitUpdateManager(_mockUnitService.Object);

            // Act
            manager.StartAllPlayerUpdates();
            await Task.Delay(200);

            // Assert
            var activePlayerIds = manager.GetActivePlayerIds();
            Assert.Equal(8, activePlayerIds.Count);

            for (byte i = 0; i < 8; i++)
            {
                Assert.True(manager.IsPlayerUpdateActive(i));
            }

            // Player 0과 1만 유닛이 있어야 함
            Assert.Equal(2, manager.GetLatestPlayerUnits(0).Count);
            Assert.Equal(1, manager.GetLatestPlayerUnits(1).Count);
            Assert.Equal(0, manager.GetLatestPlayerUnits(2).Count);

            manager.StopAllUpdates();
            Console.WriteLine("✅ 모든 플레이어 업데이트 시작 테스트 통과");
        }

        [Fact]
        public void StopPlayerUnitUpdates_ShouldStopSpecificPlayer()
        {
            // Arrange
            using var manager = new UnitUpdateManager(_mockUnitService.Object);
            manager.StartPlayerUnitUpdates(0);
            manager.StartPlayerUnitUpdates(1);

            // Act
            manager.StopPlayerUnitUpdates(0);

            // Assert
            Assert.False(manager.IsPlayerUpdateActive(0));
            Assert.True(manager.IsPlayerUpdateActive(1));
            Assert.Equal(1, manager.GetActivePlayerIds().Count);

            manager.StopAllUpdates();
            Console.WriteLine("✅ 특정 플레이어 업데이트 중지 테스트 통과");
        }

        [Fact]
        public void StopAllUpdates_ShouldStopAllPlayers()
        {
            // Arrange
            using var manager = new UnitUpdateManager(_mockUnitService.Object);
            manager.StartPlayerUnitUpdates(0);
            manager.StartPlayerUnitUpdates(1);
            manager.StartPlayerUnitUpdates(2);

            // Act
            manager.StopAllUpdates();

            // Assert
            Assert.Equal(0, manager.GetActivePlayerIds().Count);
            Assert.False(manager.IsPlayerUpdateActive(0));
            Assert.False(manager.IsPlayerUpdateActive(1));
            Assert.False(manager.IsPlayerUpdateActive(2));

            Console.WriteLine("✅ 모든 업데이트 중지 테스트 통과");
        }

        [Fact]
        public void GetLatestPlayerUnits_NoUpdatesStarted_ShouldReturnEmptyList()
        {
            // Arrange
            using var manager = new UnitUpdateManager(_mockUnitService.Object);

            // Act
            var units = manager.GetLatestPlayerUnits(0);

            // Assert
            Assert.NotNull(units);
            Assert.Equal(0, units.Count);

            Console.WriteLine("✅ 업데이트 미시작 시 빈 목록 반환 테스트 통과");
        }

        [Fact] 
        public void StartPlayerUnitUpdates_SamePlayerTwice_ShouldNotDuplicate()
        {
            // Arrange
            using var manager = new UnitUpdateManager(_mockUnitService.Object);

            // Act
            manager.StartPlayerUnitUpdates(0);
            manager.StartPlayerUnitUpdates(0); // 중복 호출

            // Assert
            Assert.Equal(1, manager.GetActivePlayerIds().Count);
            Assert.True(manager.IsPlayerUpdateActive(0));

            manager.StopAllUpdates();
            Console.WriteLine("✅ 중복 플레이어 시작 방지 테스트 통과");
        }

        [Fact]
        public void Dispose_ShouldStopAllUpdatesAndCleanup()
        {
            // Arrange
            var manager = new UnitUpdateManager(_mockUnitService.Object);
            manager.StartPlayerUnitUpdates(0);
            manager.StartPlayerUnitUpdates(1);

            // Act
            manager.Dispose();

            // Assert - Dispose 후에는 모든 업데이트가 중지되어야 함
            Assert.Equal(0, manager.GetActivePlayerIds().Count);

            Console.WriteLine("✅ Dispose 기능 테스트 통과");
        }
    }
}