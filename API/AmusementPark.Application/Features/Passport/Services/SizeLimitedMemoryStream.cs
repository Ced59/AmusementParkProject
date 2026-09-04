namespace AmusementPark.Application.Features.Passport.Services;

internal sealed class SizeLimitedMemoryStream : MemoryStream
{
    private readonly long maximumLength;

    public SizeLimitedMemoryStream(long maximumLength)
    {
        if (maximumLength < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumLength));
        }

        this.maximumLength = maximumLength;
    }

    public override void SetLength(long value)
    {
        this.EnsureLength(value);
        base.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        this.EnsureWrite(count);
        base.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        this.EnsureWrite(buffer.Length);
        base.Write(buffer);
    }

    public override void WriteByte(byte value)
    {
        this.EnsureWrite(1);
        base.WriteByte(value);
    }

    public override Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        this.EnsureWrite(count);
        return base.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        this.EnsureWrite(buffer.Length);
        return base.WriteAsync(buffer, cancellationToken);
    }

    private void EnsureWrite(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        this.EnsureLength(checked(this.Position + count));
    }

    private void EnsureLength(long length)
    {
        if (length > this.maximumLength)
        {
            throw new PassportExportSizeLimitException();
        }
    }
}
