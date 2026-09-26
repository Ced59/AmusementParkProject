using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class ParkHistoricalSnapshotBuilderTests
{
    private static readonly DateTime RecordedAtUtc = new DateTime(
        2026,
        9,
        26,
        12,
        0,
        0,
        DateTimeKind.Utc);

    private static readonly HistoricalSubject Subject = new HistoricalSubject(
        HistoricalSubjectType.ParkItem,
        "attraction-1",
        "Attraction exemple",
        HistoricalSubjectPublicationPolicy.FollowCurrentSubject);

    private readonly IParkHistoricalSnapshotBuilder builder = new ParkHistoricalSnapshotBuilder();

    [Fact]
    public void Build_WithoutEligibleLifecycleFact_ReturnsUnknown()
    {
        HistoricalSubjectSnapshot snapshot = this.BuildSubject(
            HistoricalInstant.ForDay(2000, 1, 1),
            Array.Empty<HistoricalFact>());

        Assert.Equal(HistoricalOperationalState.Unknown, snapshot.OperationalState);
        Assert.Equal(HistoricalPresenceExtent.None, snapshot.PresenceExtent);
        Assert.Contains(
            snapshot.Reasons,
            reason => reason.Code == HistoricalSnapshotReasonCode.NoEligibleLifecycleFact);
    }

    [Fact]
    public void Build_OnConfirmedOpeningDay_ReturnsKnownOpen()
    {
        HistoricalFact opening = CreateLifecycleFact(
            HistoricalFactType.Opening,
            HistoricalDate.ForDay(1998, 5, 12),
            LifecycleBoundaryMeaning.FirstOperatingDay);

        HistoricalSubjectSnapshot snapshot = this.BuildSubject(
            HistoricalInstant.ForDay(1998, 5, 12),
            new[] { opening });

        Assert.Equal(HistoricalOperationalState.KnownOpen, snapshot.OperationalState);
        Assert.Equal(HistoricalPresenceExtent.EntireRequestedPeriod, snapshot.PresenceExtent);
        Assert.Equal(
            new HistoricalPresenceInterval(new DateOnly(1998, 5, 12), new DateOnly(1998, 5, 12)),
            Assert.Single(snapshot.ConfirmedPresenceIntervals));
    }

    [Fact]
    public void Build_BeforeConfirmedInitialOpening_ReturnsKnownClosed()
    {
        HistoricalFact opening = CreateLifecycleFact(
            HistoricalFactType.Opening,
            HistoricalDate.ForDay(1998, 5, 12),
            LifecycleBoundaryMeaning.FirstOperatingDay);

        HistoricalSubjectSnapshot snapshot = this.BuildSubject(
            HistoricalInstant.ForDay(1998, 5, 11),
            new[] { opening });

        Assert.Equal(HistoricalOperationalState.KnownClosed, snapshot.OperationalState);
        Assert.Contains(
            snapshot.Reasons,
            reason => reason.Code == HistoricalSnapshotReasonCode.BeforeConfirmedInitialOpening);
    }

    [Fact]
    public void Build_ForOpeningKnownOnlyByYear_ReportsPartialConfirmedPresence()
    {
        HistoricalFact opening = CreateLifecycleFact(
            HistoricalFactType.Opening,
            HistoricalDate.ForYear(1998),
            LifecycleBoundaryMeaning.FirstOperatingDay);

        HistoricalSubjectSnapshot snapshot = this.BuildSubject(
            HistoricalInstant.ForYear(1998),
            new[] { opening });

        Assert.Equal(HistoricalOperationalState.KnownOpen, snapshot.OperationalState);
        Assert.Equal(HistoricalPresenceExtent.PartOfRequestedPeriod, snapshot.PresenceExtent);
        Assert.Equal(
            new HistoricalPresenceInterval(new DateOnly(1998, 12, 31), new DateOnly(1998, 12, 31)),
            Assert.Single(snapshot.ConfirmedPresenceIntervals));
        Assert.Contains(
            snapshot.Reasons,
            reason => reason.Code == HistoricalSnapshotReasonCode.PartialLifecycleBoundary);
    }

    [Fact]
    public void Build_InsidePartialOpeningBoundary_ReturnsPossiblyOpen()
    {
        HistoricalFact opening = CreateLifecycleFact(
            HistoricalFactType.Opening,
            HistoricalDate.ForYear(1998),
            LifecycleBoundaryMeaning.FirstOperatingDay);

        HistoricalSubjectSnapshot snapshot = this.BuildSubject(
            HistoricalInstant.ForDay(1998, 6, 1),
            new[] { opening });

        Assert.Equal(HistoricalOperationalState.PossiblyOpen, snapshot.OperationalState);
    }

    [Fact]
    public void Build_AfterVerifiedBoundedOpening_AppliesTransition()
    {
        HistoricalFact opening = CreateLifecycleFact(
            HistoricalFactType.Opening,
            HistoricalDate.ForYear(2000, qualifier: DateQualifier.Before),
            LifecycleBoundaryMeaning.FirstOperatingDay);

        HistoricalSubjectSnapshot snapshot = this.BuildSubject(
            HistoricalInstant.ForDay(2001, 1, 1),
            new[] { opening });

        Assert.Equal(HistoricalOperationalState.KnownOpen, snapshot.OperationalState);
    }

    [Fact]
    public void Build_BetweenDefinitiveClosureAndReopening_ReturnsKnownClosed()
    {
        HistoricalFact[] facts =
        {
            CreateLifecycleFact(
                HistoricalFactType.Opening,
                HistoricalDate.ForDay(2000, 1, 1),
                LifecycleBoundaryMeaning.FirstOperatingDay),
            CreateLifecycleFact(
                HistoricalFactType.DefinitiveClosure,
                HistoricalDate.ForDay(2005, 6, 1),
                LifecycleBoundaryMeaning.FirstClosedDay),
            CreateLifecycleFact(
                HistoricalFactType.Reopening,
                HistoricalDate.ForDay(2007, 3, 1),
                LifecycleBoundaryMeaning.FirstOperatingDay),
        };

        HistoricalSubjectSnapshot closedSnapshot = this.BuildSubject(
            HistoricalInstant.ForDay(2006, 1, 1),
            facts);
        HistoricalSubjectSnapshot reopenedSnapshot = this.BuildSubject(
            HistoricalInstant.ForDay(2008, 1, 1),
            facts);

        Assert.Equal(HistoricalOperationalState.KnownClosed, closedSnapshot.OperationalState);
        Assert.Equal(HistoricalOperationalState.KnownOpen, reopenedSnapshot.OperationalState);
        Assert.Contains(
            reopenedSnapshot.Reasons,
            reason => reason.Code == HistoricalSnapshotReasonCode.InconsistentLifecycleSequence);
    }

    [Fact]
    public void Build_OnLastOperatingDay_KeepsSubjectOpenUntilNextDay()
    {
        HistoricalFact[] facts =
        {
            CreateLifecycleFact(
                HistoricalFactType.Opening,
                HistoricalDate.ForDay(2000, 1, 1),
                LifecycleBoundaryMeaning.FirstOperatingDay),
            CreateLifecycleFact(
                HistoricalFactType.DefinitiveClosure,
                HistoricalDate.ForDay(2005, 6, 1),
                LifecycleBoundaryMeaning.LastOperatingDay),
        };

        HistoricalSubjectSnapshot lastOperatingDay = this.BuildSubject(
            HistoricalInstant.ForDay(2005, 6, 1),
            facts);
        HistoricalSubjectSnapshot nextDay = this.BuildSubject(
            HistoricalInstant.ForDay(2005, 6, 2),
            facts);

        Assert.Equal(HistoricalOperationalState.KnownOpen, lastOperatingDay.OperationalState);
        Assert.Equal(HistoricalOperationalState.KnownClosed, nextDay.OperationalState);
    }

    [Fact]
    public void Build_WithOnlyLastOperatingDay_ConfirmsActivityOnThatDay()
    {
        HistoricalFact closure = CreateLifecycleFact(
            HistoricalFactType.DefinitiveClosure,
            HistoricalDate.ForDay(2005, 6, 1),
            LifecycleBoundaryMeaning.LastOperatingDay);

        HistoricalSubjectSnapshot snapshot = this.BuildSubject(
            HistoricalInstant.ForDay(2005, 6, 1),
            new[] { closure });

        Assert.Equal(HistoricalOperationalState.KnownOpen, snapshot.OperationalState);
        Assert.Equal(HistoricalPresenceExtent.EntireRequestedPeriod, snapshot.PresenceExtent);
    }

    [Fact]
    public void Build_AfterUnboundedTemporaryClosure_ReturnsUnknown()
    {
        HistoricalFact[] facts =
        {
            CreateLifecycleFact(
                HistoricalFactType.Opening,
                HistoricalDate.ForDay(2000, 1, 1),
                LifecycleBoundaryMeaning.FirstOperatingDay),
            CreateLifecycleFact(
                HistoricalFactType.TemporaryClosure,
                HistoricalDate.ForDay(2005, 6, 1),
                LifecycleBoundaryMeaning.FirstClosedDay),
        };

        HistoricalSubjectSnapshot snapshot = this.BuildSubject(
            HistoricalInstant.ForDay(2005, 6, 2),
            facts);

        Assert.Equal(HistoricalOperationalState.Unknown, snapshot.OperationalState);
        Assert.Contains(
            snapshot.Reasons,
            reason => reason.Code == HistoricalSnapshotReasonCode.UnboundedTemporaryClosure);
    }

    [Fact]
    public void Build_OnExactFirstDayOfUnboundedTemporaryClosure_ReturnsKnownClosed()
    {
        HistoricalFact[] facts =
        {
            CreateLifecycleFact(
                HistoricalFactType.Opening,
                HistoricalDate.ForDay(2000, 1, 1),
                LifecycleBoundaryMeaning.FirstOperatingDay),
            CreateLifecycleFact(
                HistoricalFactType.TemporaryClosure,
                HistoricalDate.ForDay(2005, 6, 1),
                LifecycleBoundaryMeaning.FirstClosedDay),
        };

        HistoricalSubjectSnapshot snapshot = this.BuildSubject(
            HistoricalInstant.ForDay(2005, 6, 1),
            facts);

        Assert.Equal(HistoricalOperationalState.KnownClosed, snapshot.OperationalState);
    }

    [Fact]
    public void Build_DuringTemporaryClosureBoundedByReopening_ReturnsKnownClosed()
    {
        HistoricalFact[] facts =
        {
            CreateLifecycleFact(
                HistoricalFactType.Opening,
                HistoricalDate.ForDay(2000, 1, 1),
                LifecycleBoundaryMeaning.FirstOperatingDay),
            CreateLifecycleFact(
                HistoricalFactType.TemporaryClosure,
                HistoricalDate.ForDay(2005, 6, 1),
                LifecycleBoundaryMeaning.FirstClosedDay),
            CreateLifecycleFact(
                HistoricalFactType.Reopening,
                HistoricalDate.ForDay(2006, 4, 1),
                LifecycleBoundaryMeaning.FirstOperatingDay),
        };

        HistoricalSubjectSnapshot snapshot = this.BuildSubject(
            HistoricalInstant.ForDay(2005, 8, 1),
            facts);

        Assert.Equal(HistoricalOperationalState.KnownClosed, snapshot.OperationalState);
    }

    [Fact]
    public void Build_WithProbableOpening_ReturnsPossiblyOpen()
    {
        HistoricalFact opening = CreateLifecycleFact(
            HistoricalFactType.Opening,
            HistoricalDate.ForDay(1998, 5, 12),
            LifecycleBoundaryMeaning.FirstOperatingDay,
            state: HistoricalFactState.Probable);

        HistoricalSubjectSnapshot snapshot = this.BuildSubject(
            HistoricalInstant.ForDay(1998, 5, 12),
            new[] { opening });

        Assert.Equal(HistoricalOperationalState.PossiblyOpen, snapshot.OperationalState);
        Assert.Contains(
            snapshot.Reasons,
            reason => reason.Code == HistoricalSnapshotReasonCode.UncertainEvidence);
    }

    [Fact]
    public void Build_WithUnorderedSameDayTransitions_ReturnsPossiblyOpen()
    {
        HistoricalDate date = HistoricalDate.ForDay(2000, 1, 1);
        HistoricalFact[] facts =
        {
            CreateLifecycleFact(
                HistoricalFactType.Opening,
                date,
                LifecycleBoundaryMeaning.FirstOperatingDay),
            CreateLifecycleFact(
                HistoricalFactType.DefinitiveClosure,
                date,
                LifecycleBoundaryMeaning.FirstClosedDay),
        };

        HistoricalSubjectSnapshot snapshot = this.BuildSubject(
            HistoricalInstant.ForDay(2000, 1, 1),
            facts);

        Assert.Equal(HistoricalOperationalState.PossiblyOpen, snapshot.OperationalState);
        Assert.Contains(
            snapshot.Reasons,
            reason => reason.Code == HistoricalSnapshotReasonCode.AmbiguousTransitionOrder);
    }

    [Fact]
    public void Build_WithSequencedSameDayTransitions_UsesExplicitOrder()
    {
        HistoricalDate date = HistoricalDate.ForDay(2000, 1, 1);
        HistoricalFact[] facts =
        {
            CreateLifecycleFact(
                HistoricalFactType.Opening,
                date,
                LifecycleBoundaryMeaning.FirstOperatingDay,
                sequenceWithinDate: 1),
            CreateLifecycleFact(
                HistoricalFactType.DefinitiveClosure,
                date,
                LifecycleBoundaryMeaning.FirstClosedDay,
                sequenceWithinDate: 2),
        };

        HistoricalSubjectSnapshot snapshot = this.BuildSubject(
            HistoricalInstant.ForDay(2000, 1, 1),
            facts);

        Assert.Equal(HistoricalOperationalState.KnownClosed, snapshot.OperationalState);
        Assert.DoesNotContain(
            snapshot.Reasons,
            reason => reason.Code == HistoricalSnapshotReasonCode.AmbiguousTransitionOrder);
    }

    [Fact]
    public void Build_WhenLatestFactRevisionIsRetracted_ExcludesOlderPublishedRevision()
    {
        HistoricalFact opening = CreateLifecycleFact(
            HistoricalFactType.Opening,
            HistoricalDate.ForDay(1998, 5, 12),
            LifecycleBoundaryMeaning.FirstOperatingDay);
        HistoricalFact retraction = opening.CreateRetraction(RecordedAtUtc.AddMinutes(1));

        HistoricalSubjectSnapshot snapshot = this.BuildSubject(
            HistoricalInstant.ForDay(2000, 1, 1),
            new[] { opening, retraction });

        Assert.Equal(HistoricalOperationalState.Unknown, snapshot.OperationalState);
        Assert.Empty(snapshot.SupportingFactIds);
    }

    [Fact]
    public void Build_ExcludesNarrativeFactFromSupportingFactIds()
    {
        HistoricalFact opening = CreateLifecycleFact(
            HistoricalFactType.Opening,
            HistoricalDate.ForDay(1998, 5, 12),
            LifecycleBoundaryMeaning.FirstOperatingDay);
        HistoricalFact announcement = CreateFact(
            HistoricalFactType.Announcement,
            HistoricalPeriod.Point(HistoricalDate.ForDay(2000, 1, 1)),
            HistoricalFactState.Verified,
            null,
            null,
            null,
            null,
            null);

        HistoricalSubjectSnapshot snapshot = this.BuildSubject(
            HistoricalInstant.ForDay(2001, 1, 1),
            new[] { announcement, opening });

        Assert.Equal(new[] { opening.Id }, snapshot.SupportingFactIds);
    }

    [Fact]
    public void Build_OnExactRenamingBoundary_UsesPreviousThenNewName()
    {
        HistoricalFact opening = CreateLifecycleFact(
            HistoricalFactType.Opening,
            HistoricalDate.ForDay(1990, 1, 1),
            LifecycleBoundaryMeaning.FirstOperatingDay);
        HistoricalFact renaming = CreateAttributeFact(
            HistoricalDate.ForDay(2000, 5, 1),
            AttributeBoundaryMeaning.FirstDayOfNewValue,
            "Ancien nom",
            "Nouveau nom");

        HistoricalAttributeSnapshot previous = Assert.Single(this.BuildSubject(
            HistoricalInstant.ForDay(2000, 4, 30),
            new[] { opening, renaming }).Attributes);
        HistoricalAttributeSnapshot current = Assert.Single(this.BuildSubject(
            HistoricalInstant.ForDay(2000, 5, 1),
            new[] { opening, renaming }).Attributes);

        Assert.Equal(HistoricalAttributeValueState.Known, previous.State);
        Assert.Equal("Ancien nom", previous.Value);
        Assert.Equal(HistoricalAttributeValueState.Known, current.State);
        Assert.Equal("Nouveau nom", current.Value);
    }

    [Fact]
    public void Build_InsidePartialRenamingBoundary_ReturnsBothCandidates()
    {
        HistoricalFact opening = CreateLifecycleFact(
            HistoricalFactType.Opening,
            HistoricalDate.ForDay(1990, 1, 1),
            LifecycleBoundaryMeaning.FirstOperatingDay);
        HistoricalFact renaming = CreateAttributeFact(
            HistoricalDate.ForYear(2000),
            AttributeBoundaryMeaning.FirstDayOfNewValue,
            "Ancien nom",
            "Nouveau nom");

        HistoricalAttributeSnapshot attribute = Assert.Single(this.BuildSubject(
            HistoricalInstant.ForDay(2000, 6, 1),
            new[] { opening, renaming }).Attributes);

        Assert.Equal(HistoricalAttributeValueState.Ambiguous, attribute.State);
        Assert.Equal(new[] { "Ancien nom", "Nouveau nom" }, attribute.Candidates);
        Assert.Contains(
            attribute.Reasons,
            reason => reason.Code == HistoricalSnapshotReasonCode.PartialAttributeBoundary);
    }

    [Fact]
    public void Build_WithLargeRequiredAttributeGroup_DoesNotRetainPreGroupValue()
    {
        HistoricalDate date = HistoricalDate.ForDay(2000, 1, 1);
        HistoricalFact opening = CreateLifecycleFact(
            HistoricalFactType.Opening,
            HistoricalDate.ForDay(1990, 1, 1),
            LifecycleBoundaryMeaning.FirstOperatingDay);
        HistoricalFact[] renamings = Enumerable.Range(1, 11)
            .Select(index => CreateAttributeFact(
                date,
                AttributeBoundaryMeaning.FirstDayOfNewValue,
                "Valeur initiale",
                $"Nom {index:D2}"))
            .ToArray();

        HistoricalAttributeSnapshot attribute = Assert.Single(this.BuildSubject(
            HistoricalInstant.ForDay(2000, 1, 2),
            renamings.Append(opening).ToArray()).Attributes);

        Assert.Equal(HistoricalAttributeValueState.Ambiguous, attribute.State);
        Assert.Equal(11, attribute.Candidates.Count);
        Assert.DoesNotContain("Valeur initiale", attribute.Candidates);
    }

    [Fact]
    public void Build_WithConnectedOverlapGroup_PreservesDefinitePairwiseOrder()
    {
        HistoricalFact opening = CreateLifecycleFact(
            HistoricalFactType.Opening,
            HistoricalDate.ForDay(1990, 1, 1),
            LifecycleBoundaryMeaning.FirstOperatingDay);
        HistoricalFact broadRenaming = CreateAttributeFact(
            HistoricalDate.ForYear(2000),
            AttributeBoundaryMeaning.FirstDayOfNewValue,
            "Nom initial",
            "Nom annuel");
        HistoricalFact februaryRenaming = CreateAttributeFact(
            HistoricalDate.ForDay(2000, 2, 1),
            AttributeBoundaryMeaning.FirstDayOfNewValue,
            "Nom initial",
            "Nom février");
        HistoricalFact novemberRenaming = CreateAttributeFact(
            HistoricalDate.ForDay(2000, 11, 1),
            AttributeBoundaryMeaning.FirstDayOfNewValue,
            "Nom février",
            "Nom novembre");

        HistoricalAttributeSnapshot attribute = Assert.Single(this.BuildSubject(
            HistoricalInstant.ForDay(2001, 1, 1),
            new[] { opening, broadRenaming, februaryRenaming, novemberRenaming }).Attributes);

        Assert.Equal(HistoricalAttributeValueState.Ambiguous, attribute.State);
        Assert.Equal(new[] { "Nom annuel", "Nom novembre" }, attribute.Candidates);
        Assert.DoesNotContain("Nom février", attribute.Candidates);
    }

    [Fact]
    public void Build_BeforeSequencedSameDayRenamings_UsesFirstPreviousValue()
    {
        HistoricalFact opening = CreateLifecycleFact(
            HistoricalFactType.Opening,
            HistoricalDate.ForDay(1990, 1, 1),
            LifecycleBoundaryMeaning.FirstOperatingDay);
        HistoricalFact firstRenaming = CreateAttributeFact(
            HistoricalDate.ForDay(2000, 5, 1),
            AttributeBoundaryMeaning.FirstDayOfNewValue,
            "Nom A",
            "Nom B",
            sequenceWithinDate: 1,
            id: Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"));
        HistoricalFact secondRenaming = CreateAttributeFact(
            HistoricalDate.ForDay(2000, 5, 1),
            AttributeBoundaryMeaning.FirstDayOfNewValue,
            "Nom B",
            "Nom C",
            sequenceWithinDate: 2,
            id: Guid.Parse("00000000-0000-0000-0000-000000000001"));

        HistoricalAttributeSnapshot attribute = Assert.Single(this.BuildSubject(
            HistoricalInstant.ForDay(2000, 4, 30),
            new[] { opening, secondRenaming, firstRenaming }).Attributes);

        Assert.Equal(HistoricalAttributeValueState.Known, attribute.State);
        Assert.Equal("Nom A", attribute.Value);
    }

    [Fact]
    public void Build_WithBroadAndSequencedSameDayRenamings_PreservesSourcedOrder()
    {
        HistoricalFact opening = CreateLifecycleFact(
            HistoricalFactType.Opening,
            HistoricalDate.ForDay(1990, 1, 1),
            LifecycleBoundaryMeaning.FirstOperatingDay);
        HistoricalFact broadRenaming = CreateAttributeFact(
            HistoricalDate.ForYear(2000),
            AttributeBoundaryMeaning.FirstDayOfNewValue,
            "Nom initial",
            "Nom annuel");
        HistoricalFact firstRenaming = CreateAttributeFact(
            HistoricalDate.ForDay(2000, 5, 1),
            AttributeBoundaryMeaning.FirstDayOfNewValue,
            "Nom initial",
            "Nom B",
            sequenceWithinDate: 1);
        HistoricalFact secondRenaming = CreateAttributeFact(
            HistoricalDate.ForDay(2000, 5, 1),
            AttributeBoundaryMeaning.FirstDayOfNewValue,
            "Nom B",
            "Nom C",
            sequenceWithinDate: 2);

        HistoricalAttributeSnapshot attribute = Assert.Single(this.BuildSubject(
            HistoricalInstant.ForDay(2001, 1, 1),
            new[] { opening, broadRenaming, secondRenaming, firstRenaming }).Attributes);

        Assert.Equal(HistoricalAttributeValueState.Ambiguous, attribute.State);
        Assert.Equal(new[] { "Nom C", "Nom annuel" }, attribute.Candidates);
        Assert.DoesNotContain("Nom B", attribute.Candidates);
    }

    [Fact]
    public void Build_WithLargeConnectedAttributeGroup_ExcludesValuesWithRequiredSuccessors()
    {
        HistoricalFact opening = CreateLifecycleFact(
            HistoricalFactType.Opening,
            HistoricalDate.ForDay(1990, 1, 1),
            LifecycleBoundaryMeaning.FirstOperatingDay);
        HistoricalFact broadRenaming = CreateAttributeFact(
            HistoricalDate.ForYear(2000),
            AttributeBoundaryMeaning.FirstDayOfNewValue,
            "Nom initial",
            "Nom annuel");
        HistoricalFact[] exactRenamings = Enumerable.Range(1, 10)
            .Select(index => CreateAttributeFact(
                HistoricalDate.ForDay(2000, index, 1),
                AttributeBoundaryMeaning.FirstDayOfNewValue,
                index == 1 ? "Nom initial" : $"Nom {index - 1:D2}",
                $"Nom {index:D2}"))
            .ToArray();

        HistoricalAttributeSnapshot attribute = Assert.Single(this.BuildSubject(
            HistoricalInstant.ForDay(2001, 1, 1),
            exactRenamings.Append(opening).Append(broadRenaming).ToArray()).Attributes);

        Assert.Equal(HistoricalAttributeValueState.Ambiguous, attribute.State);
        Assert.Equal(new[] { "Nom 10", "Nom annuel" }, attribute.Candidates);
        Assert.DoesNotContain("Nom 01", attribute.Candidates);
    }

    [Fact]
    public void Build_WithLargeConnectedLifecycleGroup_ExcludesStatesWithRequiredSuccessors()
    {
        HistoricalFact broadOpening = CreateLifecycleFact(
            HistoricalFactType.Opening,
            HistoricalDate.ForYear(2000),
            LifecycleBoundaryMeaning.FirstOperatingDay);
        HistoricalFact closure = CreateLifecycleFact(
            HistoricalFactType.Closure,
            HistoricalDate.ForDay(2000, 1, 1),
            LifecycleBoundaryMeaning.FirstClosedDay);
        HistoricalFact[] reopenings = Enumerable.Range(2, 9)
            .Select(month => CreateLifecycleFact(
                HistoricalFactType.Reopening,
                HistoricalDate.ForDay(2000, month, 1),
                LifecycleBoundaryMeaning.FirstOperatingDay))
            .ToArray();

        HistoricalSubjectSnapshot snapshot = this.BuildSubject(
            HistoricalInstant.ForDay(2001, 1, 1),
            reopenings.Append(closure).Append(broadOpening).ToArray());

        Assert.Equal(HistoricalOperationalState.KnownOpen, snapshot.OperationalState);
    }

    [Fact]
    public void Build_WithLargeRequiredLifecycleGroup_DoesNotRetainPreGroupState()
    {
        HistoricalDate date = HistoricalDate.ForDay(2000, 1, 1);
        HistoricalFact[] openings = Enumerable.Range(1, 11)
            .Select(_ => CreateLifecycleFact(
                HistoricalFactType.Opening,
                date,
                LifecycleBoundaryMeaning.FirstOperatingDay))
            .ToArray();

        HistoricalSubjectSnapshot snapshot = this.BuildSubject(
            HistoricalInstant.ForDay(2000, 1, 2),
            openings);

        Assert.Equal(HistoricalOperationalState.KnownOpen, snapshot.OperationalState);
    }

    [Fact]
    public void Build_WithSameInputs_IsDeterministicAndSorted()
    {
        HistoricalFact opening = CreateLifecycleFact(
            HistoricalFactType.Opening,
            HistoricalDate.ForDay(1998, 5, 12),
            LifecycleBoundaryMeaning.FirstOperatingDay);
        HistoricalFact renaming = CreateAttributeFact(
            HistoricalDate.ForDay(2000, 5, 1),
            AttributeBoundaryMeaning.FirstDayOfNewValue,
            "Ancien nom",
            "Nouveau nom");
        HistoricalInstant instant = HistoricalInstant.ForDay(2001, 1, 1);

        ParkHistoricalSnapshot first = this.builder.Build(
            "park-1",
            instant,
            new[] { Subject },
            new[] { renaming, opening });
        ParkHistoricalSnapshot second = this.builder.Build(
            "park-1",
            instant,
            new[] { Subject },
            new[] { opening, renaming });

        HistoricalSubjectSnapshot firstSubject = Assert.Single(first.Subjects);
        HistoricalSubjectSnapshot secondSubject = Assert.Single(second.Subjects);
        Assert.Equal(first.MethodologyVersion, second.MethodologyVersion);
        Assert.Equal(firstSubject.OperationalState, secondSubject.OperationalState);
        Assert.Equal(firstSubject.PresenceExtent, secondSubject.PresenceExtent);
        Assert.Equal(firstSubject.ConfirmedPresenceIntervals, secondSubject.ConfirmedPresenceIntervals);
        Assert.Equal(firstSubject.SupportingFactIds, secondSubject.SupportingFactIds);
        Assert.Equal(
            firstSubject.Attributes.Select(static attribute => (attribute.Kind, attribute.State, attribute.Value)),
            secondSubject.Attributes.Select(static attribute => (attribute.Kind, attribute.State, attribute.Value)));
    }

    [Fact]
    public void Build_AtMaximumSupportedDate_DoesNotOverflowCalendar()
    {
        HistoricalFact opening = CreateLifecycleFact(
            HistoricalFactType.Opening,
            HistoricalDate.ForDay(9999, 12, 31),
            LifecycleBoundaryMeaning.FirstOperatingDay);

        HistoricalSubjectSnapshot snapshot = this.BuildSubject(
            HistoricalInstant.ForDay(9999, 12, 31),
            new[] { opening });

        Assert.Equal(HistoricalOperationalState.KnownOpen, snapshot.OperationalState);
    }

    private HistoricalSubjectSnapshot BuildSubject(
        HistoricalInstant instant,
        IReadOnlyCollection<HistoricalFact> facts)
    {
        ParkHistoricalSnapshot snapshot = this.builder.Build(
            "park-1",
            instant,
            new[] { Subject },
            facts);
        return Assert.Single(snapshot.Subjects);
    }

    private static HistoricalFact CreateLifecycleFact(
        HistoricalFactType type,
        HistoricalDate date,
        LifecycleBoundaryMeaning boundaryMeaning,
        HistoricalFactState state = HistoricalFactState.Verified,
        int? sequenceWithinDate = null)
    {
        HistoricalPeriod period = HistoricalPeriod.Point(date);
        return CreateFact(
            type,
            period,
            state,
            boundaryMeaning,
            null,
            null,
            sequenceWithinDate,
            null);
    }

    private static HistoricalFact CreateAttributeFact(
        HistoricalDate date,
        AttributeBoundaryMeaning boundaryMeaning,
        string previousValue,
        string nextValue,
        int? sequenceWithinDate = null,
        Guid? id = null)
    {
        string structuredValue = string.Concat(
            "{\"previous\":\"",
            previousValue,
            "\",\"next\":\"",
            nextValue,
            "\"}");
        return CreateFact(
            HistoricalFactType.Renaming,
            HistoricalPeriod.Point(date),
            HistoricalFactState.Verified,
            null,
            HistoricalAttributeKind.Name,
            boundaryMeaning,
            sequenceWithinDate,
            structuredValue,
            id);
    }

    private static HistoricalFact CreateFact(
        HistoricalFactType type,
        HistoricalPeriod period,
        HistoricalFactState state,
        LifecycleBoundaryMeaning? lifecycleBoundaryMeaning,
        HistoricalAttributeKind? attributeKind,
        AttributeBoundaryMeaning? attributeBoundaryMeaning,
        int? sequenceWithinDate,
        string? structuredValue,
        Guid? id = null)
    {
        return new HistoricalFact(
            id ?? Guid.NewGuid(),
            Subject,
            type,
            period,
            state,
            HistoricalImportance.Major,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            state == HistoricalFactState.Verified
                ? Array.Empty<HistoricalLocalizedText>()
                : CreateCompleteExplanations(),
            lifecycleBoundaryMeaning,
            attributeKind,
            attributeBoundaryMeaning,
            sequenceWithinDate,
            new[]
            {
                CreateSourceReference(
                    type,
                    period,
                    lifecycleBoundaryMeaning,
                    attributeKind,
                    attributeBoundaryMeaning,
                    sequenceWithinDate,
                    structuredValue),
            },
            structuredValue,
            null,
            null,
            state == HistoricalFactState.Verified ? RecordedAtUtc.AddMinutes(-2) : null,
            RecordedAtUtc.AddMinutes(-1),
            ParkHistoricalSnapshotBuilder.CurrentMethodologyVersion,
            2,
            1,
            RecordedAtUtc);
    }

    private static HistoricalSourceRevisionReference CreateSourceReference(
        HistoricalFactType type,
        HistoricalPeriod period,
        LifecycleBoundaryMeaning? lifecycleBoundaryMeaning,
        HistoricalAttributeKind? attributeKind,
        AttributeBoundaryMeaning? attributeBoundaryMeaning,
        int? sequenceWithinDate,
        string? structuredValue)
    {
        List<HistoricalSourceScope> scopes = new List<HistoricalSourceScope>
        {
            HistoricalSourceScope.SubjectIdentity,
            HistoricalSourceScope.HistoricalLabel,
            HistoricalSourceScope.FactType,
            HistoricalSourceScope.Period,
        };
        if (sequenceWithinDate.HasValue)
        {
            scopes.Add(HistoricalSourceScope.SequenceWithinDate);
        }

        if (structuredValue is not null)
        {
            scopes.Add(HistoricalSourceScope.StructuredValue);
        }

        return new HistoricalSourceRevisionReference(
            Guid.NewGuid(),
            1,
            Subject.Type,
            Subject.Id,
            type,
            period,
            HistoricalEvidencePosition.Supports,
            scopes,
            Subject.HistoricalLabel,
            structuredValue,
            sequenceWithinDate,
            null,
            null,
            lifecycleBoundaryMeaning,
            attributeKind,
            attributeBoundaryMeaning);
    }

    private static IReadOnlyCollection<HistoricalLocalizedText> CreateCompleteExplanations()
    {
        return HistoricalLocalizationPolicy.SupportedLanguageCodes
            .Select(static languageCode => new HistoricalLocalizedText(
                languageCode,
                "The date remains uncertain."))
            .ToArray();
    }
}
