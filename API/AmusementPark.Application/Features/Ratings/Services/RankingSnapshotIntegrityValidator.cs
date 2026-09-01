using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

public sealed class RankingSnapshotIntegrityValidator
{
    private readonly RankingSnapshotChecksumCalculator checksumCalculator;

    public RankingSnapshotIntegrityValidator(RankingSnapshotChecksumCalculator checksumCalculator)
    {
        this.checksumCalculator = checksumCalculator;
    }

    public RankingSnapshotIntegrityResult Validate(
        RankingSnapshotHeader header,
        IReadOnlyCollection<RankingSnapshotChunk> chunks,
        RankingScopeDefinition scope)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(scope);

        if (header.ScopeKey != scope.Key || header.MethodologyVersion != scope.MethodologyVersion)
        {
            return RankingSnapshotIntegrityResult.Invalid(RankingSnapshotErrorCodes.MethodologyMismatch);
        }

        if (chunks.Count != header.ChunkCount)
        {
            return RankingSnapshotIntegrityResult.Invalid(RankingSnapshotErrorCodes.ChunkCountMismatch);
        }

        List<RankingSnapshotChunk> orderedChunks = chunks
            .OrderBy(static chunk => chunk.ChunkIndex)
            .ToList();
        HashSet<(RatingTargetType TargetType, string TargetId)> targetKeys =
            new HashSet<(RatingTargetType TargetType, string TargetId)>();
        int entryCount = 0;
        int expectedRank = 1;
        for (int chunkIndex = 0; chunkIndex < orderedChunks.Count; chunkIndex++)
        {
            RankingSnapshotChunk chunk = orderedChunks[chunkIndex];
            if (chunk.SnapshotId != header.Id || chunk.ChunkIndex != chunkIndex)
            {
                return RankingSnapshotIntegrityResult.Invalid(RankingSnapshotErrorCodes.ChunkIndexMismatch);
            }

            int expectedChunkEntryCount = chunkIndex == header.ChunkCount - 1
                ? header.EligibleEntryCount - (chunkIndex * header.ChunkSize)
                : header.ChunkSize;
            if (chunk.Entries.Count != expectedChunkEntryCount)
            {
                return RankingSnapshotIntegrityResult.Invalid(RankingSnapshotErrorCodes.ChunkSizeInvalid);
            }

            if (this.checksumCalculator.CalculateChunk(chunk.Entries) != chunk.Checksum)
            {
                return RankingSnapshotIntegrityResult.Invalid(RankingSnapshotErrorCodes.ChunkChecksumMismatch);
            }

            foreach (RankingSnapshotEntry entry in chunk.Entries)
            {
                if (entry.Rank != expectedRank)
                {
                    return RankingSnapshotIntegrityResult.Invalid(RankingSnapshotErrorCodes.RankSequenceInvalid);
                }

                RatingTargetType expectedTargetType = scope.TargetFamily == RankingTargetFamily.Parks
                    ? RatingTargetType.Park
                    : RatingTargetType.ParkItem;
                if (entry.TargetType != expectedTargetType)
                {
                    return RankingSnapshotIntegrityResult.Invalid(RankingSnapshotErrorCodes.TargetFamilyMismatch);
                }

                if (entry.Evidence.MethodologyVersion != header.MethodologyVersion)
                {
                    return RankingSnapshotIntegrityResult.Invalid(RankingSnapshotErrorCodes.MethodologyMismatch);
                }

                if (!targetKeys.Add((entry.TargetType, entry.TargetId)))
                {
                    return RankingSnapshotIntegrityResult.Invalid(RankingSnapshotErrorCodes.DuplicateTarget);
                }

                expectedRank++;
                entryCount++;
            }
        }

        if (entryCount != header.EligibleEntryCount)
        {
            return RankingSnapshotIntegrityResult.Invalid(RankingSnapshotErrorCodes.EntryCountMismatch);
        }

        RankingSnapshotChecksum snapshotChecksum = this.checksumCalculator.CalculateSnapshot(
            header.TotalEntryCount,
            header.EligibleEntryCount,
            header.ChunkSize,
            orderedChunks);
        return snapshotChecksum == header.Checksum
            ? RankingSnapshotIntegrityResult.Valid
            : RankingSnapshotIntegrityResult.Invalid(RankingSnapshotErrorCodes.SnapshotChecksumMismatch);
    }
}
