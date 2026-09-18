using System.Text.Json;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.WebAPI.Contracts.Trips;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class TripInvitationHttpMapperTests
{
    [Fact]
    public void CreateRequest_ShouldReadTheDelegatedRoleAsAString()
    {
        CreateTripInvitationRequestDto request = JsonSerializer.Deserialize<CreateTripInvitationRequestDto>(
            """
            {
              "expectedPlanVersion": 4,
              "proposedRole": "Participant",
              "lifetimeHours": 24
            }
            """,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        bool parsed = request.TryToApplication(out TripInvitationCreateInput? input);

        Assert.True(parsed);
        Assert.NotNull(input);
        Assert.Equal(TripDelegatedRole.Participant, input.ProposedRole);
    }

    [Fact]
    public void Preview_ShouldWriteEveryInvitationEnumAsAString()
    {
        TripInvitationPreviewDto preview = new(
            "Séjour privé",
            "Camille",
            TripDelegatedRoleDto.Editor,
            TripInvitationPeriodKindDto.MonthRange,
            "2027-06",
            "2027-07",
            TripInvitationMemberCountBandDto.TwoToFive,
            new DateTime(2027, 6, 1, 12, 0, 0, DateTimeKind.Utc),
            true);

        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(preview));
        JsonElement root = document.RootElement;

        Assert.Equal("Editor", root.GetProperty("ProposedRole").GetString());
        Assert.Equal("MonthRange", root.GetProperty("PeriodKind").GetString());
        Assert.Equal("TwoToFive", root.GetProperty("MemberCountBand").GetString());
    }
}
