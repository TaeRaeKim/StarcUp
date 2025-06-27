using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Runtime.Services;
using StarcUp.Business.Units.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StarcUp.Test.Business.Units.Runtime.Services
{
    [TestClass]
    public class UnitUpdateManagerTest
    {
        private Mock<IUnitService> _mockUnitService;
        private List<Unit> _player0Units;
        private List<Unit> _player1Units;

        [TestInitialize]
        public void Setup()
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
            _mockUnitService.Setup(x => x.GetPlayerUnitsUsingAllyPointer(0))
                           .Returns(_player0Units);
            _mockUnitService.Setup(x => x.GetPlayerUnits(0))
                           .Returns(_player0Units);
            
            _mockUnitService.Setup(x => x.GetPlayerUnitsUsingAllyPointer(1))
                           .Returns(_player1Units);
            _mockUnitService.Setup(x => x.GetPlayerUnits(1))
                           .Returns(_player1Units);

            // 다른 플레이어들은 빈 목록 반환
            for (byte i = 2; i < 8; i++)
            {
                _mockUnitService.Setup(x => x.GetPlayerUnitsUsingAllyPointer(i))
                               .Returns(new List<Unit>());
                _mockUnitService.Setup(x => x.GetPlayerUnits(i))
                               .Returns(new List<Unit>());
            }
        }

        [TestMethod]
        public void Constructor_ValidUnitService_ShouldInitialize()
        {
            // Act
            using var manager = new UnitUpdateManager(_mockUnitService.Object);

            // Assert
            Assert.IsNotNull(manager);
            Assert.AreEqual(0, manager.GetActivePlayerIds().Count);
            Console.WriteLine("✅ UnitUpdateManager 생성자 테스트 통과");
        }

        [TestMethod]
        public void Constructor_NullUnitService_ShouldThrowException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new UnitUpdateManager(null));
            Console.WriteLine("✅ null UnitService 예외 처리 테스트 통과");
        }

        [TestMethod]
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
            Assert.IsTrue(updateReceived, "Player 0 업데이트가 발생해야 함");
            Assert.IsTrue(manager.IsPlayerUpdateActive(0), "Player 0이 활성화되어야 함");
            Assert.AreEqual(1, manager.GetActivePlayerIds().Count, "활성 플레이어가 1명이어야 함");

            var currentUnits = manager.GetCurrentPlayerUnits();
            Assert.AreEqual(2, currentUnits.Count, "현재 플레이어 유닛이 2개여야 함");

            manager.StopPlayerUnitUpdates(0);
            Console.WriteLine("✅ 현재 플레이어 업데이트 시작 테스트 통과");
        }

        [TestMethod]
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
            Assert.IsTrue(player1UpdateReceived, "Player 1 업데이트가 발생해야 함");
            Assert.IsTrue(manager.IsPlayerUpdateActive(1), "Player 1이 활성화되어야 함");

            var player1Units = manager.GetLatestPlayerUnits(1);
            Assert.AreEqual(1, player1Units.Count, "Player 1 유닛이 1개여야 함");
            Assert.AreEqual(UnitType.ProtossZealot, player1Units[0].UnitType, "유닛 타입이 일치해야 함");

            manager.StopPlayerUnitUpdates(1);
            Console.WriteLine("✅ 특정 플레이어 업데이트 시작 테스트 통과");
        }

        [TestMethod]
        public async Task StartAllPlayerUpdates_ShouldStartAllPlayers()
        {
            // Arrange
            using var manager = new UnitUpdateManager(_mockUnitService.Object);

            // Act
            manager.StartAllPlayerUpdates();
            await Task.Delay(200);

            // Assert
            var activePlayerIds = manager.GetActivePlayerIds();
            Assert.AreEqual(8, activePlayerIds.Count, "8명의 플레이어가 모두 활성화되어야 함");

            for (byte i = 0; i < 8; i++)
            {
                Assert.IsTrue(manager.IsPlayerUpdateActive(i), $"Player {i}이 활성화되어야 함");
            }

            // Player 0과 1만 유닛이 있어야 함
            Assert.AreEqual(2, manager.GetLatestPlayerUnits(0).Count, "Player 0은 2개 유닛");
            Assert.AreEqual(1, manager.GetLatestPlayerUnits(1).Count, "Player 1은 1개 유닛");
            Assert.AreEqual(0, manager.GetLatestPlayerUnits(2).Count, "Player 2는 0개 유닛");

            manager.StopAllUpdates();
            Console.WriteLine("✅ 모든 플레이어 업데이트 시작 테스트 통과");
        }

        [TestMethod]
        public void StopPlayerUnitUpdates_ShouldStopSpecificPlayer()
        {
            // Arrange
            using var manager = new UnitUpdateManager(_mockUnitService.Object);
            manager.StartPlayerUnitUpdates(0);
            manager.StartPlayerUnitUpdates(1);

            // Act
            manager.StopPlayerUnitUpdates(0);

            // Assert
            Assert.IsFalse(manager.IsPlayerUpdateActive(0), "Player 0이 비활성화되어야 함");
            Assert.IsTrue(manager.IsPlayerUpdateActive(1), "Player 1은 여전히 활성화되어야 함");
            Assert.AreEqual(1, manager.GetActivePlayerIds().Count, "활성 플레이어가 1명이어야 함");

            manager.StopAllUpdates();
            Console.WriteLine("✅ 특정 플레이어 업데이트 중지 테스트 통과");
        }

        [TestMethod]
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
            Assert.AreEqual(0, manager.GetActivePlayerIds().Count, "모든 플레이어가 비활성화되어야 함");
            Assert.IsFalse(manager.IsPlayerUpdateActive(0), "Player 0이 비활성화되어야 함");
            Assert.IsFalse(manager.IsPlayerUpdateActive(1), "Player 1이 비활성화되어야 함");
            Assert.IsFalse(manager.IsPlayerUpdateActive(2), "Player 2가 비활성화되어야 함");

            Console.WriteLine("✅ 모든 업데이트 중지 테스트 통과");
        }

        [TestMethod]
        public void GetLatestPlayerUnits_NoUpdatesStarted_ShouldReturnEmptyList()
        {
            // Arrange
            using var manager = new UnitUpdateManager(_mockUnitService.Object);

            // Act
            var units = manager.GetLatestPlayerUnits(0);

            // Assert
            Assert.IsNotNull(units, "결과가 null이 아니어야 함");
            Assert.AreEqual(0, units.Count, "업데이트가 시작되지 않았으면 빈 목록이어야 함");

            Console.WriteLine("✅ 업데이트 미시작 시 빈 목록 반환 테스트 통과");
        }

        [TestMethod] 
        public void StartPlayerUnitUpdates_SamePlayerTwice_ShouldNotDuplicate()
        {
            // Arrange
            using var manager = new UnitUpdateManager(_mockUnitService.Object);

            // Act
            manager.StartPlayerUnitUpdates(0);
            manager.StartPlayerUnitUpdates(0); // 중복 호출

            // Assert
            Assert.AreEqual(1, manager.GetActivePlayerIds().Count, "중복 시작해도 1개만 있어야 함");
            Assert.IsTrue(manager.IsPlayerUpdateActive(0), "Player 0이 활성화되어야 함");

            manager.StopAllUpdates();
            Console.WriteLine("✅ 중복 플레이어 시작 방지 테스트 통과");
        }

        [TestMethod]
        public void Dispose_ShouldStopAllUpdatesAndCleanup()
        {
            // Arrange
            var manager = new UnitUpdateManager(_mockUnitService.Object);
            manager.StartPlayerUnitUpdates(0);
            manager.StartPlayerUnitUpdates(1);

            // Act
            manager.Dispose();

            // Assert - Dispose 후에는 모든 업데이트가 중지되어야 함
            Assert.AreEqual(0, manager.GetActivePlayerIds().Count, "Dispose 후 모든 플레이어가 비활성화되어야 함");

            Console.WriteLine("✅ Dispose 기능 테스트 통과");
        }
    }
}