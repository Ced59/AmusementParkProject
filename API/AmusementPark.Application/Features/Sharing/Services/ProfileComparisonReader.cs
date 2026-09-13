using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class ProfileComparisonReader
{
    public const int ManagementListLimit = 25;
    public const int ManagementScanLimit = 100;
    private const int ManagementPageSize = 25;
    private readonly IProfileComparisonRepository comparisonRepository;
    private readonly ProfileComparisonPassportResolver passportResolver;

    public ProfileComparisonReader(
        IProfileComparisonRepository comparisonRepository,
        ProfileComparisonPassportResolver passportResolver)
    {
        this.comparisonRepository = comparisonRepository
            ?? throw new ArgumentNullException(nameof(comparisonRepository));
        this.passportResolver = passportResolver
            ?? throw new ArgumentNullException(nameof(passportResolver));
    }

    public async Task<ApplicationResult<SharedProfileComparisonResult>> GetSharedAsync(
        string shareId,
        CancellationToken cancellationToken)
    {
        if (!ShareToken.TryParse(shareId, out ShareToken shareToken))
        {
            return NotFound();
        }

        ProfileComparison? comparison = await this.comparisonRepository.GetByShareTokenAsync(
            shareToken,
            cancellationToken);
        if (comparison?.IsPubliclyResolvable != true
            || !await this.PassportsRemainAvailableAsync(comparison, cancellationToken))
        {
            return NotFound();
        }

        if (!await this.PassportsRemainAvailableAsync(comparison, cancellationToken))
        {
            return NotFound();
        }

        ProfileComparison? currentComparison =
            await this.comparisonRepository.GetByShareTokenAsync(
                shareToken,
                cancellationToken);
        if (currentComparison?.IsPubliclyResolvable != true
            || currentComparison.Id != comparison.Id
            || currentComparison.Version != comparison.Version)
        {
            return NotFound();
        }

        return ApplicationResult<SharedProfileComparisonResult>.Success(
            new SharedProfileComparisonResult(
                currentComparison.CreatedAtUtc,
                currentComparison.Calculation));
    }

    public async Task<ApplicationResult<IReadOnlyCollection<ProfileComparisonSummaryResult>>>
        ListForParticipantAsync(string userId, CancellationToken cancellationToken)
    {
        string normalizedUserId = userId?.Trim() ?? string.Empty;
        if (normalizedUserId.Length == 0)
        {
            return ApplicationResult<IReadOnlyCollection<ProfileComparisonSummaryResult>>.Failure(
                SharingApplicationErrors.ComparisonNotFound());
        }

        List<ProfileComparisonSummaryResult> results = new();
        int scannedCount = 0;
        ProfileComparisonListCursor? after = null;
        while (results.Count < ManagementListLimit
            && scannedCount < ManagementScanLimit)
        {
            int remainingScanBudget = ManagementScanLimit - scannedCount;
            int pageSize = Math.Min(ManagementPageSize, remainingScanBudget);
            IReadOnlyCollection<ProfileComparison> comparisons =
                await this.comparisonRepository.ListActiveByParticipantAsync(
                    normalizedUserId,
                    after,
                    pageSize,
                    cancellationToken);
            foreach (ProfileComparison comparison in comparisons)
            {
                scannedCount++;
                if (!await this.PassportsRemainAvailableAsync(comparison, cancellationToken))
                {
                    continue;
                }

                results.Add(new ProfileComparisonSummaryResult(
                    comparison.ShareToken.Value,
                    string.Equals(
                        comparison.CreatorUserId,
                        normalizedUserId,
                        StringComparison.Ordinal)
                        ? comparison.Calculation.AcceptorDisplayName
                        : comparison.Calculation.CreatorDisplayName,
                    comparison.CreatedAtUtc,
                    comparison.Calculation.Categories,
                    comparison.IsModerationSuspended));
                if (results.Count == ManagementListLimit)
                {
                    break;
                }
            }

            if (comparisons.Count < pageSize || scannedCount >= ManagementScanLimit)
            {
                break;
            }

            ProfileComparison lastComparison = comparisons.Last();
            after = new ProfileComparisonListCursor(
                lastComparison.CreatedAtUtc,
                lastComparison.Id.Value);
        }

        return ApplicationResult<IReadOnlyCollection<ProfileComparisonSummaryResult>>.Success(
            results);
    }

    private async Task<bool> PassportsRemainAvailableAsync(
        ProfileComparison comparison,
        CancellationToken cancellationToken)
    {
        Task<ProfileComparisonPassportReference?> creatorTask =
            this.passportResolver.ResolveExactAsync(
                comparison.CreatorPassportPublicationId,
                comparison.CreatorUserId,
                comparison.CreatorPassportPublicationVersion,
                cancellationToken);
        Task<ProfileComparisonPassportReference?> acceptorTask =
            this.passportResolver.ResolveExactAsync(
                comparison.AcceptorPassportPublicationId,
                comparison.AcceptorUserId,
                comparison.AcceptorPassportPublicationVersion,
                cancellationToken);
        ProfileComparisonPassportReference?[] passports = await Task.WhenAll(
            creatorTask,
            acceptorTask);
        return IsUsable(passports[0], comparison.Calculation.Categories)
            && IsUsable(passports[1], comparison.Calculation.Categories);
    }

    private static bool IsUsable(
        ProfileComparisonPassportReference? passport,
        IReadOnlyCollection<ProfileComparisonCategory> categories)
    {
        return passport is not null
            && passport.Snapshot.Content.AllowsComparisons
            && ProfileComparisonCategoryPolicy.AllowsAll(passport.ContentPolicy, categories);
    }

    private static ApplicationResult<SharedProfileComparisonResult> NotFound()
    {
        return ApplicationResult<SharedProfileComparisonResult>.Failure(
            SharingApplicationErrors.ComparisonNotFound());
    }
}
