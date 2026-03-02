using AcquirerFlow.Authorization.Application.DTOs;
using AcquirerFlow.Authorization.Domain.Entities;
using AcquirerFlow.Authorization.Domain.Ports.Out;
using AcquirerFlow.Authorization.Domain.ValueObjects;
using AcquirerFlow.Contracts.Events;

namespace AcquirerFlow.Authorization.Application.Services;

public class AuthorizationAppService
{
    private readonly ITransactionRepository _repository;
    private readonly ICardTokenizer _tokenizer;
    private readonly IFraudChecker _fraudChecker;
    private readonly IEventPublisher _eventPublisher;

    public AuthorizationAppService(
        ITransactionRepository repository,
        ICardTokenizer tokenizer,
        IFraudChecker fraudChecker,
        IEventPublisher eventPublisher)
    {
        _repository = repository;
        _tokenizer = tokenizer;
        _fraudChecker = fraudChecker;
        _eventPublisher = eventPublisher;
    }

    public async Task<AuthorizationResponseDto> AuthorizeAsync(AuthorizationRequestDto request)
    {
        // 1. Tokeniza o cartão (PCI DSS — nunca trafega PAN internamente)
        var cardToken = await _tokenizer.TokenizeAsync(request.CardNumber, request.CardBrand);

        // 2. Cria a transação no domínio
        var money = Money.Create(request.Amount, request.Currency);
        var transaction = Transaction.Create(
            request.ExternalId,
            request.MerchantId,
            cardToken,
            money,
            request.Installments,
            request.Type);

        // 3. Verifica fraude
        var fraudResult = await _fraudChecker.CheckAsync(
            request.MerchantId, request.Amount, cardToken.Token);

        if (!fraudResult.IsApproved)
        {
            // Transação recusada por fraude
            transaction.Decline(fraudResult.Reason ?? "FRAUD_SUSPECTED");

            await _repository.SaveAsync(transaction);

            await _eventPublisher.PublishAsync(
                new TransactionDeclined(
                    transaction.Id,
                    transaction.MerchantId,
                    cardToken.Token,
                    transaction.Amount.Amount,
                    transaction.DeclinedReason!,
                    DateTime.UtcNow),
                "transaction-declined");

            return MapToResponse(transaction);
        }

        // 4. Autoriza a transação
        var authCode = GenerateAuthorizationCode();
        transaction.Authorize(authCode);

        // 5. Persiste
        await _repository.SaveAsync(transaction);

        // 6. Publica evento no Kafka
        await _eventPublisher.PublishAsync(
            new TransactionAuthorized(
                transaction.Id,
                transaction.MerchantId,
                cardToken.Token,
                cardToken.Brand,
                transaction.Amount.Amount,
                transaction.Amount.Currency,
                transaction.Installments,
                transaction.Type,
                authCode,
                transaction.AuthorizedAt!.Value),
            "transaction-authorized");

        return MapToResponse(transaction);
    }

    private static AuthorizationResponseDto MapToResponse(Transaction tx) =>
        new(
            tx.Id,
            tx.Status.ToString(),
            tx.AuthorizationCode,
            tx.DeclinedReason,
            tx.Card.MaskedDisplay,
            tx.Amount.Amount,
            tx.Amount.Currency,
            DateTime.UtcNow);

    private static string GenerateAuthorizationCode() =>
        Random.Shared.Next(100000, 999999).ToString();
}
