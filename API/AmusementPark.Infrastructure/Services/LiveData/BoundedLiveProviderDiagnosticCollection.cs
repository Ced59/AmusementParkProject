using System.Collections;
using AmusementPark.Application.Features.LiveData.Models;

namespace AmusementPark.Infrastructure.Services.LiveData;

internal sealed class BoundedLiveProviderDiagnosticCollection : ICollection<LiveProviderDiagnostic>
{
    private readonly ICollection<LiveProviderDiagnostic> inner;
    private readonly int maximumCount;

    public BoundedLiveProviderDiagnosticCollection(
        ICollection<LiveProviderDiagnostic> inner,
        int maximumCount)
    {
        ArgumentNullException.ThrowIfNull(inner);
        if (maximumCount <= 0 || inner.Count > maximumCount)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount));
        }

        this.inner = inner;
        this.maximumCount = maximumCount;
    }

    public int Count => this.inner.Count;

    public bool IsReadOnly => this.inner.IsReadOnly;

    public void Add(LiveProviderDiagnostic item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (this.inner.Count < this.maximumCount)
        {
            this.inner.Add(item);
        }
    }

    public void Clear()
    {
        this.inner.Clear();
    }

    public bool Contains(LiveProviderDiagnostic item)
    {
        return this.inner.Contains(item);
    }

    public void CopyTo(LiveProviderDiagnostic[] array, int arrayIndex)
    {
        this.inner.CopyTo(array, arrayIndex);
    }

    public bool Remove(LiveProviderDiagnostic item)
    {
        return this.inner.Remove(item);
    }

    public IEnumerator<LiveProviderDiagnostic> GetEnumerator()
    {
        return this.inner.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
