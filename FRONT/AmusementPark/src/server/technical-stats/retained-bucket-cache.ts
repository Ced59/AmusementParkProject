export interface DatedBucket {
  readonly date: string;
}

export class RetainedBucketCache<TBucket extends DatedBucket> {
  private snapshot: TBucket[] | null = null;

  getOrLoad(loader: () => TBucket[]): readonly TBucket[] {
    if (this.snapshot === null) {
      this.snapshot = this.sort(loader());
    }
    return this.snapshot;
  }

  replace(bucket: TBucket): void {
    if (this.snapshot === null) {
      return;
    }
    this.snapshot = this.sort([
      ...this.snapshot.filter((existingBucket: TBucket): boolean => existingBucket.date !== bucket.date),
      bucket
    ]);
  }

  invalidate(): void {
    this.snapshot = null;
  }

  private sort(buckets: TBucket[]): TBucket[] {
    return [...buckets].sort((left: TBucket, right: TBucket): number => left.date.localeCompare(right.date));
  }
}
