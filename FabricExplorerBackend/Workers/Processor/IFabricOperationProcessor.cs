namespace FabricExplorerBackend.Workers.Processor
{
    public interface IFabricOperationProcessor
    {
        /// <summary>
        /// Một lượt xử lý: tìm các operation chưa xong và đã đến hạn poll, rồi cập nhật trạng thái từ Fabric.
        /// Lỗi của từng operation được ghi log và không làm hỏng các operation còn lại.
        /// </summary>
        Task ProcessNotDoneOperationsAsync(CancellationToken cancellationToken = default);
    }
}
