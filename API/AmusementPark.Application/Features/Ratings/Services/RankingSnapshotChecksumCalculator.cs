using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

public sealed class RankingSnapshotChecksumCalculator
{
    public RankingSnapshotChecksum CalculateChunk(IReadOnlyCollection<RankingSnapshotEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendInt32(hash, entries.Count);
        foreach (RankingSnapshotEntry entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            AppendInt32(hash, entry.Position);
            AppendInt32(hash, entry.Rank);
            AppendInt32(hash, (int)entry.TargetType);
            AppendString(hash, entry.TargetId);
            AppendNullableInt32(hash, entry.ParkItemCategory.HasValue
                ? (int)entry.ParkItemCategory.Value
                : null);
            AppendInt64(hash, BitConverter.DoubleToInt64Bits(entry.Score));
            RankingEvidence evidence = entry.Evidence;
            AppendInt32(hash, (int)evidence.Level);
            AppendInt32(hash, evidence.UniqueContributorCount);
            AppendInt32(hash, evidence.RatingObservationCount);
            AppendNullableInt32(hash, evidence.DirectParkContributorCount);
            AppendNullableInt32(hash, evidence.ItemContributorCount);
            AppendNullableInt32(hash, evidence.EligibleItemCount);
            AppendNullableInt32(hash, evidence.EligibleCategoryCount);
            AppendNullableBoolean(hash, evidence.IsSingleCategoryParkException);
            AppendString(hash, evidence.MethodologyVersion.Value);
            AppendNullableInt32(hash, evidence.NextContributorThreshold);
        }

        return Complete(hash);
    }

    public RankingSnapshotChecksum CalculateSnapshot(
        int totalEntryCount,
        int eligibleEntryCount,
        int chunkSize,
        IReadOnlyCollection<RankingSnapshotChunk> chunks)
    {
        if (totalEntryCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalEntryCount));
        }

        if (eligibleEntryCount < 0 || eligibleEntryCount > totalEntryCount)
        {
            throw new ArgumentOutOfRangeException(nameof(eligibleEntryCount));
        }

        if (chunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize));
        }

        ArgumentNullException.ThrowIfNull(chunks);
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendInt32(hash, totalEntryCount);
        AppendInt32(hash, eligibleEntryCount);
        AppendInt32(hash, chunkSize);
        AppendInt32(hash, chunks.Count);
        foreach (RankingSnapshotChunk chunk in chunks.OrderBy(static item => item.ChunkIndex))
        {
            ArgumentNullException.ThrowIfNull(chunk);
            AppendInt32(hash, chunk.ChunkIndex);
            AppendInt32(hash, chunk.Entries.Count);
            AppendString(hash, chunk.Checksum.Value);
        }

        return Complete(hash);
    }

    private static void AppendNullableInt32(IncrementalHash hash, int? value)
    {
        AppendInt32(hash, value.HasValue ? 1 : 0);
        if (value.HasValue)
        {
            AppendInt32(hash, value.Value);
        }
    }

    private static void AppendNullableBoolean(IncrementalHash hash, bool? value)
    {
        AppendInt32(hash, value.HasValue ? 1 : 0);
        if (value.HasValue)
        {
            AppendInt32(hash, value.Value ? 1 : 0);
        }
    }

    private static void AppendString(IncrementalHash hash, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        AppendInt32(hash, bytes.Length);
        hash.AppendData(bytes);
    }

    private static void AppendInt32(IncrementalHash hash, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        hash.AppendData(bytes);
    }

    private static void AppendInt64(IncrementalHash hash, long value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(bytes, value);
        hash.AppendData(bytes);
    }

    private static RankingSnapshotChecksum Complete(IncrementalHash hash)
    {
        string value = Convert.ToHexString(hash.GetHashAndReset()).ToLower(CultureInfo.InvariantCulture);
        return RankingSnapshotChecksum.Parse(value);
    }
}
