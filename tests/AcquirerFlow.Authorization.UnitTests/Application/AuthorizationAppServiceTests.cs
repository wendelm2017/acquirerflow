using AcquirerFlow.Authorization.Application.DTOs;
using AcquirerFlow.Authorization.Application.Services;
using AcquirerFlow.Authorization.Domain.Ports.Out;
using AcquirerFlow.Authorization.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace AcquirerFlow.Authorization.UnitTests.Application;

public class AuthorizationAppServiceTests
{
    private readonly Mock<ITransactionRepository> _repoMock;
    private readonly Mock<ICardTokenizer> _tokenizerMock;
    private readonly Mock<IFraudChecker> _fraudMock;
    private readonly Mock<IEventPublisher> _publisherMock;
    private readonly AuthorizationAppService _service;

    public AuthorizationAppServiceTests()
    {
        _repoMock = new Mock<ITransactionRepository>();
        _tokenizerMock = new Mock<ICardTokenizer>();
        _fraudMock = new Mock<IFraudChecker>();
        _publisherMock = new Mock<IEventPublisher>();

        _tokenizerMock
            .Setup(t => t.TokenizeAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CardToken.Create("tok_test123", "1234", "VISA"));

        _service = new AuthorizationAppService(
            _repoMock.Object,
            _tokenizerMock.Object,
            _fraudMock.Object,
            _publisherMock.Object);
    }

    private static AuthorizationRequestDto ValidRequest => new(
        "POS-001", Guid.NewGuid(), "4111111111111234", "VISA", 150m, "BRL", 1, "DEBIT");

    [Fact]
    public async Task Authorize_WhenApproved_ShouldReturnAuthorizedStatus()
    {
        _fraudMock.Setup(f => f.CheckAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new FraudCheckResult(true, null));

        var result = await _service.AuthorizeAsync(ValidRequest);

        result.Status.Should().Be("AUTHORIZED");
        result.AuthorizationCode.Should().NotBeNullOrEmpty();
        result.DeclinedReason.Should().BeNull();
        result.CardMasked.Should().Be("****-****-****-1234");
    }

    [Fact]
    public async Task Authorize_WhenFraudDetected_ShouldReturnDeclinedStatus()
    {
        _fraudMock.Setup(f => f.CheckAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new FraudCheckResult(false, "FRAUD_SUSPECTED"));

        var result = await _service.AuthorizeAsync(ValidRequest);

        result.Status.Should().Be("DECLINED");
        result.DeclinedReason.Should().Be("FRAUD_SUSPECTED");
        result.AuthorizationCode.Should().BeNull();
    }

    [Fact]
    public async Task Authorize_ShouldAlwaysTokenizeCard()
    {
        _fraudMock.Setup(f => f.CheckAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new FraudCheckResult(true, null));

        await _service.AuthorizeAsync(ValidRequest);

        _tokenizerMock.Verify(t => t.TokenizeAsync("4111111111111234", "VISA"), Times.Once);
    }

    [Fact]
    public async Task Authorize_WhenApproved_ShouldSaveAndPublish()
    {
        _fraudMock.Setup(f => f.CheckAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new FraudCheckResult(true, null));

        await _service.AuthorizeAsync(ValidRequest);

        _repoMock.Verify(r => r.SaveAsync(It.IsAny<AcquirerFlow.Authorization.Domain.Entities.Transaction>()), Times.Once);
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<AcquirerFlow.Contracts.Events.TransactionAuthorized>(), "transaction-authorized"), Times.Once);
    }

    [Fact]
    public async Task Authorize_WhenDeclined_ShouldSaveAndPublishDeclinedEvent()
    {
        _fraudMock.Setup(f => f.CheckAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new FraudCheckResult(false, "HIGH_VALUE_TRANSACTION"));

        await _service.AuthorizeAsync(ValidRequest);

        _repoMock.Verify(r => r.SaveAsync(It.IsAny<AcquirerFlow.Authorization.Domain.Entities.Transaction>()), Times.Once);
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<AcquirerFlow.Contracts.Events.TransactionDeclined>(), "transaction-declined"), Times.Once);
    }

    [Fact]
    public async Task Authorize_ShouldReturnCorrectAmount()
    {
        _fraudMock.Setup(f => f.CheckAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new FraudCheckResult(true, null));

        var result = await _service.AuthorizeAsync(ValidRequest);

        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("BRL");
    }
}
