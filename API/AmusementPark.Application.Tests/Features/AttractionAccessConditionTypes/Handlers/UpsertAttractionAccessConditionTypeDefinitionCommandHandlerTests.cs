using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Commands;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Contracts;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Handlers;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Ports;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.AttractionAccessConditionTypes.Handlers;

public sealed class UpsertAttractionAccessConditionTypeDefinitionCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenKeyIsBlank_ShouldReturnInvalidKeyAndNotCallRepository()
    {
        Mock<IAttractionAccessConditionTypeDefinitionRepository> repository = new Mock<IAttractionAccessConditionTypeDefinitionRepository>(MockBehavior.Strict);
        UpsertAttractionAccessConditionTypeDefinitionCommandHandler handler = new UpsertAttractionAccessConditionTypeDefinitionCommandHandler(repository.Object);
        AttractionAccessConditionTypeDefinitionWriteModel model = new AttractionAccessConditionTypeDefinitionWriteModel
        {
            Key = "   ",
            Labels = new[] { new LocalizedTextValue("fr", "Libellé") },
        };

        ApplicationResult<AttractionAccessConditionTypeDefinition> result = await handler.HandleAsync(new UpsertAttractionAccessConditionTypeDefinitionCommand(model));

        Assert.False(result.IsSuccess);
        ApplicationError error = Assert.Single(result.Errors);
        Assert.Equal("attraction-access-condition-type.key.invalid", error.Code);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenLabelsAreMissingAfterNormalization_ShouldReturnMissingLabelsAndNotCallRepository()
    {
        Mock<IAttractionAccessConditionTypeDefinitionRepository> repository = new Mock<IAttractionAccessConditionTypeDefinitionRepository>(MockBehavior.Strict);
        UpsertAttractionAccessConditionTypeDefinitionCommandHandler handler = new UpsertAttractionAccessConditionTypeDefinitionCommandHandler(repository.Object);
        AttractionAccessConditionTypeDefinitionWriteModel model = new AttractionAccessConditionTypeDefinitionWriteModel
        {
            Key = "Minimum Height",
            Labels = new[]
            {
                new LocalizedTextValue("fr", "   "),
                new LocalizedTextValue(" ", "Label"),
            },
        };

        ApplicationResult<AttractionAccessConditionTypeDefinition> result = await handler.HandleAsync(new UpsertAttractionAccessConditionTypeDefinitionCommand(model));

        Assert.False(result.IsSuccess);
        ApplicationError error = Assert.Single(result.Errors);
        Assert.Equal("attraction-access-condition-type.labels.missing", error.Code);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenModelIsValid_ShouldNormalizeValuesAndCallRepository()
    {
        CancellationToken cancellationToken = new CancellationTokenSource().Token;
        AttractionAccessConditionTypeDefinition persisted = new AttractionAccessConditionTypeDefinition { Id = "id", Key = "minimum-height" };
        Mock<IAttractionAccessConditionTypeDefinitionRepository> repository = new Mock<IAttractionAccessConditionTypeDefinitionRepository>(MockBehavior.Strict);
        repository.Setup(item => item.UpsertAsync(It.Is<AttractionAccessConditionTypeDefinitionWriteModel>(model =>
                model.Key == "minimum-height"
                && model.Labels.Count == 2
                && model.Labels.Any(label => label.LanguageCode == "en" && label.Value == "Minimum height")
                && model.Labels.Any(label => label.LanguageCode == "fr" && label.Value == "Taille")
                && model.Descriptions.Count == 1), cancellationToken))
            .ReturnsAsync(persisted);
        UpsertAttractionAccessConditionTypeDefinitionCommandHandler handler = new UpsertAttractionAccessConditionTypeDefinitionCommandHandler(repository.Object);
        AttractionAccessConditionTypeDefinitionWriteModel model = new AttractionAccessConditionTypeDefinitionWriteModel
        {
            Key = " Minimum Height ",
            Labels = new[]
            {
                new LocalizedTextValue(" FR ", " Taille "),
                new LocalizedTextValue("en", "Old"),
                new LocalizedTextValue("EN", " Minimum height "),
            },
            Descriptions = new[] { new LocalizedTextValue(" fr ", " Description ") },
        };

        ApplicationResult<AttractionAccessConditionTypeDefinition> result = await handler.HandleAsync(new UpsertAttractionAccessConditionTypeDefinitionCommand(model), cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Same(persisted, result.Value);
        repository.VerifyAll();
    }
}
