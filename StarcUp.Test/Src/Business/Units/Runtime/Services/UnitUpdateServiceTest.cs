using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Runtime.Services;
using StarcUp.Business.Units.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StarcUp.Test.Business.Units.Runtime.Services
{
    [TestClass]
    public class UnitUpdateServiceTest
    {
        private Mock<IUnitService> _mockUnitService;
        private List<Unit> _testUnits;

        [TestInitialize]
        public void Setup()
        {
            _mockUnitService = new Mock<IUnitService>();
            
            // 테스트용 유닛 생성
            _testUnits = new List<Unit>
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
                    UnitType = UnitType.TerranSiegeTankTankMode, 
                    PlayerIndex = 0, 
                    Health = 150, 
                    CurrentX = 300, 
                    CurrentY = 400 
                }
            };

            // Mock 설정
            _mockUnitService.Setup(x => x.GetPlayerUnitsUsingAllyPointer(0))
                           .Returns(_testUnits);
            _mockUnitService.Setup(x => x.GetPlayerUnits(0))
                           .Returns(_testUnits);
        }

        [TestMethod]
        public void Constructor_ValidUnitService_ShouldInitialize()
        {
            // Act
            using var service = new UnitUpdateService(_mockUnitService.Object, 0);

            // Assert
            Assert.IsNotNull(service);
            Console.WriteLine("✅ UnitUpdateService 생성자 테스트 통과");
        }

        [TestMethod]
        public void Constructor_NullUnitService_ShouldThrowException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new UnitUpdateService(null, 0));
            Console.WriteLine("✅ null UnitService 예외 처리 테스트 통과");
        }

        [TestMethod]
        public async Task StartAndStop_ShouldWorkCorrectly()
        {
            // Arrange
            using var service = new UnitUpdateService(_mockUnitService.Object, 0);
            var updateReceived = false;
            UnitsUpdatedEventArgs receivedArgs = null;

            service.UnitsUpdated += (sender, args) =>
            {
                updateReceived = true;
                receivedArgs = args;
            };

            // Act
            service.Start();
            await Task.Delay(200); // 100ms 업데이트 주기보다 길게 대기
            service.Stop();

            // Assert
            Assert.IsTrue(updateReceived, "업데이트 이벤트가 발생해야 함");
            Assert.IsNotNull(receivedArgs, "이벤트 인자가 null이 아니어야 함");
            Assert.AreEqual(0, receivedArgs.PlayerId, "플레이어 ID가 일치해야 함");
            Assert.AreEqual(2, receivedArgs.Units.Count, "유닛 수가 일치해야 함");
            
            Console.WriteLine("✅ Start/Stop 기능 테스트 통과");
        }

        [TestMethod]
        public async Task UnitsUpdated_ShouldContainCorrectData()
        {
            // Arrange
            using var service = new UnitUpdateService(_mockUnitService.Object, 0);
            UnitsUpdatedEventArgs receivedArgs = null;

            service.UnitsUpdated += (sender, args) =>
            {
                receivedArgs = args;
            };

            // Act
            service.Start();
            await Task.Delay(150);
            service.Stop();

            // Assert
            Assert.IsNotNull(receivedArgs);
            Assert.AreEqual(0, receivedArgs.PlayerId);
            Assert.AreEqual(2, receivedArgs.Units.Count);
            
            var marine = receivedArgs.Units.First(u => u.UnitType == UnitType.TerranMarine);
            Assert.AreEqual(40, marine.Health);
            Assert.AreEqual(100, marine.CurrentX);
            Assert.AreEqual(200, marine.CurrentY);

            Console.WriteLine("✅ 업데이트 데이터 정확성 테스트 통과");
        }

        [TestMethod]
        public void Dispose_ShouldStopUpdatesAndCleanup()
        {
            // Arrange
            var service = new UnitUpdateService(_mockUnitService.Object, 0);
            var updateCount = 0;

            service.UnitsUpdated += (sender, args) => updateCount++;

            // Act
            service.Start();
            service.Dispose();

            // Assert - Dispose 후에는 업데이트가 중지되어야 함
            var initialCount = updateCount;
            Thread.Sleep(200);
            Assert.AreEqual(initialCount, updateCount, "Dispose 후에는 더 이상 업데이트가 발생하지 않아야 함");

            Console.WriteLine("✅ Dispose 기능 테스트 통과");
        }

        [TestMethod]
        public void Start_WhenAlreadyRunning_ShouldNotStartAgain()
        {
            // Arrange
            using var service = new UnitUpdateService(_mockUnitService.Object, 0);

            // Act
            service.Start();
            service.Start(); // 두 번째 호출

            // Assert - 두 번째 Start 호출이 문제없이 처리되어야 함
            Thread.Sleep(100);
            service.Stop();

            Console.WriteLine("✅ 중복 Start 호출 처리 테스트 통과");
        }

        [TestMethod]
        public void Stop_WhenNotRunning_ShouldNotThrow()
        {
            // Arrange
            using var service = new UnitUpdateService(_mockUnitService.Object, 0);

            // Act & Assert - 실행 중이 아닐 때 Stop 호출해도 예외가 발생하지 않아야 함
            service.Stop();

            Console.WriteLine("✅ 미실행 상태에서 Stop 호출 테스트 통과");
        }
    }
}