using AmusementPark.Application.Features.ContextualBlocks.Results;
using AmusementPark.WebAPI.Contracts.ContextualBlocks;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class ContextualBlocksHttpMappersTests
{
    [Fact]
    public void ToHttp_WhenPreviewResultProvided_ShouldMapPreviewContract()
    {
        ContextualBlockPreviewResult source = new ContextualBlockPreviewResult
        {
            OperationId = "operation-1",
            BlockType = "park.description",
            IsApplied = false,
            CanApply = true,
            PreviewedAtUtc = new DateTime(2026, 6, 21, 10, 0, 0, DateTimeKind.Utc),
            Target = new ContextualBlockPreviewTarget
            {
                EntityType = "Park",
                EntityId = "park-1",
                DisplayName = "Phantasialand",
            },
            Counts = new ContextualBlockPreviewCounts
            {
                Updated = 1,
                Unchanged = 7,
                Warnings = 2,
                Errors = 0,
            },
        };
        source.Changes.Add(new ContextualBlockPreviewChange
        {
            EntityType = "Park",
            EntityId = "park-1",
            DisplayName = "Phantasialand",
            Field = "descriptions.fr.value",
            LanguageCode = "fr",
            ChangeType = "Updated",
            OldValue = "Ancien",
            NewValue = "Nouveau",
        });
        source.Warnings.Add("warning");

        ContextualBlockPreviewResultDto dto = source.ToHttp();

        Assert.Equal("operation-1", dto.OperationId);
        Assert.Equal("park.description", dto.BlockType);
        Assert.False(dto.IsApplied);
        Assert.True(dto.CanApply);
        Assert.Equal("Park", dto.Target.EntityType);
        Assert.Equal("park-1", dto.Target.EntityId);
        Assert.Equal("Phantasialand", dto.Target.DisplayName);
        Assert.Equal(1, dto.Counts.Updated);
        Assert.Equal(7, dto.Counts.Unchanged);
        Assert.Equal(2, dto.Counts.Warnings);
        Assert.Equal(0, dto.Counts.Errors);
        ContextualBlockPreviewChangeDto change = Assert.Single(dto.Changes);
        Assert.Equal("descriptions.fr.value", change.Field);
        Assert.Equal("fr", change.LanguageCode);
        Assert.Equal("Ancien", change.OldValue);
        Assert.Equal("Nouveau", change.NewValue);
        Assert.Equal("warning", Assert.Single(dto.Warnings));
    }
}
