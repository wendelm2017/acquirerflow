using AcquirerFlow.Capture.Domain.Entities;
using AcquirerFlow.Capture.Domain.Ports.Out;
using AcquirerFlow.Contracts.Events;

namespace AcquirerFlow.Capture.Application.Services;

public class CaptureService
{
    private readonly ICaptureRepository _repository;

    public CaptureService(ICaptureRepository repository)
    {
        _repository = repository;
    }

    public async Task<CapturedTransaction> ProcessAuthorizationAsync(TransactionAuthorized @event)
    {
        // Idempotência: se já capturou, retorna o existente
        if (await _repository.ExistsAsync(@event.TransactionId))
        {
            var existing = await _repository.GetByTransactionIdAsync(@event.TransactionId);
            Console.WriteLine($"[CAPTURE] Transaction {@event.TransactionId} already captured. Skipping.");
            return existing!;
        }

        // Auto-capture: captura o valor total autorizado
        var captured = CapturedTransaction.CreateFromAuthorization(
            @event.TransactionId,
            @event.MerchantId,
            @event.CardToken,
            @event.CardBrand,
            @event.Amount,
            @event.Currency,
            @event.Installments,
            @event.Type,
            @event.AuthorizationCode,
            @event.AuthorizedAt);

        await _repository.SaveAsync(captured);

        Console.WriteLine($"[CAPTURE] Transaction {@event.TransactionId} captured: {captured.Currency} {captured.CapturedAmount:N2}");

        return captured;
    }
}
