namespace AmusementPark.WebAPI.Contracts.TechnicalStats;

public sealed class TechnicalStatsStorageSummaryDto
{
    public int MemoryEntries { get; set; }

    public int MemoryMaxEntries { get; set; }

    public bool DiskEnabled { get; set; }

    public int DiskEntries { get; set; }

    public long DiskBytes { get; set; }

    public long DiskMaxBytes { get; set; }

    public long DiskWrites { get; set; }

    public int TechnicalStatsPersistenceEntries { get; set; }

    public long TechnicalStatsPersistenceBytes { get; set; }

    public int TechnicalStatsPersistencePurgedBuckets { get; set; }

    public int SeoDocumentEntries { get; set; }

    public int SeoDocumentMaxEntries { get; set; }

    public long SeoDocumentRequests { get; set; }

    public long SeoDocumentHits { get; set; }

    public long SeoDocumentMisses { get; set; }

    public long AssetMisses { get; set; }
}
