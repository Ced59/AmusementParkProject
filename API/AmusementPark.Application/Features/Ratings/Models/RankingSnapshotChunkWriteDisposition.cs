namespace AmusementPark.Application.Features.Ratings.Models;

public enum RankingSnapshotChunkWriteDisposition
{
    Written,
    AlreadyWritten,
    Conflict,
    BuildNotWritable,
}
