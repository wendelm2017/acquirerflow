using AcquirerFlow.Capture.Domain.Entities;
using AcquirerFlow.Capture.Domain.Ports.Out;
using AcquirerFlow.Contracts.Events;

namespace AcquirerFlow.Capture.Application.Services;

public class CaptureService
{
    private readonly ICaptureRepository _repository;
    private readonly Action<TransactionCaptured>? _onCaptured;

    public CaptureService(ICaptureRepository repository, Action<TransactionCaptured>? onCaptured = null)
    {
        _repository = repository;
        _onCaptured = onCaptured;
    }

    public async Task<CapturedTransaction> ProcessAuthorizationAsync(TransactionAuthorized @event)
    {
        if (await _repository.ExistsAsync(@event.TransactionId))
        {
            var existing = await _repository.GetByTransactionIdAsync(@event.TransactionId);
            Console.WriteLine($"[CAPTURE] Transaction {@event.TransactionId} already captured. Skipping.");
            return existing!;
        }

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

        Console.WriteLine($"[CAPTURE] Transaction {@event.TransactionId} captured: {@event.Currency} {@event.Amount:N2}");

        // Publica evento pro Settlement via RabbitMQ
        var capturedEvent = new TransactionCaptured(
            captured.OriginalTransactionId,
            captured.MerchantId,
            captured.CardToken,
            captured.CardBrand,
            captured.CapturedAmount,
            captured.Currency,
            captured.Installments,
            captured.Type,
            captured.AuthorizationCode,
            captured.CapturedAt);

        _onCaptured?.Invoke(capturedEvent);

        return captured;
    }
}
