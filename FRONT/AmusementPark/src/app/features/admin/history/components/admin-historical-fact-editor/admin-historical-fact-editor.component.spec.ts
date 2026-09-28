import { SimpleChange } from '@angular/core';

import {
  AdminHistoricalFact,
  AdminHistoricalSource,
  AdminHistoricalSubject,
  SaveHistoricalFactRequest
} from '@app/models/history/admin-historical-workbench.models';
import { AdminHistoricalFactEditorComponent } from './admin-historical-fact-editor.component';

describe('AdminHistoricalFactEditorComponent', () => {
  it('round-trips immutable evidence, the complete period and hidden canonical fields', () => {
    const subject: AdminHistoricalSubject = {
      type: 'ParkItem',
      id: 'item-1',
      label: 'Attraction historique',
      publicationPolicy: 'Public'
    };
    const source: AdminHistoricalSource = {
      id: 'source-1',
      revision: 3,
      type: 'OfficialWebsite',
      title: 'Source officielle',
      publisherOrAuthor: 'Parc',
      url: 'https://example.test/source',
      bibliographicReference: null,
      publishedOn: null,
      accessedOn: '2026-09-28',
      languageCode: 'fr',
      archiveUrl: null,
      scopes: ['SubjectIdentity', 'HistoricalLabel', 'FactType', 'Period'],
      adminNote: null,
      accessibility: 'Public',
      workflowState: 'Published',
      publicationState: 'Published'
    };
    const fact: AdminHistoricalFact = {
      id: 'fact-1',
      revision: 7,
      subject,
      type: 'Renaming',
      period: {
        start: {
          year: 1998,
          month: 4,
          day: null,
          precision: 'Month',
          isApproximate: true,
          qualifier: 'Circa'
        },
        end: {
          year: 2001,
          month: 9,
          day: 12,
          precision: 'Day',
          isApproximate: false,
          qualifier: 'Before'
        },
        startConfidence: 'Estimated',
        endConfidence: 'Disputed'
      },
      state: 'Verified',
      importance: 'Major',
      workflowState: 'Published',
      publicationState: 'Published',
      publicUncertaintyExplanation: [],
      lifecycleBoundaryMeaning: null,
      attributeKind: 'Name',
      attributeBoundaryMeaning: 'FirstDayOfNewValue',
      sequenceWithinDate: 4,
      sources: [{ sourceId: source.id, revision: 1, position: 'Supports' }],
      structuredValue: 'Nouveau nom',
      otherTypeLabel: null,
      narrativeContentId: 'story-42'
    };
    const component: AdminHistoricalFactEditorComponent = new AdminHistoricalFactEditorComponent();
    component.subjects = [subject];
    component.sources = [source];
    component.fact = fact;
    component.ngOnChanges({
      fact: new SimpleChange(null, fact, true),
      subjects: new SimpleChange([], [subject], true)
    });
    let request: SaveHistoricalFactRequest | null = null;
    component.submitted.subscribe((event): void => {
      request = event.request;
    });

    (component as unknown as { submit(): void }).submit();

    expect(request).not.toBeNull();
    expect(request!.period).toEqual(fact.period);
    expect(request!.sources).toEqual(fact.sources);
    expect(request!.sequenceWithinDate).toBe(4);
    expect(request!.narrativeContentId).toBe('story-42');
    expect(request!.attributeKind).toBe('Name');
    expect(request!.attributeBoundaryMeaning).toBe('FirstDayOfNewValue');
  });
});
