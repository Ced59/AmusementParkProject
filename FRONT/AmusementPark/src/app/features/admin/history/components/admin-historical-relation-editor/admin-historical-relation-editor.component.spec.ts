import { SimpleChange } from '@angular/core';

import {
  AdminHistoricalRelation,
  AdminHistoricalSource,
  AdminHistoricalSubject,
  SaveHistoricalRelationRequest
} from '@app/models/history/admin-historical-workbench.models';
import { AdminHistoricalRelationEditorComponent } from './admin-historical-relation-editor.component';

describe('AdminHistoricalRelationEditorComponent', () => {
  it('round-trips immutable evidence and the complete relation period', () => {
    const sourceSubject: AdminHistoricalSubject = {
      type: 'ParkItem',
      id: 'item-1',
      label: 'Ancien nom',
      publicationPolicy: 'Public'
    };
    const targetSubject: AdminHistoricalSubject = {
      type: 'ParkItem',
      id: 'item-2',
      label: 'Nouveau nom',
      publicationPolicy: 'Public'
    };
    const source: AdminHistoricalSource = {
      id: 'source-1',
      revision: 6,
      type: 'OfficialWebsite',
      title: 'Source officielle',
      publisherOrAuthor: 'Parc',
      url: 'https://example.test/source',
      bibliographicReference: null,
      publishedOn: null,
      accessedOn: '2026-09-28',
      languageCode: 'fr',
      archiveUrl: null,
      scopes: ['RelationSourceIdentity', 'RelationTargetIdentity', 'RelationType', 'Period'],
      adminNote: null,
      accessibility: 'Public',
      workflowState: 'Published',
      publicationState: 'Published'
    };
    const relation: AdminHistoricalRelation = {
      id: 'relation-1',
      revision: 5,
      source: sourceSubject,
      target: targetSubject,
      type: 'RenamedTo',
      direction: 'Directed',
      period: {
        start: {
          year: 1997,
          month: 3,
          day: 8,
          precision: 'Day',
          isApproximate: true,
          qualifier: 'Circa'
        },
        end: {
          year: 1998,
          month: 10,
          day: null,
          precision: 'Month',
          isApproximate: false,
          qualifier: 'Before'
        },
        startConfidence: 'Estimated',
        endConfidence: 'Disputed'
      },
      state: 'Verified',
      workflowState: 'Published',
      publicationState: 'Published',
      publicUncertaintyExplanation: [],
      sources: [{ sourceId: source.id, revision: 2, position: 'Contradicts' }],
      editorialNote: 'Relation documentée.'
    };
    const component: AdminHistoricalRelationEditorComponent = new AdminHistoricalRelationEditorComponent();
    component.subjects = [sourceSubject, targetSubject];
    component.sources = [source];
    component.relation = relation;
    component.ngOnChanges({
      relation: new SimpleChange(null, relation, true),
      subjects: new SimpleChange([], [sourceSubject, targetSubject], true)
    });
    let request: SaveHistoricalRelationRequest | null = null;
    component.submitted.subscribe((event): void => {
      request = event.request;
    });

    (component as unknown as { submit(): void }).submit();

    expect(request).not.toBeNull();
    expect(request!.period).toEqual(relation.period);
    expect(request!.sources).toEqual(relation.sources);
  });
});
