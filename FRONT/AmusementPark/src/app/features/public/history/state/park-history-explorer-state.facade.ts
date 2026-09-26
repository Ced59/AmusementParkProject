import { Injectable, Signal, computed } from '@angular/core';

import {
  PublicHistoricalTimelineEntry,
  PublicHistoricalSubjectSnapshot,
  PublicParkHistoricalSnapshot,
  PublicParkHistoricalTimeline
} from '@app/models/history/public-park-history.models';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

@Injectable()
export class ParkHistoryExplorerStateFacade {
  private readonly timelineStore = new SignalScreenStateStore<PublicParkHistoricalTimeline>();
  private readonly snapshotStore = new SignalScreenStateStore<PublicParkHistoricalSnapshot>();

  readonly timelineState = this.timelineStore.state;
  readonly snapshotState = this.snapshotStore.state;
  readonly timeline = this.timelineStore.data;
  readonly snapshot = this.snapshotStore.data;

  readonly parkIdentity: Signal<PublicHistoricalSubjectSnapshot | null> = computed(() =>
    this.snapshot()?.subjects.find((subject: PublicHistoricalSubjectSnapshot): boolean => subject.subjectType === 'Park') ?? null
  );
  readonly knownOpenSubjects: Signal<PublicHistoricalSubjectSnapshot[]> = computed(() =>
    this.snapshotSubjects('KnownOpen', 'ParkItem')
  );
  readonly possiblyOpenSubjects: Signal<PublicHistoricalSubjectSnapshot[]> = computed(() =>
    this.snapshotSubjects('PossiblyOpen', 'ParkItem')
  );
  readonly uncertainSubjects: Signal<PublicHistoricalSubjectSnapshot[]> = computed(() =>
    (this.snapshot()?.subjects ?? []).filter((subject: PublicHistoricalSubjectSnapshot): boolean =>
      subject.subjectType === 'ParkItem' && subject.operationalState === 'Unknown'
    )
  );
  readonly zones: Signal<PublicHistoricalSubjectSnapshot[]> = computed(() =>
    (this.snapshot()?.subjects ?? []).filter((subject: PublicHistoricalSubjectSnapshot): boolean => subject.subjectType === 'ParkZone')
  );
  readonly suggestedYears: Signal<number[]> = computed(() => {
    const years: number[] = (this.timeline()?.events ?? [])
      .flatMap((event) => [event.period.start?.year, event.period.end?.year])
      .filter((year): year is number => Number.isInteger(year));
    return [...new Set(years)].sort((left: number, right: number): number => left - right);
  });
  readonly nearbyEvents: Signal<PublicHistoricalTimelineEntry[]> = computed(() => {
    const requestedYear: number | undefined = this.snapshot()?.requestedInstant.year;
    if (!requestedYear) {
      return [];
    }

    return (this.timeline()?.events ?? []).filter((event: PublicHistoricalTimelineEntry): boolean => {
      const eventYear: number | undefined = event.period.start?.year ?? event.period.end?.year;
      return eventYear !== undefined && Math.abs(eventYear - requestedYear) <= 1;
    });
  });

  setResolvedTimeline(timeline: PublicParkHistoricalTimeline | null): void {
    if (timeline) {
      this.timelineStore.setReady(timeline);
      return;
    }

    this.timelineStore.setError('history.explorer.errorMessage');
  }

  setResolvedSnapshot(
    snapshot: PublicParkHistoricalSnapshot | null,
    timeline: PublicParkHistoricalTimeline | null
  ): void {
    if (snapshot) {
      this.snapshotStore.setReady(snapshot);
    } else {
      this.snapshotStore.setError('history.explorer.errorMessage');
    }

    if (timeline) {
      this.timelineStore.setReady(timeline);
    } else {
      this.timelineStore.setEmpty();
    }
  }

  private snapshotSubjects(
    operationalState: string,
    subjectType: string
  ): PublicHistoricalSubjectSnapshot[] {
    return (this.snapshot()?.subjects ?? []).filter((subject: PublicHistoricalSubjectSnapshot): boolean =>
      subject.subjectType === subjectType && subject.operationalState === operationalState
    );
  }
}
