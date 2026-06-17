using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Contact.Commands;
using AmusementPark.Application.Features.Contact.Contracts;
using AmusementPark.Application.Features.Contact.Handlers;
using AmusementPark.Application.Features.Contact.Ports;
using AmusementPark.Core.Domain.Contact;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Contact.Handlers;

public sealed class SubmitContactGrievanceCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenMessageContainsMarkup_ShouldRejectWithoutPersisting()
    {
        Mock<IContactGrievanceRepository> repository = new Mock<IContactGrievanceRepository>(MockBehavior.Strict);
        SubmitContactGrievanceCommandHandler handler = new SubmitContactGrievanceCommandHandler(repository.Object);

        ApplicationResult<ContactGrievanceSubmissionResult> result = await handler.HandleAsync(new SubmitContactGrievanceCommand(
            new ContactGrievanceSubmission("Bonjour <script>alert(1)</script>", null, "fr", "127.0.0.1", "test")));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "contact.grievance.invalid");
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenHoneypotIsFilled_ShouldReturnSuccessWithoutPersisting()
    {
        Mock<IContactGrievanceRepository> repository = new Mock<IContactGrievanceRepository>(MockBehavior.Strict);
        SubmitContactGrievanceCommandHandler handler = new SubmitContactGrievanceCommandHandler(repository.Object);

        ApplicationResult<ContactGrievanceSubmissionResult> result = await handler.HandleAsync(new SubmitContactGrievanceCommand(
            new ContactGrievanceSubmission("Message valide pour un humain", "https://spam.example", "fr", "127.0.0.1", "test")));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value!.Accepted);
        Assert.Null(result.Value.SubmittedAtUtc);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenIpSubmittedTooOften_ShouldRejectBeforePersisting()
    {
        Mock<IContactGrievanceRepository> repository = new Mock<IContactGrievanceRepository>(MockBehavior.Strict);
        repository
            .Setup(repo => repo.CountRecentByIpAsync("127.0.0.1", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        SubmitContactGrievanceCommandHandler handler = new SubmitContactGrievanceCommandHandler(repository.Object);

        ApplicationResult<ContactGrievanceSubmissionResult> result = await handler.HandleAsync(new SubmitContactGrievanceCommand(
            new ContactGrievanceSubmission("Message valide pour le cahier de doleances", null, "fr", "127.0.0.1", "test")));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "contact.grievance.too-many-submissions");
        repository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenSubmissionIsValid_ShouldNormalizeAndPersist()
    {
        DateTime createdAtUtc = new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Utc);
        Mock<IContactGrievanceRepository> repository = new Mock<IContactGrievanceRepository>(MockBehavior.Strict);
        repository
            .Setup(repo => repo.CountRecentByIpAsync("127.0.0.1", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        repository
            .Setup(repo => repo.CreateAsync(It.IsAny<ContactGrievance>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContactGrievance grievance, CancellationToken _) =>
            {
                grievance.Id = "grievance-1";
                grievance.CreatedAtUtc = createdAtUtc;
                return grievance;
            });
        SubmitContactGrievanceCommandHandler handler = new SubmitContactGrievanceCommandHandler(repository.Object);

        ApplicationResult<ContactGrievanceSubmissionResult> result = await handler.HandleAsync(new SubmitContactGrievanceCommand(
            new ContactGrievanceSubmission("  Une idee utile pour ameliorer le site.\r\nMerci.  ", null, "fr-FR", "127.0.0.1", new string('a', 300))));

        Assert.True(result.IsSuccess);
        Assert.Equal(createdAtUtc, result.Value!.SubmittedAtUtc);
        repository.Verify(repo => repo.CreateAsync(It.Is<ContactGrievance>(grievance =>
            grievance.Message == "Une idee utile pour ameliorer le site.\nMerci." &&
            grievance.LanguageCode == "fr" &&
            grievance.IpAddress == "127.0.0.1" &&
            grievance.UserAgent != null &&
            grievance.UserAgent.Length == 256), It.IsAny<CancellationToken>()), Times.Once);
        repository.VerifyAll();
    }
}
