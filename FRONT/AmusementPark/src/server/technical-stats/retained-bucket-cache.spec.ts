import { RetainedBucketCache } from './retained-bucket-cache';

interface TestBucket {
  readonly date: string;
  readonly requests: number;
}

describe('RetainedBucketCache', () => {
  it('loads retained buckets once and keeps them sorted', () => {
    const cache: RetainedBucketCache<TestBucket> = new RetainedBucketCache<TestBucket>();
    const loader: jasmine.Spy<() => TestBucket[]> = jasmine.createSpy('loader').and.returnValue([
      { date: '2026-07-23', requests: 2 },
      { date: '2026-07-22', requests: 1 }
    ]);

    expect(cache.getOrLoad(loader).map((bucket: TestBucket): string => bucket.date))
      .toEqual(['2026-07-22', '2026-07-23']);
    expect(cache.getOrLoad(loader).length).toBe(2);
    expect(loader).toHaveBeenCalledTimes(1);
  });

  it('replaces a written bucket without reloading retained files', () => {
    const cache: RetainedBucketCache<TestBucket> = new RetainedBucketCache<TestBucket>();
    const loader: jasmine.Spy<() => TestBucket[]> = jasmine.createSpy('loader').and.returnValue([
      { date: '2026-07-22', requests: 1 }
    ]);
    cache.getOrLoad(loader);

    cache.replace({ date: '2026-07-22', requests: 5 });

    expect(cache.getOrLoad(loader)).toEqual([{ date: '2026-07-22', requests: 5 }]);
    expect(loader).toHaveBeenCalledTimes(1);
  });

  it('reloads retained files after a purge invalidates the snapshot', () => {
    const cache: RetainedBucketCache<TestBucket> = new RetainedBucketCache<TestBucket>();
    const loader: jasmine.Spy<() => TestBucket[]> = jasmine.createSpy('loader')
      .and.returnValues(
        [{ date: '2026-07-21', requests: 1 }, { date: '2026-07-22', requests: 2 }],
        [{ date: '2026-07-22', requests: 2 }]
      );
    cache.getOrLoad(loader);

    cache.invalidate();

    expect(cache.getOrLoad(loader)).toEqual([{ date: '2026-07-22', requests: 2 }]);
    expect(loader).toHaveBeenCalledTimes(2);
  });
});
