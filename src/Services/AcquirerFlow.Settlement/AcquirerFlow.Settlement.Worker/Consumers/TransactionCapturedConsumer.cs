using AcquirerFlow.Contracts.Events;
using AcquirerFlow.Settlement.Application.Services;
using MassTransit;

namespace AcquirerFlow.Settlement.Worker.Consumers;

public class TransactionCapturedConsumer : IConsumer<TransactionCaptured>
{
    private readonly SettlementService _settlementService;
    private readonly ILogger<TransactionCapturedConsumer> _logger;

    public TransactionCapturedConsumer(SettlementService settlementService, ILogger<TransactionCapturedConsumer> logger)
    {
        _settlementService = settlementService;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<TransactionCaptured> context)
    {
        var msg = context.Message;
        _logger.LogInformation("[SETTLEMENT] Received capture: TxId={TxId} Amount={Amount} Merchant={MerchantId}",
            msg.TransactionId, msg.CapturedAmount, msg.MerchantId);

        _settlementService.AccumulateCapture(msg);

        return Task.CompletedTask;
    }
}
