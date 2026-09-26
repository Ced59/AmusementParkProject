import {
  PublicHistoricalTimelineEntry,
  PublicParkHistoricalTimeline
} from '@app/models/history/public-park-history.models';
import { HistoryTimelinePageViewModel } from '../models/history-view.model';
import { ParkHistoryTimelinePageComponent } from './park-history-timeline-page.component';

interface TimelineSeoHarness {
  eventKey: (entry: PublicHistoricalTimelineEntry, index: number) => string;
  uncertainty: (entry: PublicHistoricalTimelineEntry) => string | null;
  eventDate: (entry: PublicHistoricalTimelineEntry) => string;
  factTypeLabel: (entry: PublicHistoricalTimelineEntry) => string;
  narrativeLink: (
    entry: PublicHistoricalTimelineEntry,
    timeline: PublicParkHistoricalTimeline
  ) => string[] | null;
  translateService: { instant: (key: string) => string };
  toSeoViewModel: (timeline: PublicParkHistoricalTimeline) => HistoryTimelinePageViewModel;
}

describe('park history timeline page SEO model', () => {
  it('uses the visible narrative URL for the matching structured-data event', () => {
    const articleLink: string[] = [
      '/', 'fr', 'park', 'park-1', 'parc-test', 'item', 'item-1', 'nom-actuel',
      'history', 'event-1', 'ouverture'
    ];
    const entry: PublicHistoricalTimelineEntry = createEntry();
    const timeline: PublicParkHistoricalTimeline = {
      parkId: 'park-1',
      parkName: 'Parc test',
      events: [entry],
      pagination: { currentPage: 1, itemsPerPage: 25, totalItems: 1, totalPages: 1 }
    };
    const harness: TimelineSeoHarness = Object.create(
      ParkHistoryTimelinePageComponent.prototype
    ) as unknown as TimelineSeoHarness;
    harness.eventKey = (): string => 'event-key';
    harness.uncertainty = (): null => null;
    harness.eventDate = (): string => '1998';
    harness.factTypeLabel = (): string => 'Ouverture';
    harness.narrativeLink = (): string[] => articleLink;
    harness.translateService = { instant: (key: string): string => key };

    const viewModel: HistoryTimelinePageViewModel = harness.toSeoViewModel(timeline);

    expect(viewModel.events[0]?.articleLink).toEqual(articleLink);
  });
});

function createEntry(): PublicHistoricalTimelineEntry {
  return {
    subjectType: 'ParkItem',
    subjectId: 'item-1',
    subjectLabel: 'Ancien nom',
    currentSubjectName: 'Nom actuel',
    factType: 'Opening',
    period: {
      start: { year: 1998, precision: 'Year' },
      end: { year: 1998, precision: 'Year' },
      startConfidence: 'Confirmed',
      endConfidence: 'Confirmed'
    },
    evidenceState: 'Verified',
    importance: 'Major',
    uncertaintyExplanations: [],
    sources: [],
    narrative: {
      eventId: 'event-1',
      slug: 'ouverture',
      titles: []
    }
  };
}
