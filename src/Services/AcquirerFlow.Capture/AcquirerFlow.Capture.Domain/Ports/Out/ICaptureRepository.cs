using AcquirerFlow.Capture.Domain.Entities;

namespace AcquirerFlow.Capture.Domain.Ports.Out;

public interface ICaptureRepository
{
    Task SaveAsync(CapturedTransaction transaction);
    Task<CapturedTransaction?> GetByTransactionIdAsync(Guid transactionId);
    Task<bool> ExistsAsync(Guid transactionId);
}
