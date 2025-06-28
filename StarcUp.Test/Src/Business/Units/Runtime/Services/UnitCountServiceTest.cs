using Xunit;
using FluentAssertions;
using Moq;
using StarcUp.Business.Units.Runtime.Services;
using StarcUp.Business.Units.Runtime.Adapters;
using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Types;

namespace StarcUp.Test.Src.Business.Units.Runtime.Services
{
    public class UnitCountServiceTest
    {
        private readonly Mock<IUnitCountAdapter> _mockAdapter;
        private readonly UnitCountService _service;

        public UnitCountServiceTest()
        {
            _mockAdapter = new Mock<IUnitCountAdapter>();
            _service = new UnitCountService(_mockAdapter.Object);
        }

        [Fact]
        public void Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var service = new UnitCountService(_mockAdapter.Object);

            // Assert
            service.IsRunning.Should().BeFalse();
            service.LastUpdateTime.Should().Be(default(DateTime));
        }

        [Fact]
        public void Initialize_WhenAdapterInitializeFails_ShouldReturnFalse()
        {
            // Arrange
            _mockAdapter.Setup(x => x.InitializeBaseAddress()).Returns(false);

            // Act
            var result = _service.Initialize();

            // Assert
            result.Should().BeFalse();
            _service.IsRunning.Should().BeFalse();
        }

        [Fact]
        public void Initialize_WhenAdapterLoadFails_ShouldReturnFalse()
        {
            // Arrange
            _mockAdapter.Setup(x => x.InitializeBaseAddress()).Returns(true);
            _mockAdapter.Setup(x => x.LoadAllUnitCounts()).Returns(false);

            // Act
            var result = _service.Initialize();

            // Assert
            result.Should().BeFalse();
            _service.IsRunning.Should().BeFalse();
        }

        [Fact]
        public void Initialize_WhenSuccessful_ShouldStartService()
        {
            // Arrange
            _mockAdapter.Setup(x => x.InitializeBaseAddress()).Returns(true);
            _mockAdapter.Setup(x => x.LoadAllUnitCounts()).Returns(true);
            _mockAdapter.Setup(x => x.GetAllUnitCountsToBuffer(It.IsAny<byte>(), It.IsAny<bool>()))
                       .Returns(new List<UnitCount>());

            // Act
            var result = _service.Initialize();

            // Assert
            result.Should().BeTrue();
            _service.IsRunning.Should().BeTrue();
            _service.LastUpdateTime.Should().BeAfter(DateTime.Now.AddSeconds(-1));

            // Cleanup
            _service.Stop();
        }

        [Fact]
        public void GetUnitCount_WithValidData_ShouldReturnCorrectCount()
        {
            // Arrange
            var testUnitCounts = new List<UnitCount>
            {
                new UnitCount(UnitType.ProtossZealot, 0, 5, 2)
            };

            _mockAdapter.Setup(x => x.InitializeBaseAddress()).Returns(true);
            _mockAdapter.Setup(x => x.LoadAllUnitCounts()).Returns(true);
            _mockAdapter.Setup(x => x.GetAllUnitCountsToBuffer(0, false))
                       .Returns(testUnitCounts);
            _mockAdapter.Setup(x => x.GetAllUnitCountsToBuffer(It.IsAny<byte>(), true))
                       .Returns(new List<UnitCount>());

            _service.Initialize();

            // Act
            var result = _service.GetUnitCount(UnitType.ProtossZealot, 0, false);

            // Assert
            result.Should().Be(5);

            // Cleanup
            _service.Stop();
        }

        [Fact]
        public void GetUnitCount_WithInvalidPlayerIndex_ShouldThrowException()
        {
            // Arrange
            _mockAdapter.Setup(x => x.InitializeBaseAddress()).Returns(true);
            _mockAdapter.Setup(x => x.LoadAllUnitCounts()).Returns(true);
            _mockAdapter.Setup(x => x.GetAllUnitCountsToBuffer(It.IsAny<byte>(), It.IsAny<bool>()))
                       .Returns(new List<UnitCount>());

            _service.Initialize();

            // Act & Assert
            _service.Invoking(s => s.GetUnitCount(UnitType.ProtossZealot, 8))
                   .Should().Throw<ArgumentOutOfRangeException>();

            // Cleanup
            _service.Stop();
        }

        [Fact]
        public void Stop_ShouldStopService()
        {
            // Arrange
            _mockAdapter.Setup(x => x.InitializeBaseAddress()).Returns(true);
            _mockAdapter.Setup(x => x.LoadAllUnitCounts()).Returns(true);
            _mockAdapter.Setup(x => x.GetAllUnitCountsToBuffer(It.IsAny<byte>(), It.IsAny<bool>()))
                       .Returns(new List<UnitCount>());

            _service.Initialize();

            // Act
            _service.Stop();

            // Assert
            _service.IsRunning.Should().BeFalse();
        }

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Arrange
            _mockAdapter.Setup(x => x.InitializeBaseAddress()).Returns(true);
            _mockAdapter.Setup(x => x.LoadAllUnitCounts()).Returns(true);
            _mockAdapter.Setup(x => x.GetAllUnitCountsToBuffer(It.IsAny<byte>(), It.IsAny<bool>()))
                       .Returns(new List<UnitCount>());

            _service.Initialize();

            // Act
            _service.Dispose();

            // Assert
            _service.IsRunning.Should().BeFalse();
            _service.Invoking(s => s.GetUnitCount(UnitType.ProtossZealot, 0))
                   .Should().Throw<ObjectDisposedException>();
        }
    }
}