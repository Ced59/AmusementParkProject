import {
  PublicHistoricalTimelineEntry,
  PublicParkHistoricalTimeline
} from '@app/models/history/public-park-history.models';
import { buildCanonicalHistoryNarrativeLink } from './history-narrative-link';

describe('history narrative link', () => {
  const timeline: PublicParkHistoricalTimeline = {
    parkId: 'park-1',
    parkName: 'Parc Astérix',
    events: [],
    pagination: { currentPage: 1, itemsPerPage: 25, totalItems: 1, totalPages: 1 }
  };

  it('uses the returned park name for a stable canonical park article route', () => {
    const entry: PublicHistoricalTimelineEntry = createEntry('Park', 'park-1', 'Parc Astérix');

    expect(buildCanonicalHistoryNarrativeLink(entry, timeline, 'fr')).toEqual([
      '/', 'fr', 'park', 'park-1', 'parc-asterix', 'history', 'event-1', 'grande-ouverture'
    ]);
  });

  it('keeps park item narratives under their contextual item route', () => {
    const entry: PublicHistoricalTimelineEntry = createEntry(
      'ParkItem',
      'item-1',
      'Ancien Grand Huit',
      'Le Grand Huit'
    );

    expect(buildCanonicalHistoryNarrativeLink(entry, timeline, 'fr')).toEqual([
      '/', 'fr', 'park', 'park-1', 'parc-asterix', 'item', 'item-1', 'le-grand-huit',
      'history', 'event-1', 'grande-ouverture'
    ]);
  });
});

function createEntry(
  subjectType: string,
  subjectId: string,
  subjectLabel: string,
  currentSubjectName: string | null = null
): PublicHistoricalTimelineEntry {
  return {
    subjectType,
    subjectId,
    subjectLabel,
    currentSubjectName,
    factType: 'Opening',
    period: {
      start: { year: 1989, precision: 'Year' },
      end: { year: 1989, precision: 'Year' },
      startConfidence: 'Confirmed',
      endConfidence: 'Confirmed'
    },
    evidenceState: 'Verified',
    importance: 'Major',
    uncertaintyExplanations: [],
    sources: [],
    narrative: {
      eventId: 'event-1',
      slug: null,
      titles: [{ languageCode: 'fr', value: 'Grande ouverture' }]
    }
  };
}
