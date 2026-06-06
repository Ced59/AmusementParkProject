using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Handlers;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Ports;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Queries;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.AttractionAccessConditionTypes.Handlers;

public sealed class ListAttractionAccessConditionTypeDefinitionsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenRepositoryReturnsDefinitions_ShouldWrapDefinitionsInSuccessResult()
    {
        CancellationToken cancellationToken = new CancellationTokenSource().Token;
        AttractionAccessConditionTypeDefinition[] definitions = new[]
        {
            new AttractionAccessConditionTypeDefinition { Key = "min-height" },
        };
        Mock<IAttractionAccessConditionTypeDefinitionRepository> repository = new Mock<IAttractionAccessConditionTypeDefinitionRepository>(MockBehavior.Strict);
        repository.Setup(item => item.GetAllAsync(true, cancellationToken)).ReturnsAsync(definitions);
        ListAttractionAccessConditionTypeDefinitionsQueryHandler handler = new ListAttractionAccessConditionTypeDefinitionsQueryHandler(repository.Object);

        ApplicationResult<IReadOnlyCollection<AttractionAccessConditionTypeDefinition>> result = await handler.HandleAsync(new ListAttractionAccessConditionTypeDefinitionsQuery(true), cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Same(definitions, result.Value);
        repository.VerifyAll();
    }
}
