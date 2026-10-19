using FabricExplorerBackend.UnitTests.Features.Connection.Abstraction;
using Moq;
using Entities = FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.UnitTests.Features.Connection
{
    public class DeleteConnection_Tests : ConnectionServiceTestBase
    {
        [Fact]
        public async Task DeleteConnection_WhenConnectionExists_ReturnsSuccess()
        {
            // Arrange
            _unitOfWork
                .Setup(x => x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.DeleteConnection(_connectionId1);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(
                "Connection deleted successfully",
                result.Data!.Message);

            _unitOfWork.Verify(x =>
                x.ConnectionRepository.Delete(
                    It.Is<Entities.Connection>(c => c.Id == _connectionId1)),
                Times.Once);

            _unitOfWork.Verify(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteConnection_WhenConnectionDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    id,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entities.Connection?)null);

            // Act
            var result = await _service.DeleteConnection(id);

            // Assert
            Assert.False(result.IsSuccess);

            _unitOfWork.Verify(x =>
                x.ConnectionRepository.Delete(It.IsAny<Entities.Connection>()),
                Times.Never);

            _unitOfWork.Verify(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteConnection_WhenSaveChangesReturnsZero_ThrowsInvalidOperationException()
        {
            // Arrange
            _unitOfWork
                .Setup(x => x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.DeleteConnection(_connectionId1));
        }
    }
}
