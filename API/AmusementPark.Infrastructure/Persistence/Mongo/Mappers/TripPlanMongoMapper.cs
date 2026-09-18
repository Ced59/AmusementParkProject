using System.Globalization;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class TripPlanMongoMapper
{
    private const string DateFormat = "yyyy-MM-dd";

    public static TripPlanDocument ToDocument(this TripPlan trip)
    {
        ArgumentNullException.ThrowIfNull(trip);
        return new TripPlanDocument
        {
            Id = trip.Id.Value,
            OwnerUserId = trip.OwnerUserId,
            Title = trip.Title,
            DateProposal = trip.DateProposal.ToDocument(),
            DestinationTimeZoneId = trip.DestinationTimeZoneId,
            Status = trip.Status,
            AccessScope = trip.AccessScope,
            Members = trip.Members.Select(static member => member.ToDocument()).ToList(),
            MemberAdmissionFence = trip.MemberAdmissionFence?.ToDocument(),
            AdmissionClosureState = trip.AdmissionClosureState,
            DeletionState = trip.DeletionState,
            ChildMutationEpoch = trip.ChildMutationEpoch,
            CreatedAt = ToMongoPrecision(trip.CreatedAtUtc),
            UpdatedAt = ToMongoPrecision(trip.UpdatedAtUtc),
            Version = trip.Version,
        };
    }

    public static TripPlan ToDomain(this TripPlanDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Restore(
            document.Id,
            document.OwnerUserId,
            document.Title,
            document.DateProposal,
            document.DestinationTimeZoneId,
            document.Status,
            document.AccessScope,
            document.Members,
            document.MemberAdmissionFence,
            document.AdmissionClosureState,
            document.DeletionState,
            document.ChildMutationEpoch,
            document.CreatedAt,
            document.UpdatedAt,
            document.Version);
    }

    public static TripPlanCreationSnapshotDocument CreateCreationSnapshot(this TripPlanDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new TripPlanCreationSnapshotDocument
        {
            OwnerUserId = document.OwnerUserId,
            Title = document.Title,
            DateProposal = Clone(document.DateProposal),
            DestinationTimeZoneId = document.DestinationTimeZoneId,
            Status = document.Status,
            AccessScope = document.AccessScope,
            Members = document.Members.Select(Clone).ToList(),
            AdmissionClosureState = document.AdmissionClosureState,
            DeletionState = document.DeletionState,
            ChildMutationEpoch = document.ChildMutationEpoch,
            CreatedAtUtc = document.CreatedAt,
            UpdatedAtUtc = document.UpdatedAt,
            Version = document.Version,
        };
    }

    public static TripPlan CreationSnapshotToDomain(this TripPlanDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        TripPlanCreationSnapshotDocument snapshot = document.CreationSnapshot
            ?? throw new InvalidOperationException("The idempotent trip creation snapshot is missing.");
        return Restore(
            document.Id,
            snapshot.OwnerUserId,
            snapshot.Title,
            snapshot.DateProposal,
            snapshot.DestinationTimeZoneId,
            snapshot.Status,
            snapshot.AccessScope,
            snapshot.Members,
            null,
            snapshot.AdmissionClosureState,
            snapshot.DeletionState,
            snapshot.ChildMutationEpoch,
            snapshot.CreatedAtUtc,
            snapshot.UpdatedAtUtc,
            snapshot.Version);
    }

    private static TripPlan Restore(
        string id,
        string ownerUserId,
        string title,
        TripDateProposalDocument dateProposal,
        string? destinationTimeZoneId,
        TripPlanStatus status,
        TripPlanAccessScope accessScope,
        IReadOnlyCollection<TripMemberDocument> members,
        TripMemberAdmissionFenceDocument? memberAdmissionFence,
        TripAdmissionClosureState admissionClosureState,
        TripDeletionState deletionState,
        long childMutationEpoch,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        long version)
    {
        return TripPlan.Restore(
            TripPlanId.Parse(id),
            ownerUserId,
            title,
            dateProposal.ToDomain(),
            destinationTimeZoneId,
            status,
            accessScope,
            members.Select(static member => member.ToDomain()).ToArray(),
            admissionClosureState,
            deletionState,
            childMutationEpoch,
            DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(updatedAtUtc, DateTimeKind.Utc),
            version,
            memberAdmissionFence?.ToDomain());
    }

    private static TripDateProposalDocument ToDocument(this TripDateProposal proposal)
    {
        return new TripDateProposalDocument
        {
            Kind = proposal.Kind,
            StartDate = Format(proposal.StartDate),
            EndDate = Format(proposal.EndDate),
            CandidateDates = proposal.CandidateDates.Select(static date => Format(date)!).ToList(),
        };
    }

    private static TripMemberDocument ToDocument(this TripMember member)
    {
        return new TripMemberDocument
        {
            MemberId = member.Id.Value,
            UserId = member.UserId,
            DelegatedRole = member.DelegatedRole,
            State = member.State,
            JoinedAtUtc = ToMongoPrecision(member.JoinedAtUtc),
            MemberDataEpoch = member.MemberDataEpoch,
            AdmissionOperationId = member.AdmissionOperationId,
        };
    }

    private static TripMemberAdmissionFenceDocument ToDocument(this TripMemberAdmissionFence fence)
    {
        return new TripMemberAdmissionFenceDocument
        {
            InvitationId = fence.InvitationId.Value,
            OperationId = fence.OperationId,
            CandidateUserId = fence.CandidateUserId,
            Generation = fence.Generation,
            LeaseExpiresAtUtc = ToMongoPrecision(fence.LeaseExpiresAtUtc),
            State = fence.State,
        };
    }

    private static TripMemberAdmissionFence ToDomain(this TripMemberAdmissionFenceDocument document)
    {
        return TripMemberAdmissionFence.Restore(
            TripInvitationId.Parse(document.InvitationId),
            document.OperationId,
            document.CandidateUserId,
            document.Generation,
            DateTime.SpecifyKind(document.LeaseExpiresAtUtc, DateTimeKind.Utc),
            document.State);
    }

    private static TripDateProposal ToDomain(this TripDateProposalDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return TripDateProposal.Restore(
            document.Kind,
            Parse(document.StartDate),
            Parse(document.EndDate),
            document.CandidateDates.Select(static value => ParseRequired(value)).ToArray());
    }

    private static TripMember ToDomain(this TripMemberDocument document)
    {
        return TripMember.Restore(
            TripMemberId.Parse(document.MemberId),
            document.UserId,
            document.DelegatedRole,
            document.State,
            DateTime.SpecifyKind(document.JoinedAtUtc, DateTimeKind.Utc),
            document.MemberDataEpoch < 1 ? 1 : document.MemberDataEpoch,
            document.AdmissionOperationId);
    }

    private static TripDateProposalDocument Clone(TripDateProposalDocument document)
    {
        return new TripDateProposalDocument
        {
            Kind = document.Kind,
            StartDate = document.StartDate,
            EndDate = document.EndDate,
            CandidateDates = document.CandidateDates.ToList(),
        };
    }

    private static TripMemberDocument Clone(TripMemberDocument document)
    {
        return new TripMemberDocument
        {
            MemberId = document.MemberId,
            UserId = document.UserId,
            DelegatedRole = document.DelegatedRole,
            State = document.State,
            JoinedAtUtc = document.JoinedAtUtc,
            MemberDataEpoch = document.MemberDataEpoch,
            AdmissionOperationId = document.AdmissionOperationId,
        };
    }

    private static string? Format(DateOnly? value)
    {
        return value?.ToString(DateFormat, CultureInfo.InvariantCulture);
    }

    private static DateOnly? Parse(string? value)
    {
        return value is null ? null : ParseRequired(value);
    }

    private static DateOnly ParseRequired(string value)
    {
        return DateOnly.ParseExact(value, DateFormat, CultureInfo.InvariantCulture);
    }

    private static DateTime ToMongoPrecision(DateTime value)
    {
        long ticks = value.Ticks - (value.Ticks % TimeSpan.TicksPerMillisecond);
        return new DateTime(ticks, DateTimeKind.Utc);
    }

}
