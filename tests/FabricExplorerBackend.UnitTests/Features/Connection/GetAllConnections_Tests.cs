using FabricExplorerBackend.Application.Connection.Contracts;
using FabricExplorerBackend.Commons.Paginations;
using FabricExplorerBackend.UnitTests.Features.Connection.Abstraction;
using Moq;
using Entities = FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.UnitTests.Features.Connection
{
    public class GetAllConnections_Tests : ConnectionServiceTestBase
    {
        [Fact]
        public async Task GetAllConnectionsAsync_WithValidPagination_ReturnsSuccess()
        {
            // Arrange
            var request = new PaginationRequest
            {
                Page = 1,
                PageSize = 20
            };

            var responses = new List<ConnectionResponse>
            {
                new(),
                new()
            };

            _connectionMapper
                .Setup(x => x.Map(It.IsAny<IEnumerable<Entities.Connection>>()))
                .Returns(responses);

            // Act
            var result = await _service.GetAllConnectionsAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);

            Assert.Equal(2, result.Data.PageInformation!.ItemsCount);
            Assert.Equal(2, result.Data.PageInformation!.TotalItems);
            Assert.Equal(1, result.Data.PageInformation!.TotalPages);

            _unitOfWork.Verify(x =>
                x.ConnectionRepository.GetAllAsync(
                    0,
                    20,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _connectionMapper.Verify(x =>
                x.Map(It.IsAny<IEnumerable<Entities.Connection>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllConnectionsAsync_Page2_UsesCorrectSkip()
        {
            // Arrange
            var request = new PaginationRequest
            {
                Page = 2,
                PageSize = 20
            };

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetAllAsync(
                    20,
                    20,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<Entities.Connection>(), 2));

            _connectionMapper
                .Setup(x => x.Map(It.IsAny<IEnumerable<Entities.Connection>>()))
                .Returns([]);

            // Act
            var result = await _service.GetAllConnectionsAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data.Items);

            Assert.Equal(0, result.Data.PageInformation!.ItemsCount);
            Assert.Equal(2, result.Data.PageInformation!.TotalItems);
            Assert.Equal(1, result.Data.PageInformation!.TotalPages);

            _unitOfWork.Verify(x =>
                x.ConnectionRepository.GetAllAsync(
                    20,
                    20,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ActiveConnection_WhenUserAndTargetExist_ReturnsSuccess()
        {
            // Arrange
            var user = new Entities.User
            {
                Id = _userId,
                ActiveConnectionId = _connectionId2
            };

            var targetConnection = new Entities.Connection
            {
                Id = _connectionId2
            };

            _unitOfWork
                .Setup(x => x.UserRepository.GetByIdAsync(
                    _userId,
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId2,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(targetConnection);

            _unitOfWork
                .Setup(x => x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.ActiveConnection(
                _connectionId2);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(
                "Active connection succeeded",
                result.Data!.Message);

            Assert.Equal(
                _connectionId2,
                user.ActiveConnectionId);

            _unitOfWork.Verify(x =>
                x.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task ActiveConnection_WhenUserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            _unitOfWork
                .Setup(x => x.UserRepository.GetByIdAsync(
                    _userId,
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entities.User?)null);

            // Act
            var result = await _service.ActiveConnection(
                _connectionId2);

            // Assert
            Assert.False(result.IsSuccess);

            _unitOfWork.Verify(x =>
                x.ConnectionRepository.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _unitOfWork.Verify(x =>
                x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ActiveConnection_WhenTargetConnectionDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var user = new Entities.User
            {
                Id = _userId
            };

            _unitOfWork
                .Setup(x => x.UserRepository.GetByIdAsync(
                    _userId,
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId2,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entities.Connection?)null);

            // Act
            var result = await _service.ActiveConnection(
                _connectionId2);

            // Assert
            Assert.False(result.IsSuccess);

            _unitOfWork.Verify(x =>
                x.SaveChangesAsync(),
                Times.Never);
        }
    }
}
