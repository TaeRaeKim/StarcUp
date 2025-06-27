using Moq;
using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Runtime.Services;
using StarcUp.Business.Units.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace StarcUp.Test.Business.Units.Runtime.Services
{
    public class UnitUpdateServiceTest
    {
        private Mock<IUnitService> _mockUnitService;
        private List<Unit> _testUnits;

        public UnitUpdateServiceTest()
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
            _mockUnitService.Setup(x => x.GetPlayerUnits(0))
                           .Returns(_testUnits);
            _mockUnitService.Setup(x => x.GetPlayerUnitsToBuffer(0, It.IsAny<Unit[]>(), It.IsAny<int>()))
                           .Returns((byte playerId, Unit[] buffer, int maxCount) => 
                           {
                               var units = _testUnits.Take(maxCount).ToArray();
                               for (int i = 0; i < units.Length && i < buffer.Length; i++)
                               {
                                   buffer[i] = units[i];
                               }
                               return units.Length;
                           });
        }

        [Fact]
        public void Constructor_ValidUnitService_ShouldInitialize()
        {
            // Act
            using var service = new UnitUpdateService(_mockUnitService.Object, 0);

            // Assert
            Assert.NotNull(service);
            Console.WriteLine("✅ UnitUpdateService 생성자 테스트 통과");
        }

        [Fact]
        public void Constructor_NullUnitService_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new UnitUpdateService(null, 0));
            Console.WriteLine("✅ null UnitService 예외 처리 테스트 통과");
        }

        [Fact]
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
            Assert.True(updateReceived);
            Assert.NotNull(receivedArgs);
            Assert.Equal(0, receivedArgs.PlayerId);
            Assert.Equal(2, receivedArgs.Units.Count);
            
            Console.WriteLine("✅ Start/Stop 기능 테스트 통과");
        }

        [Fact]
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
            Assert.NotNull(receivedArgs);
            Assert.Equal(0, receivedArgs.PlayerId);
            Assert.Equal(2, receivedArgs.Units.Count);
            
            var marine = receivedArgs.Units.First(u => u.UnitType == UnitType.TerranMarine);
            Assert.Equal(40u, marine.Health);
            Assert.Equal(100, marine.CurrentX);
            Assert.Equal(200, marine.CurrentY);

            Console.WriteLine("✅ 업데이트 데이터 정확성 테스트 통과");
        }

        [Fact]
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
            Assert.Equal(initialCount, updateCount);

            Console.WriteLine("✅ Dispose 기능 테스트 통과");
        }

        [Fact]
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

        [Fact]
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