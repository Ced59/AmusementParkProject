import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of, Subject, throwError } from 'rxjs';

import {
  PassportRideOccurrence,
  PassportRideOccurrenceMutationResult
} from '@app/models/passport/passport-ride-occurrence.models';
import { PassportVisit } from '@app/models/passport/passport-visit.models';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import {
  PASSPORT_VISIT_EDITOR_ATTRACTIONS_PORT,
  PASSPORT_VISIT_EDITOR_OCCURRENCES_PORT,
  PASSPORT_VISIT_EDITOR_OPERATION_ID_PORT,
  PASSPORT_VISIT_EDITOR_PARKS_PORT,
  PASSPORT_VISIT_EDITOR_VISITS_PORT,
  PASSPORT_VISIT_EDITOR_ZONES_PORT
} from './passport-visit-editor-data.ports';
import { PassportVisitEditorStateFacade } from './passport-visit-editor-state.facade';

describe('PassportVisitEditorStateFacade', () => {
  const visit: PassportVisit = createVisit();
  const firstOccurrence: PassportRideOccurrence = createOccurrence('occurrence-1', 'ride-1', 1024);
  const secondOccurrence: PassportRideOccurrence = createOccurrence('occurrence-2', 'ride-2', 2048);
  let visitsPort: {
    getVisit: ReturnType<typeof vi.fn>;
    updateVisit: ReturnType<typeof vi.fn>;
    completeVisit: ReturnType<typeof vi.fn>;
    reopenVisit: ReturnType<typeof vi.fn>;
    archiveVisit: ReturnType<typeof vi.fn>;
    upsertParkAssessment: ReturnType<typeof vi.fn>;
    deleteParkAssessment: ReturnType<typeof vi.fn>;
  };
  let occurrencesPort: {
    list: ReturnType<typeof vi.fn>;
    get: ReturnType<typeof vi.fn>;
    addBatch: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
    reorder: ReturnType<typeof vi.fn>;
    upsertAssessment: ReturnType<typeof vi.fn>;
    deleteAssessment: ReturnType<typeof vi.fn>;
  };
  let parksPort: { getParkById: ReturnType<typeof vi.fn> };
  let zonesPort: { getParkZonesByParkId: ReturnType<typeof vi.fn> };
  let attractionsPort: { getParkItemsByParkIdPage: ReturnType<typeof vi.fn> };
  let operationIds: { create: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    visitsPort = {
      getVisit: vi.fn().mockReturnValue(of(visit)),
      updateVisit: vi.fn(),
      completeVisit: vi.fn(),
      reopenVisit: vi.fn(),
      archiveVisit: vi.fn(),
      upsertParkAssessment: vi.fn(),
      deleteParkAssessment: vi.fn()
    };
    occurrencesPort = {
      list: vi.fn().mockReturnValue(of({ items: [firstOccurrence, secondOccurrence], nextCursor: null })),
      get: vi.fn(),
      addBatch: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
      reorder: vi.fn(),
      upsertAssessment: vi.fn(),
      deleteAssessment: vi.fn()
    };
    parksPort = {
      getParkById: vi.fn().mockReturnValue(of({ id: 'park-1', name: 'Test Park', latitude: 1, longitude: 2 }))
    };
    zonesPort = {
      getParkZonesByParkId: vi.fn().mockReturnValue(of([{
        id: 'zone-1',
        parkId: 'park-1',
        names: [
          { languageCode: 'fr', value: 'Le Village' },
          { languageCode: 'de', value: 'Das Dorf' }
        ]
      }]))
    };
    attractionsPort = {
      getParkItemsByParkIdPage: vi.fn().mockReturnValue(of({
        items: [{
          id: 'ride-1',
          parkId: 'park-1',
          name: 'Grand Huit',
          category: 'Attraction',
          type: 'RollerCoaster',
          latitude: null,
          longitude: null,
          attractionDetails: { status: 'Operating' }
        }],
        pagination: { currentPage: 1, itemsPerPage: 24, totalItems: 1, totalPages: 1 }
      }))
    };
    operationIds = { create: vi.fn().mockReturnValue('operation-stable') };

    TestBed.configureTestingModule({
      providers: [
        PassportVisitEditorStateFacade,
        { provide: PASSPORT_VISIT_EDITOR_VISITS_PORT, useValue: visitsPort },
        { provide: PASSPORT_VISIT_EDITOR_OCCURRENCES_PORT, useValue: occurrencesPort },
        { provide: PASSPORT_VISIT_EDITOR_PARKS_PORT, useValue: parksPort },
        { provide: PASSPORT_VISIT_EDITOR_ZONES_PORT, useValue: zonesPort },
        { provide: PASSPORT_VISIT_EDITOR_ATTRACTIONS_PORT, useValue: attractionsPort },
        { provide: PASSPORT_VISIT_EDITOR_OPERATION_ID_PORT, useValue: operationIds },
        { provide: ToastMessageService, useValue: { add: vi.fn() } },
        { provide: TranslateService, useValue: { instant: (key: string): string => key } }
      ]
    });
  });

  it('loads the owned visit, historical catalogue, localized zones and ordered timeline', () => {
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);

    facade.load('visit-1', 'fr');

    expect(facade.visit()).toEqual(visit);
    expect(facade.parkName()).toBe('Test Park');
    expect(facade.zones()).toEqual([{ id: 'zone-1', name: 'Le Village' }]);
    expect(facade.attractions()[0]).toEqual(expect.objectContaining({ id: 'ride-1', name: 'Grand Huit' }));
    expect(facade.occurrences().map((occurrence: PassportRideOccurrence): string => occurrence.id)).toEqual([
      'occurrence-1',
      'occurrence-2'
    ]);
    expect(occurrencesPort.list).toHaveBeenCalledWith('visit-1', null, 50);
    expect(attractionsPort.getParkItemsByParkIdPage).toHaveBeenCalledWith(
      'park-1',
      1,
      24,
      { closedFilter: 'all', category: 'Attraction', search: null, zoneId: null },
      { closedFilter: 'all' }
    );
  });

  it('updates visit metadata through the port and refreshes the optimistic version', () => {
    const updatedVisit: PassportVisit = {
      ...visit,
      date: { year: 2025, month: null, day: null, precision: 'Year', isApproximate: true },
      title: 'Souvenir',
      version: 2
    };
    visitsPort.updateVisit.mockReturnValue(of(updatedVisit));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.updateVisitMetadataDraft({
      precision: 'Year',
      year: 2025,
      isApproximate: true,
      title: ' Souvenir '
    });

    facade.saveVisitMetadata();

    expect(visitsPort.updateVisit).toHaveBeenCalledWith('visit-1', expect.objectContaining({
      date: { year: 2025, month: null, day: null, precision: 'Year', isApproximate: true },
      title: 'Souvenir',
      expectedVersion: 1
    }));
    expect(facade.visit()?.version).toBe(2);
    expect(facade.metadataHasChanges()).toBe(false);
  });

  it('preserves submitted metadata when conflict reconciliation loads another version', () => {
    const concurrentVisit: PassportVisit = {
      ...visit,
      title: 'Version distante',
      version: 2
    };
    visitsPort.updateVisit.mockReturnValue(throwError(() => new HttpErrorResponse({
      status: 409,
      error: {
        status: 409,
        title: 'Conflict',
        errorCode: 'visit.version-conflict'
      }
    })));
    visitsPort.getVisit
      .mockReturnValueOnce(of(visit))
      .mockReturnValueOnce(of(concurrentVisit));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.updateVisitMetadataDraft({ title: 'Ma correction' });

    facade.saveVisitMetadata();

    expect(facade.visit()?.version).toBe(2);
    expect(facade.metadataDraft().title).toBe('Ma correction');
    expect(facade.metadataHasChanges()).toBe(true);
    expect(facade.visitMutationErrorKey()).toBe('passport.editor.visit.errors.conflict');
  });

  it('explains why temporal metadata stays locked when the visit contains rides', () => {
    visitsPort.updateVisit.mockReturnValue(throwError(() => new HttpErrorResponse({
      status: 409,
      error: {
        status: 409,
        title: 'Conflict',
        errorCode: 'visit.temporal-metadata-locked'
      }
    })));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.updateVisitMetadataDraft({ year: 2025 });

    facade.saveVisitMetadata();

    expect(facade.metadataDraft().year).toBe(2025);
    expect(facade.metadataHasChanges()).toBe(true);
    expect(facade.visitMutationErrorKey())
      .toBe('passport.editor.visit.errors.temporalMetadataLocked');
  });

  it('requires an explicit lifecycle transition and reconciles a lost completion response', () => {
    const completedVisit: PassportVisit = {
      ...visit,
      status: 'Completed',
      version: 2,
      completedAtUtc: '2026-09-03T10:30:00Z'
    };
    visitsPort.completeVisit.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 0 })));
    visitsPort.getVisit
      .mockReturnValueOnce(of(visit))
      .mockReturnValueOnce(of(completedVisit));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');

    facade.completeVisit();

    expect(visitsPort.completeVisit).toHaveBeenCalledWith('visit-1', 1);
    expect(facade.visit()?.status).toBe('Completed');
    expect(facade.canEditVisit()).toBe(false);
    expect(facade.visitMutationErrorKey()).toBeNull();
  });

  it('does not complete a visit while its park assessment draft is unsaved', () => {
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.updateParkAssessmentDraft({ value: 4.5, privateComment: 'Brouillon local' });

    facade.completeVisit();

    expect(facade.hasUnsavedAssessmentChanges()).toBe(true);
    expect(facade.hasUnsavedStatusTransitionChanges()).toBe(true);
    expect(visitsPort.completeVisit).not.toHaveBeenCalled();
    expect(facade.visitMutationErrorKey()).toBe('passport.editor.visit.errors.saveBeforeStatus');
  });

  it('does not archive a visit while one ride assessment draft is unsaved', () => {
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.updateRideAssessmentDraft('occurrence-1', {
      value: 4,
      privateComment: 'À conserver'
    });

    facade.archiveVisit();

    expect(facade.hasUnsavedAssessmentChanges()).toBe(true);
    expect(facade.hasUnsavedStatusTransitionChanges()).toBe(true);
    expect(visitsPort.archiveVisit).not.toHaveBeenCalled();
    expect(facade.visitMutationErrorKey()).toBe('passport.editor.visit.errors.saveBeforeStatus');
  });

  it('does not complete a visit while attractions are still selected for addition', () => {
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.toggleAttraction(facade.attractions()[0]);

    facade.completeVisit();

    expect(facade.hasUnsavedOccurrenceChanges()).toBe(true);
    expect(facade.hasUnsavedStatusTransitionChanges()).toBe(true);
    expect(visitsPort.completeVisit).not.toHaveBeenCalled();
    expect(facade.visitMutationErrorKey()).toBe('passport.editor.visit.errors.saveBeforeStatus');
  });

  it('does not archive a visit while one occurrence edit is unsaved', () => {
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.updateOccurrenceDraft('occurrence-1', { privateNote: 'Brouillon local' });

    facade.archiveVisit();

    expect(facade.hasUnsavedOccurrenceChanges()).toBe(true);
    expect(facade.hasUnsavedStatusTransitionChanges()).toBe(true);
    expect(visitsPort.archiveVisit).not.toHaveBeenCalled();
    expect(facade.visitMutationErrorKey()).toBe('passport.editor.visit.errors.saveBeforeStatus');
  });

  it('refreshes localized data without losing pending selections or edit drafts', () => {
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.toggleAttraction(facade.attractions()[0]);
    facade.updateSelection('ride-1', { count: 2, privateNote: 'À ajouter' });
    facade.updateOccurrenceDraft('occurrence-1', { privateNote: 'À conserver' });

    facade.changeLanguage('de');

    expect(facade.zones()).toEqual([{ id: 'zone-1', name: 'Das Dorf' }]);
    expect(facade.selectedAttractions()).toEqual([
      expect.objectContaining({ parkItemId: 'ride-1', count: 2, privateNote: 'À ajouter' })
    ]);
    expect(facade.editDrafts()['occurrence-1']).toEqual(expect.objectContaining({ privateNote: 'À conserver' }));
    expect(visitsPort.getVisit).toHaveBeenCalledTimes(1);
    expect(attractionsPort.getParkItemsByParkIdPage).toHaveBeenCalledTimes(2);
  });

  it('ignores a stale localized response after a newer language has loaded', () => {
    const delayedGermanPark = new Subject<{ id: string; name: string; latitude: number; longitude: number }>();
    parksPort.getParkById
      .mockReturnValueOnce(of({ id: 'park-1', name: 'Parc initial', latitude: 1, longitude: 2 }))
      .mockReturnValueOnce(delayedGermanPark)
      .mockReturnValueOnce(of({ id: 'park-1', name: 'Nederlands park', latitude: 1, longitude: 2 }));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');

    facade.changeLanguage('de');
    facade.changeLanguage('nl');
    delayedGermanPark.next({ id: 'park-1', name: 'Deutscher Park', latitude: 1, longitude: 2 });
    delayedGermanPark.complete();

    expect(facade.parkName()).toBe('Nederlands park');
  });

  it('retries a failed locale refresh without losing pending drafts', () => {
    occurrencesPort.list
      .mockReturnValueOnce(of({ items: [firstOccurrence, secondOccurrence], nextCursor: null }))
      .mockReturnValueOnce(throwError(() => new Error('temporary failure')))
      .mockReturnValueOnce(of({ items: [firstOccurrence, secondOccurrence], nextCursor: null }));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.toggleAttraction(facade.attractions()[0]);
    facade.updateSelection('ride-1', { count: 2, privateNote: 'À ajouter' });
    facade.updateOccurrenceDraft('occurrence-1', { privateNote: 'À conserver' });
    facade.changeLanguage('de');

    facade.retryLoad();

    expect(facade.loadErrorKey()).toBeNull();
    expect(facade.selectedAttractions()).toEqual([
      expect.objectContaining({ parkItemId: 'ride-1', count: 2, privateNote: 'À ajouter' })
    ]);
    expect(facade.editDrafts()['occurrence-1']).toEqual(expect.objectContaining({ privateNote: 'À conserver' }));
  });

  it('does not let a delayed locale refresh overwrite a newer timeline reload', () => {
    const delayedGermanPark = new Subject<{ id: string; name: string; latitude: number; longitude: number }>();
    parksPort.getParkById
      .mockReturnValueOnce(of({ id: 'park-1', name: 'Parc initial', latitude: 1, longitude: 2 }))
      .mockReturnValueOnce(delayedGermanPark);
    occurrencesPort.list
      .mockReturnValueOnce(of({ items: [firstOccurrence], nextCursor: null }))
      .mockReturnValueOnce(of({ items: [firstOccurrence], nextCursor: null }))
      .mockReturnValueOnce(of({ items: [secondOccurrence], nextCursor: null }));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');

    facade.changeLanguage('de');
    facade.reloadTimeline();
    delayedGermanPark.next({ id: 'park-1', name: 'Deutscher Park', latitude: 1, longitude: 2 });
    delayedGermanPark.complete();

    expect(facade.occurrences().map((occurrence: PassportRideOccurrence): string => occurrence.id)).toEqual([
      'occurrence-2'
    ]);
  });

  it('loads the temporal park assessment into an initially clean private draft', () => {
    const assessedVisit: PassportVisit = {
      ...visit,
      version: 2,
      parkAssessment: {
        value: 4.5,
        privateComment: 'Belle journée',
        revision: 1,
        createdAtUtc: '2026-09-03T10:00:00Z',
        updatedAtUtc: '2026-09-03T10:00:00Z'
      }
    };
    visitsPort.getVisit.mockReturnValue(of(assessedVisit));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);

    facade.load('visit-1', 'fr');

    expect(facade.assessmentDraft()).toEqual({ value: 4.5, privateComment: 'Belle journée' });
    expect(facade.assessmentHasChanges()).toBe(false);
    expect(facade.assessmentCanSave()).toBe(false);
  });

  it('saves one park assessment with the current parent version', () => {
    const updatedVisit: PassportVisit = {
      ...visit,
      version: 2,
      parkAssessment: {
        value: 4.5,
        privateComment: 'Belle journée',
        revision: 1,
        createdAtUtc: '2026-09-03T10:00:00Z',
        updatedAtUtc: '2026-09-03T10:00:00Z'
      }
    };
    visitsPort.upsertParkAssessment.mockReturnValue(of(updatedVisit));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.updateParkAssessmentDraft({ value: 4.5, privateComment: ' Belle journée ' });

    facade.saveParkAssessment();

    expect(visitsPort.upsertParkAssessment).toHaveBeenCalledWith('visit-1', {
      value: 4.5,
      privateComment: 'Belle journée',
      expectedVersion: 1
    });
    expect(facade.visit()).toEqual(updatedVisit);
    expect(facade.assessmentDraft()).toEqual({ value: 4.5, privateComment: 'Belle journée' });
    expect(facade.assessmentHasChanges()).toBe(false);
  });

  it('preserves a newer draft when the assessment response arrives', () => {
    const response: Subject<PassportVisit> = new Subject<PassportVisit>();
    visitsPort.upsertParkAssessment.mockReturnValue(response);
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.updateParkAssessmentDraft({ value: 4, privateComment: 'Première saisie' });
    facade.saveParkAssessment();

    facade.updateParkAssessmentDraft({ value: 4.5, privateComment: 'Saisie plus récente' });
    response.next({
      ...visit,
      version: 2,
      parkAssessment: {
        value: 4,
        privateComment: 'Première saisie',
        revision: 1,
        createdAtUtc: '2026-09-03T10:00:00Z',
        updatedAtUtc: '2026-09-03T10:00:00Z'
      }
    });

    expect(facade.assessmentDraft()).toEqual({ value: 4.5, privateComment: 'Saisie plus récente' });
    expect(facade.assessmentHasChanges()).toBe(true);
  });

  it('does not let a delayed locale refresh restore the assessment version saved meanwhile', () => {
    const delayedGermanPark = new Subject<{ id: string; name: string; latitude: number; longitude: number }>();
    const updatedVisit: PassportVisit = {
      ...visit,
      version: 2,
      parkAssessment: {
        value: 4.5,
        privateComment: 'Belle journée',
        revision: 1,
        createdAtUtc: '2026-09-03T10:00:00Z',
        updatedAtUtc: '2026-09-03T10:00:00Z'
      }
    };
    parksPort.getParkById
      .mockReturnValueOnce(of({ id: 'park-1', name: 'Parc initial', latitude: 1, longitude: 2 }))
      .mockReturnValueOnce(delayedGermanPark);
    visitsPort.upsertParkAssessment.mockReturnValue(of(updatedVisit));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');

    facade.changeLanguage('de');
    facade.updateParkAssessmentDraft({ value: 4.5, privateComment: 'Belle journée' });
    facade.saveParkAssessment();
    delayedGermanPark.next({ id: 'park-1', name: 'Deutscher Park', latitude: 1, longitude: 2 });
    delayedGermanPark.complete();

    expect(facade.visit()).toEqual(updatedVisit);
    expect(facade.assessmentDraft()).toEqual({ value: 4.5, privateComment: 'Belle journée' });
    expect(facade.assessmentHasChanges()).toBe(false);
  });

  it('reloads the parent version and preserves the draft after an assessment conflict', () => {
    const currentVisit: PassportVisit = {
      ...visit,
      title: 'Version distante',
      version: 2,
      parkAssessment: {
        value: 3,
        privateComment: 'État serveur',
        revision: 1,
        createdAtUtc: '2026-09-03T09:00:00Z',
        updatedAtUtc: '2026-09-03T09:00:00Z'
      }
    };
    visitsPort.getVisit
      .mockReturnValueOnce(of(visit))
      .mockReturnValueOnce(of(currentVisit));
    visitsPort.upsertParkAssessment.mockReturnValue(throwError(() => new HttpErrorResponse({
      status: 409,
      error: {
        status: 409,
        title: 'Conflict',
        errorCode: 'visit-park-assessment.version-conflict'
      }
    })));
    visitsPort.updateVisit.mockReturnValue(of({
      ...currentVisit,
      privateNote: 'Note locale',
      version: 3
    }));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.updateParkAssessmentDraft({ value: 4.5, privateComment: 'Ma saisie' });

    facade.saveParkAssessment();

    expect(visitsPort.getVisit).toHaveBeenCalledTimes(2);
    expect(facade.visit()?.version).toBe(2);
    expect(facade.metadataDraft().title).toBe('Version distante');
    expect(facade.metadataHasChanges()).toBe(false);
    expect(facade.assessmentDraft()).toEqual({ value: 4.5, privateComment: 'Ma saisie' });
    expect(facade.assessmentErrorKey()).toBe('passport.editor.assessment.errors.conflict');

    facade.updateVisitMetadataDraft({ privateNote: 'Note locale' });
    facade.saveVisitMetadata();

    expect(visitsPort.updateVisit).toHaveBeenCalledWith('visit-1', expect.objectContaining({
      title: 'Version distante',
      privateNote: 'Note locale',
      expectedVersion: 2
    }));
  });

  it('preserves unsaved metadata when an assessment conflict refreshes the parent version', () => {
    const currentVisit: PassportVisit = {
      ...visit,
      title: 'Version distante',
      version: 2,
      parkAssessment: {
        value: 3,
        privateComment: 'État serveur',
        revision: 1,
        createdAtUtc: '2026-09-03T09:00:00Z',
        updatedAtUtc: '2026-09-03T09:00:00Z'
      }
    };
    visitsPort.getVisit
      .mockReturnValueOnce(of(visit))
      .mockReturnValueOnce(of(currentVisit));
    visitsPort.upsertParkAssessment.mockReturnValue(throwError(() => new HttpErrorResponse({
      status: 409,
      error: {
        status: 409,
        title: 'Conflict',
        errorCode: 'visit-park-assessment.version-conflict'
      }
    })));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.updateVisitMetadataDraft({ title: 'Brouillon local' });
    facade.updateParkAssessmentDraft({ value: 4.5, privateComment: 'Ma saisie' });

    facade.saveParkAssessment();

    expect(facade.visit()?.title).toBe('Version distante');
    expect(facade.metadataDraft().title).toBe('Brouillon local');
    expect(facade.metadataHasChanges()).toBe(true);
  });

  it('recognises an assessment that was committed before an ambiguous response failed', () => {
    const committedVisit: PassportVisit = {
      ...visit,
      version: 2,
      parkAssessment: {
        value: 4.5,
        privateComment: 'Ma saisie',
        revision: 1,
        createdAtUtc: '2026-09-03T09:00:00Z',
        updatedAtUtc: '2026-09-03T09:00:00Z'
      }
    };
    visitsPort.getVisit
      .mockReturnValueOnce(of(visit))
      .mockReturnValueOnce(of(committedVisit));
    visitsPort.upsertParkAssessment.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 502 })));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.updateParkAssessmentDraft({ value: 4.5, privateComment: 'Ma saisie' });

    facade.saveParkAssessment();

    expect(facade.visit()).toEqual(committedVisit);
    expect(facade.assessmentHasChanges()).toBe(false);
    expect(facade.assessmentErrorKey()).toBeNull();
  });

  it('deletes the persisted assessment and adopts the returned parent version', () => {
    const assessedVisit: PassportVisit = {
      ...visit,
      version: 2,
      parkAssessment: {
        value: 4,
        privateComment: null,
        revision: 1,
        createdAtUtc: '2026-09-03T09:00:00Z',
        updatedAtUtc: '2026-09-03T09:00:00Z'
      }
    };
    visitsPort.getVisit.mockReturnValue(of(assessedVisit));
    visitsPort.deleteParkAssessment.mockReturnValue(of({
      ...assessedVisit,
      version: 3,
      parkAssessment: null
    }));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');

    facade.deleteParkAssessment();

    expect(visitsPort.deleteParkAssessment).toHaveBeenCalledWith('visit-1', 2);
    expect(facade.visit()?.version).toBe(3);
    expect(facade.assessmentDraft()).toEqual({ value: null, privateComment: '' });
  });

  it('loads and saves the private assessment of one ride with the current occurrence version', () => {
    const assessedOccurrence: PassportRideOccurrence = {
      ...firstOccurrence,
      assessment: {
        value: 3.5,
        privateComment: 'Premier tour',
        revision: 1,
        createdAtUtc: '2026-09-03T10:00:00Z',
        updatedAtUtc: '2026-09-03T10:00:00Z'
      }
    };
    const updatedOccurrence: PassportRideOccurrence = {
      ...assessedOccurrence,
      version: 2,
      target: null,
      assessment: {
        ...assessedOccurrence.assessment!,
        value: 4.5,
        privateComment: 'Meilleur tour',
        revision: 2,
        updatedAtUtc: '2026-09-03T11:00:00Z'
      }
    };
    occurrencesPort.list.mockReturnValue(of({ items: [assessedOccurrence], nextCursor: null }));
    occurrencesPort.upsertAssessment.mockReturnValue(of(updatedOccurrence));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');

    expect(facade.rideAssessmentDrafts()['occurrence-1']).toEqual({
      value: 3.5,
      privateComment: 'Premier tour'
    });
    expect(facade.rideAssessmentHasChanges('occurrence-1')).toBe(false);
    facade.updateRideAssessmentDraft('occurrence-1', { value: 4.5, privateComment: ' Meilleur tour ' });
    facade.saveRideAssessment(assessedOccurrence);

    expect(occurrencesPort.upsertAssessment).toHaveBeenCalledWith('occurrence-1', {
      value: 4.5,
      privateComment: 'Meilleur tour',
      expectedVersion: 1
    });
    expect(facade.occurrences()[0]).toEqual(expect.objectContaining({
      version: 2,
      target: assessedOccurrence.target,
      assessment: expect.objectContaining({ value: 4.5, privateComment: 'Meilleur tour' })
    }));
    expect(facade.rideAssessmentHasChanges('occurrence-1')).toBe(false);
  });

  it('preserves a newer ride assessment draft when the save response arrives', () => {
    const response: Subject<PassportRideOccurrence> = new Subject<PassportRideOccurrence>();
    occurrencesPort.upsertAssessment.mockReturnValue(response);
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.updateRideAssessmentDraft('occurrence-1', { value: 4, privateComment: 'Première saisie' });
    facade.saveRideAssessment(firstOccurrence);

    facade.updateRideAssessmentDraft('occurrence-1', { value: 5, privateComment: 'Saisie plus récente' });
    response.next({
      ...firstOccurrence,
      version: 2,
      assessment: {
        value: 4,
        privateComment: 'Première saisie',
        revision: 1,
        createdAtUtc: '2026-09-03T10:00:00Z',
        updatedAtUtc: '2026-09-03T10:00:00Z'
      }
    });

    expect(facade.rideAssessmentDrafts()['occurrence-1']).toEqual({
      value: 5,
      privateComment: 'Saisie plus récente'
    });
    expect(facade.rideAssessmentHasChanges('occurrence-1')).toBe(true);
  });

  it('reloads the current occurrence and preserves the draft after a ride assessment conflict', () => {
    const currentOccurrence: PassportRideOccurrence = {
      ...firstOccurrence,
      version: 2,
      assessment: {
        value: 3,
        privateComment: 'État serveur',
        revision: 1,
        createdAtUtc: '2026-09-03T09:00:00Z',
        updatedAtUtc: '2026-09-03T09:00:00Z'
      }
    };
    occurrencesPort.upsertAssessment.mockReturnValue(throwError(() => new HttpErrorResponse({
      status: 409,
      error: { status: 409, title: 'Conflict', errorCode: 'ride-assessment.version-conflict' }
    })));
    occurrencesPort.get.mockReturnValue(of(currentOccurrence));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.updateRideAssessmentDraft('occurrence-1', { value: 4.5, privateComment: 'Ma saisie' });

    facade.saveRideAssessment(firstOccurrence);

    expect(occurrencesPort.get).toHaveBeenCalledWith('visit-1', 'occurrence-1');
    expect(facade.occurrences()[0].version).toBe(2);
    expect(facade.rideAssessmentDrafts()['occurrence-1']).toEqual({
      value: 4.5,
      privateComment: 'Ma saisie'
    });
    expect(facade.rideAssessmentErrorKeys()['occurrence-1'])
      .toBe('passport.editor.rideAssessment.errors.conflict');
  });

  it('recognises a ride assessment committed before an ambiguous response failed', () => {
    const committedOccurrence: PassportRideOccurrence = {
      ...firstOccurrence,
      version: 2,
      assessment: {
        value: 4.5,
        privateComment: 'Ma saisie',
        revision: 1,
        createdAtUtc: '2026-09-03T09:00:00Z',
        updatedAtUtc: '2026-09-03T09:00:00Z'
      }
    };
    occurrencesPort.upsertAssessment.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 502 }))
    );
    occurrencesPort.get.mockReturnValue(of(committedOccurrence));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.updateRideAssessmentDraft('occurrence-1', { value: 4.5, privateComment: 'Ma saisie' });

    facade.saveRideAssessment(firstOccurrence);

    expect(facade.occurrences()[0]).toEqual(expect.objectContaining({ version: 2 }));
    expect(facade.rideAssessmentHasChanges('occurrence-1')).toBe(false);
    expect(facade.rideAssessmentErrorKeys()['occurrence-1']).toBeNull();
  });

  it('deletes a persisted ride assessment and adopts the returned occurrence version', () => {
    const assessedOccurrence: PassportRideOccurrence = {
      ...firstOccurrence,
      version: 2,
      assessment: {
        value: 4,
        privateComment: null,
        revision: 1,
        createdAtUtc: '2026-09-03T09:00:00Z',
        updatedAtUtc: '2026-09-03T09:00:00Z'
      }
    };
    occurrencesPort.list.mockReturnValue(of({ items: [assessedOccurrence], nextCursor: null }));
    occurrencesPort.deleteAssessment.mockReturnValue(of({
      ...assessedOccurrence,
      version: 3,
      assessment: null
    }));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');

    facade.deleteRideAssessment(assessedOccurrence);

    expect(occurrencesPort.deleteAssessment).toHaveBeenCalledWith('occurrence-1', 2);
    expect(facade.occurrences()[0].version).toBe(3);
    expect(facade.rideAssessmentDrafts()['occurrence-1']).toEqual({ value: null, privateComment: '' });
  });

  it('does not let an older timeline reload overwrite a newer locale refresh', () => {
    const staleReload: Subject<{ items: PassportRideOccurrence[]; nextCursor: string | null }> =
      new Subject<{ items: PassportRideOccurrence[]; nextCursor: string | null }>();
    occurrencesPort.list
      .mockReturnValueOnce(of({ items: [firstOccurrence], nextCursor: null }))
      .mockReturnValueOnce(staleReload)
      .mockReturnValueOnce(of({ items: [secondOccurrence], nextCursor: null }));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');

    facade.reloadTimeline();
    facade.changeLanguage('de');
    expect(facade.timelineLoading()).toBe(false);
    staleReload.next({ items: [firstOccurrence], nextCursor: null });
    staleReload.complete();

    expect(facade.occurrences()).toEqual([secondOccurrence]);
    expect(facade.zones()).toEqual([{ id: 'zone-1', name: 'Das Dorf' }]);
    expect(facade.timelineLoading()).toBe(false);
  });

  it('does not let an older reload release the loading state owned by a newer reload', () => {
    const staleReload: Subject<{ items: PassportRideOccurrence[]; nextCursor: string | null }> =
      new Subject<{ items: PassportRideOccurrence[]; nextCursor: string | null }>();
    const currentReload: Subject<{ items: PassportRideOccurrence[]; nextCursor: string | null }> =
      new Subject<{ items: PassportRideOccurrence[]; nextCursor: string | null }>();
    occurrencesPort.list
      .mockReturnValueOnce(of({ items: [firstOccurrence], nextCursor: null }))
      .mockReturnValueOnce(staleReload)
      .mockReturnValueOnce(of({ items: [secondOccurrence], nextCursor: null }))
      .mockReturnValueOnce(currentReload);
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');

    facade.reloadTimeline();
    facade.changeLanguage('de');
    facade.reloadTimeline();
    staleReload.next({ items: [firstOccurrence], nextCursor: null });
    staleReload.complete();

    expect(facade.timelineLoading()).toBe(true);
    expect(facade.occurrences()).toEqual([secondOccurrence]);

    currentReload.next({ items: [firstOccurrence], nextCursor: null });
    currentReload.complete();

    expect(facade.timelineLoading()).toBe(false);
    expect(facade.occurrences()).toEqual([firstOccurrence]);
  });

  it('ignores a locale timeline error after a newer timeline reload', () => {
    const delayedLocaleTimeline = new Subject<{ items: PassportRideOccurrence[]; nextCursor: string | null }>();
    occurrencesPort.list
      .mockReturnValueOnce(of({ items: [firstOccurrence], nextCursor: null }))
      .mockReturnValueOnce(delayedLocaleTimeline.asObservable())
      .mockReturnValueOnce(of({ items: [secondOccurrence], nextCursor: null }));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');

    facade.changeLanguage('de');
    facade.reloadTimeline();
    delayedLocaleTimeline.error(new Error('stale locale failure'));

    expect(facade.loadErrorKey()).toBeNull();
    expect(facade.occurrences()).toEqual([secondOccurrence]);
    expect(facade.zones()).toEqual([{ id: 'zone-1', name: 'Das Dorf' }]);
    expect(facade.loading()).toBe(false);
  });

  it('keeps an unsaved draft from a loaded next page during a first-page refresh', () => {
    occurrencesPort.list
      .mockReturnValueOnce(of({ items: [firstOccurrence], nextCursor: 'page-2' }))
      .mockReturnValueOnce(of({ items: [secondOccurrence], nextCursor: null }))
      .mockReturnValueOnce(of({ items: [firstOccurrence], nextCursor: 'page-2' }));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.loadMoreTimeline();
    facade.updateOccurrenceDraft('occurrence-2', { privateNote: 'Brouillon page 2' });

    facade.reloadTimeline();

    expect(facade.occurrences().map((occurrence: PassportRideOccurrence): string => occurrence.id)).toEqual([
      'occurrence-1'
    ]);
    expect(facade.editDrafts()['occurrence-2']).toEqual(expect.objectContaining({
      privateNote: 'Brouillon page 2'
    }));
  });

  it('ignores an attraction filter response from the previously loaded visit', () => {
    const staleAttractions = new Subject<unknown>();
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    attractionsPort.getParkItemsByParkIdPage
      .mockReturnValueOnce(staleAttractions.asObservable())
      .mockReturnValueOnce(of({
        items: [{
          id: 'ride-2',
          parkId: 'park-2',
          name: 'Nouvelle attraction',
          category: 'Attraction',
          type: 'RollerCoaster',
          latitude: null,
          longitude: null,
          attractionDetails: { status: 'Operating' }
        }],
        pagination: { currentPage: 1, itemsPerPage: 24, totalItems: 1, totalPages: 1 }
      }));
    visitsPort.getVisit.mockReturnValue(of({ ...visit, id: 'visit-2', parkId: 'park-2' }));

    facade.applyAttractionFilters('ancienne', null);
    facade.load('visit-2', 'fr');
    staleAttractions.next({
      items: [{
        id: 'ride-old',
        parkId: 'park-1',
        name: 'Ancienne attraction',
        category: 'Attraction',
        type: 'RollerCoaster',
        latitude: null,
        longitude: null,
        attractionDetails: { status: 'Operating' }
      }],
      pagination: { currentPage: 1, itemsPerPage: 24, totalItems: 1, totalPages: 1 }
    });
    staleAttractions.complete();

    expect(facade.attractions().map((attraction): string => attraction.id)).toEqual(['ride-2']);
  });

  it('hands attraction loading ownership to a locale refresh', () => {
    const staleFilter = new Subject<unknown>();
    const localizedPage = {
      items: [{
        id: 'ride-1',
        parkId: 'park-1',
        name: 'Lokalisierte Attraktion',
        category: 'Attraction',
        type: 'RollerCoaster',
        latitude: null,
        longitude: null,
        attractionDetails: { status: 'Operating' }
      }],
      pagination: { currentPage: 1, itemsPerPage: 24, totalItems: 1, totalPages: 1 }
    };
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    attractionsPort.getParkItemsByParkIdPage
      .mockReturnValueOnce(staleFilter.asObservable())
      .mockReturnValueOnce(of(localizedPage));
    facade.applyAttractionFilters('ancienne', null);

    facade.changeLanguage('de');
    staleFilter.next(localizedPage);
    staleFilter.complete();

    expect(facade.attractionsLoading()).toBe(false);
    expect(facade.attractions()[0].name).toBe('Lokalisierte Attraktion');
  });

  it('clears a stale catalogue error after a successful locale refresh', () => {
    const localizedPage = {
      items: [{
        id: 'ride-1',
        parkId: 'park-1',
        name: 'Deutsche Attraktion',
        category: 'Attraction',
        type: 'RollerCoaster',
        latitude: null,
        longitude: null,
        attractionDetails: { status: 'Operating' }
      }],
      pagination: { currentPage: 1, itemsPerPage: 24, totalItems: 1, totalPages: 1 }
    };
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    attractionsPort.getParkItemsByParkIdPage
      .mockReturnValueOnce(throwError(() => new Error('temporary failure')))
      .mockReturnValueOnce(of(localizedPage));

    facade.applyAttractionFilters('ancienne', null);
    expect(facade.attractionErrorKey()).toBe('passport.editor.errors.attractions');
    facade.changeLanguage('de');

    expect(facade.attractionErrorKey()).toBeNull();
    expect(facade.attractions()[0].name).toBe('Deutsche Attraktion');
  });

  it('keeps the same add idempotency key after a lost response and clears it after success', () => {
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    occurrencesPort.addBatch
      .mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 0 })))
      .mockReturnValueOnce(of({ occurrences: [firstOccurrence], wasReplayed: true, wasOrderNormalized: true }));
    facade.load('visit-1', 'fr');
    facade.toggleAttraction(facade.attractions()[0]);
    facade.updateSelection('ride-1', { count: 3, localTime: '10:30', isApproximate: true });

    facade.addSelected();
    facade.addSelected();

    expect(occurrencesPort.addBatch).toHaveBeenCalledTimes(2);
    expect(occurrencesPort.addBatch.mock.calls[0][2]).toBe('operation-stable');
    expect(occurrencesPort.addBatch.mock.calls[1][2]).toBe('operation-stable');
    expect(occurrencesPort.addBatch.mock.calls[0][1].items[0]).toEqual(expect.objectContaining({
      count: 3,
      moment: { localTime: '10:30:00', isApproximate: true }
    }));
    expect(operationIds.create).toHaveBeenCalledTimes(1);
    expect(facade.selectedAttractions()).toEqual([]);
    expect(facade.normalizationNotice()).toBe(true);
  });

  it.each([0, 502, 504])('retries the original ambiguous add after HTTP %i even when the selection was edited', (status: number) => {
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    occurrencesPort.addBatch
      .mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status })))
      .mockReturnValueOnce(of({ occurrences: [firstOccurrence], wasReplayed: true, wasOrderNormalized: false }));
    facade.load('visit-1', 'fr');
    facade.toggleAttraction(facade.attractions()[0]);
    facade.updateSelection('ride-1', { count: 3 });

    facade.addSelected();
    facade.updateSelection('ride-1', { count: 4, privateNote: 'Nouvelle saisie' });
    facade.addSelected();

    expect(occurrencesPort.addBatch).toHaveBeenCalledTimes(2);
    expect(occurrencesPort.addBatch.mock.calls[0][1]).toEqual(occurrencesPort.addBatch.mock.calls[1][1]);
    expect(occurrencesPort.addBatch.mock.calls[0][1].items[0].count).toBe(3);
    expect(occurrencesPort.addBatch.mock.calls[0][2]).toBe(occurrencesPort.addBatch.mock.calls[1][2]);
    expect(facade.selectedAttractions()).toEqual([
      expect.objectContaining({ parkItemId: 'ride-1', count: 4, privateNote: 'Nouvelle saisie' })
    ]);
  });

  it('keeps ambiguous add recovery visible and prevents clearing its selection', () => {
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    occurrencesPort.addBatch.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 504 }))
    );
    facade.load('visit-1', 'fr');
    facade.toggleAttraction(facade.attractions()[0]);
    facade.addSelected();

    facade.clearSelection();

    expect(facade.pendingAddRecovery()).toBe(true);
    expect(facade.selectedAttractions()).toHaveLength(1);
  });

  it('ignores an add response from the previous visit while a new visit add is pending', () => {
    const firstAdd = new Subject<PassportRideOccurrenceMutationResult>();
    const secondAdd = new Subject<PassportRideOccurrenceMutationResult>();
    occurrencesPort.addBatch
      .mockReturnValueOnce(firstAdd.asObservable())
      .mockReturnValueOnce(secondAdd.asObservable());
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.toggleAttraction(facade.attractions()[0]);
    facade.addSelected();
    visitsPort.getVisit.mockReturnValue(of({ ...visit, id: 'visit-2' }));
    facade.load('visit-2', 'fr');
    facade.toggleAttraction(facade.attractions()[0]);
    facade.addSelected();

    firstAdd.next({ occurrences: [firstOccurrence], wasReplayed: false, wasOrderNormalized: false });
    firstAdd.complete();

    expect(facade.adding()).toBe(true);
    expect(occurrencesPort.addBatch).toHaveBeenCalledTimes(2);

    secondAdd.next({ occurrences: [secondOccurrence], wasReplayed: false, wasOrderNormalized: false });
    secondAdd.complete();

    expect(facade.adding()).toBe(false);
  });

  it('ignores an update response from the previously loaded visit', () => {
    const liveOccurrence: PassportRideOccurrence = {
      ...firstOccurrence,
      target: {
        name: 'Grand Huit',
        category: 'Attraction',
        lifecycleStatus: 'Operating',
        isHistoricalSnapshot: false
      }
    };
    const delayedUpdate = new Subject<PassportRideOccurrence>();
    occurrencesPort.list.mockReturnValue(of({ items: [liveOccurrence], nextCursor: null }));
    occurrencesPort.update.mockReturnValue(delayedUpdate.asObservable());
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.updateOccurrence(liveOccurrence, {
      status: 'Attempted',
      localTime: '',
      isApproximate: false,
      privateNote: '',
      confirmHistoricalConflict: false
    });
    visitsPort.getVisit.mockReturnValue(of({ ...visit, id: 'visit-2' }));
    facade.load('visit-2', 'fr');

    delayedUpdate.next({ ...liveOccurrence, status: 'Attempted' });
    delayedUpdate.complete();

    expect(facade.occurrences().find((occurrence): boolean => occurrence.id === liveOccurrence.id)?.status)
      .toBe(liveOccurrence.status);
  });

  it('ignores an update response from an earlier load after returning to the same visit', () => {
    const liveOccurrence: PassportRideOccurrence = {
      ...firstOccurrence,
      target: {
        name: 'Grand Huit',
        category: 'Attraction',
        lifecycleStatus: 'Operating',
        isHistoricalSnapshot: false
      }
    };
    const firstUpdate: Subject<PassportRideOccurrence> = new Subject<PassportRideOccurrence>();
    const currentUpdate: Subject<PassportRideOccurrence> = new Subject<PassportRideOccurrence>();
    occurrencesPort.list.mockReturnValue(of({ items: [liveOccurrence], nextCursor: null }));
    occurrencesPort.update
      .mockReturnValueOnce(firstUpdate)
      .mockReturnValueOnce(currentUpdate);
    visitsPort.getVisit
      .mockReturnValueOnce(of(visit))
      .mockReturnValueOnce(of({ ...visit, id: 'visit-2' }))
      .mockReturnValueOnce(of(visit));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    const draft = {
      status: 'Attempted' as const,
      localTime: '',
      isApproximate: false,
      privateNote: '',
      confirmHistoricalConflict: false
    };

    facade.load('visit-1', 'fr');
    facade.updateOccurrence(liveOccurrence, draft);
    facade.load('visit-2', 'fr');
    facade.load('visit-1', 'fr');
    facade.updateOccurrence(liveOccurrence, draft);
    firstUpdate.next({ ...liveOccurrence, status: 'Attempted', version: 2 });
    firstUpdate.complete();

    expect(facade.busyOccurrenceIds().has(liveOccurrence.id)).toBe(true);
    expect(facade.occurrences()[0].status).toBe('Completed');
    expect(occurrencesPort.list).toHaveBeenCalledTimes(3);
  });

  it('keeps a same-visit mutation active across a language refresh', () => {
    const liveOccurrence: PassportRideOccurrence = {
      ...firstOccurrence,
      target: {
        name: 'Grand Huit',
        category: 'Attraction',
        lifecycleStatus: 'Operating',
        isHistoricalSnapshot: false
      }
    };
    const updatedOccurrence: PassportRideOccurrence = {
      ...liveOccurrence,
      status: 'Attempted',
      version: 2
    };
    const delayedUpdate: Subject<PassportRideOccurrence> = new Subject<PassportRideOccurrence>();
    occurrencesPort.list
      .mockReturnValueOnce(of({ items: [liveOccurrence], nextCursor: null }))
      .mockReturnValueOnce(of({ items: [liveOccurrence], nextCursor: null }))
      .mockReturnValueOnce(of({ items: [updatedOccurrence], nextCursor: null }));
    occurrencesPort.update.mockReturnValue(delayedUpdate);
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.updateOccurrence(liveOccurrence, {
      status: 'Attempted',
      localTime: '',
      isApproximate: false,
      privateNote: '',
      confirmHistoricalConflict: false
    });

    facade.changeLanguage('de');
    delayedUpdate.next(updatedOccurrence);
    delayedUpdate.complete();

    expect(facade.busyOccurrenceIds().has(liveOccurrence.id)).toBe(false);
    expect(facade.occurrences()[0].status).toBe('Attempted');
    expect(facade.zones()).toEqual([{ id: 'zone-1', name: 'Das Dorf' }]);
  });

  it('preserves a selection edited while its previous snapshot is being added', () => {
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    const addResponse: Subject<PassportRideOccurrenceMutationResult> =
      new Subject<PassportRideOccurrenceMutationResult>();
    occurrencesPort.addBatch.mockReturnValue(addResponse);
    facade.load('visit-1', 'fr');
    facade.toggleAttraction(facade.attractions()[0]);

    facade.addSelected();
    facade.updateSelection('ride-1', { count: 2, privateNote: 'Second passage' });
    addResponse.next({ occurrences: [firstOccurrence], wasReplayed: false, wasOrderNormalized: false });
    addResponse.complete();

    expect(facade.selectedAttractions()).toEqual([
      expect.objectContaining({ parkItemId: 'ride-1', count: 2, privateNote: 'Second passage' })
    ]);
  });

  it('builds before and after reorder commands with optimistic versions and stable keys', () => {
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    occurrencesPort.reorder.mockReturnValue(of({
      occurrences: [secondOccurrence],
      wasReplayed: false,
      wasOrderNormalized: false
    }));
    facade.load('visit-1', 'fr');

    facade.moveOccurrence(secondOccurrence, 'up');

    expect(occurrencesPort.reorder).toHaveBeenCalledWith(
      'visit-1',
      {
        occurrenceId: 'occurrence-2',
        expectedVersion: 1,
        anchorOccurrenceId: 'occurrence-1',
        placement: 'Before'
      },
      'operation-stable'
    );
  });

  it('preserves the timeline target projection when an update response only contains occurrence data', () => {
    const projectedOccurrence: PassportRideOccurrence = {
      ...firstOccurrence,
      target: {
        name: 'Nom projeté conservé',
        category: 'Attraction',
        lifecycleStatus: 'Operating',
        isHistoricalSnapshot: false
      }
    };
    const updatedOccurrence: PassportRideOccurrence = {
      ...projectedOccurrence,
      target: null,
      status: 'Attempted',
      version: 2
    };
    occurrencesPort.list
      .mockReturnValueOnce(of({ items: [projectedOccurrence], nextCursor: null }))
      .mockReturnValueOnce(of({
        items: [{ ...updatedOccurrence, target: projectedOccurrence.target }],
        nextCursor: null
      }));
    occurrencesPort.update.mockReturnValue(of(updatedOccurrence));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');

    facade.updateOccurrence(projectedOccurrence, {
      status: 'Attempted',
      localTime: '10:30',
      isApproximate: true,
      privateNote: '',
      confirmHistoricalConflict: false
    });

    expect(facade.occurrenceRows()[0].attractionName).toBe('Nom projeté conservé');
    expect(facade.occurrences()[0]).toEqual(expect.objectContaining({
      status: 'Attempted',
      version: 2,
      target: projectedOccurrence.target
    }));
  });

  it('preserves edits made while an occurrence update is in flight', () => {
    const liveOccurrence: PassportRideOccurrence = {
      ...firstOccurrence,
      target: {
        name: 'Grand Huit',
        category: 'Attraction',
        lifecycleStatus: 'Operating',
        isHistoricalSnapshot: false
      }
    };
    const updatedOccurrence: PassportRideOccurrence = {
      ...liveOccurrence,
      status: 'Attempted',
      privateNote: 'Valeur envoyée',
      version: 2
    };
    const delayedUpdate: Subject<PassportRideOccurrence> = new Subject<PassportRideOccurrence>();
    occurrencesPort.list
      .mockReturnValueOnce(of({ items: [liveOccurrence], nextCursor: null }))
      .mockReturnValueOnce(of({ items: [updatedOccurrence], nextCursor: null }));
    occurrencesPort.update.mockReturnValue(delayedUpdate);
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    const submittedDraft = {
      status: 'Attempted' as const,
      localTime: '10:30',
      isApproximate: true,
      privateNote: 'Valeur envoyée',
      confirmHistoricalConflict: false
    };

    facade.updateOccurrence(liveOccurrence, submittedDraft);
    facade.updateOccurrenceDraft(liveOccurrence.id, { privateNote: 'Saisie plus récente' });
    delayedUpdate.next(updatedOccurrence);
    delayedUpdate.complete();

    expect(facade.editDrafts()[liveOccurrence.id].privateNote).toBe('Saisie plus récente');
    expect(facade.occurrences()[0].privateNote).toBe('Valeur envoyée');
  });

  it.each([0, 502, 504])('preserves newer edits while reconciling an ambiguous update after HTTP %i', (status: number) => {
    const liveOccurrence: PassportRideOccurrence = {
      ...firstOccurrence,
      target: {
        name: 'Grand Huit',
        category: 'Attraction',
        lifecycleStatus: 'Operating',
        isHistoricalSnapshot: false
      }
    };
    const committedOccurrence: PassportRideOccurrence = {
      ...liveOccurrence,
      status: 'Attempted',
      privateNote: 'Valeur envoyée',
      version: 2
    };
    const delayedUpdate: Subject<PassportRideOccurrence> = new Subject<PassportRideOccurrence>();
    occurrencesPort.list
      .mockReturnValueOnce(of({ items: [liveOccurrence], nextCursor: null }))
      .mockReturnValueOnce(of({ items: [committedOccurrence], nextCursor: null }));
    occurrencesPort.update.mockReturnValue(delayedUpdate);
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    const submittedDraft = {
      status: 'Attempted' as const,
      localTime: '10:30',
      isApproximate: true,
      privateNote: 'Valeur envoyée',
      confirmHistoricalConflict: false
    };

    facade.updateOccurrence(liveOccurrence, submittedDraft);
    facade.updateOccurrenceDraft(liveOccurrence.id, { privateNote: 'Saisie plus récente' });
    delayedUpdate.error(new HttpErrorResponse({ status }));

    expect(facade.occurrences()[0].privateNote).toBe('Valeur envoyée');
    expect(facade.editDrafts()[liveOccurrence.id].privateNote).toBe('Saisie plus récente');
  });

  it('preserves a newer ambiguous-update draft until its later timeline page is reloaded', () => {
    const liveSecondOccurrence: PassportRideOccurrence = {
      ...secondOccurrence,
      target: {
        name: 'Deuxième attraction',
        category: 'Attraction',
        lifecycleStatus: 'Operating',
        isHistoricalSnapshot: false
      }
    };
    const committedSecondOccurrence: PassportRideOccurrence = {
      ...liveSecondOccurrence,
      status: 'Attempted',
      privateNote: 'Valeur envoyée',
      version: 2
    };
    const delayedUpdate: Subject<PassportRideOccurrence> = new Subject<PassportRideOccurrence>();
    occurrencesPort.list
      .mockReturnValueOnce(of({ items: [firstOccurrence], nextCursor: 'page-2' }))
      .mockReturnValueOnce(of({ items: [liveSecondOccurrence], nextCursor: null }))
      .mockReturnValueOnce(of({ items: [firstOccurrence], nextCursor: 'page-2' }))
      .mockReturnValueOnce(of({ items: [committedSecondOccurrence], nextCursor: null }));
    occurrencesPort.update.mockReturnValue(delayedUpdate);
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.loadMoreTimeline();

    facade.updateOccurrence(liveSecondOccurrence, {
      status: 'Attempted',
      localTime: '10:30',
      isApproximate: true,
      privateNote: 'Valeur envoyée',
      confirmHistoricalConflict: false
    });
    facade.updateOccurrenceDraft(liveSecondOccurrence.id, { privateNote: 'Saisie page 2 plus récente' });
    delayedUpdate.error(new HttpErrorResponse({ status: 504 }));
    facade.loadMoreTimeline();

    expect(facade.occurrences()[1].privateNote).toBe('Valeur envoyée');
    expect(facade.editDrafts()[liveSecondOccurrence.id].privateNote).toBe('Saisie page 2 plus récente');
  });

  it('retries an ambiguous duplicate with its original request and idempotency key', () => {
    const liveOccurrence: PassportRideOccurrence = {
      ...firstOccurrence,
      target: {
        name: 'Grand Huit',
        category: 'Attraction',
        lifecycleStatus: 'Operating',
        isHistoricalSnapshot: false
      }
    };
    occurrencesPort.list.mockReturnValue(of({ items: [liveOccurrence], nextCursor: null }));
    occurrencesPort.addBatch
      .mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 504 })))
      .mockReturnValueOnce(of({ occurrences: [secondOccurrence], wasReplayed: true, wasOrderNormalized: false }));
    operationIds.create
      .mockReturnValueOnce('duplicate-stable')
      .mockReturnValueOnce('duplicate-unexpected');
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');

    facade.duplicateOccurrence(liveOccurrence);
    expect(facade.pendingDuplicateRecoveryIds().has(liveOccurrence.id)).toBe(true);
    facade.duplicateOccurrence({ ...liveOccurrence, status: 'Attempted', privateNote: 'Édition ultérieure' });

    expect(occurrencesPort.addBatch).toHaveBeenCalledTimes(2);
    expect(occurrencesPort.addBatch.mock.calls[1][1]).toEqual(occurrencesPort.addBatch.mock.calls[0][1]);
    expect(occurrencesPort.addBatch.mock.calls[1][2]).toBe('duplicate-stable');
    expect(operationIds.create).toHaveBeenCalledTimes(1);
    expect(facade.pendingDuplicateRecoveryIds().has(liveOccurrence.id)).toBe(false);
  });

  it('discards a full reload started before a successful occurrence edit', () => {
    const liveOccurrence: PassportRideOccurrence = {
      ...firstOccurrence,
      target: {
        name: 'Grand Huit',
        category: 'Attraction',
        lifecycleStatus: 'Operating',
        isHistoricalSnapshot: false
      }
    };
    const updatedOccurrence: PassportRideOccurrence = {
      ...liveOccurrence,
      status: 'Attempted',
      version: 2
    };
    const staleReload: Subject<{ items: PassportRideOccurrence[]; nextCursor: string | null }> =
      new Subject<{ items: PassportRideOccurrence[]; nextCursor: string | null }>();
    occurrencesPort.list
      .mockReturnValueOnce(of({ items: [liveOccurrence], nextCursor: null }))
      .mockReturnValueOnce(staleReload.asObservable())
      .mockReturnValueOnce(of({ items: [updatedOccurrence], nextCursor: null }));
    occurrencesPort.update.mockReturnValue(of(updatedOccurrence));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');

    facade.reloadTimeline();
    facade.updateOccurrence(liveOccurrence, {
      status: 'Attempted',
      localTime: '10:30',
      isApproximate: true,
      privateNote: '',
      confirmHistoricalConflict: false
    });
    staleReload.next({ items: [liveOccurrence], nextCursor: null });

    expect(facade.occurrences()).toEqual([updatedOccurrence]);
    expect(facade.timelineLoading()).toBe(false);
  });

  it('preserves unsaved edit drafts when another timeline page is appended', () => {
    occurrencesPort.list.mockReturnValue(of({ items: [firstOccurrence], nextCursor: 'page-2' }));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    facade.updateOccurrenceDraft('occurrence-1', { privateNote: 'Brouillon non enregistré' });
    occurrencesPort.list.mockReturnValue(of({ items: [secondOccurrence], nextCursor: null }));

    facade.loadMoreTimeline();

    expect(facade.editDrafts()['occurrence-1'].privateNote).toBe('Brouillon non enregistré');
    expect(facade.editDrafts()['occurrence-2']).toBeDefined();
  });

  it('discards a load-more response created from a stale cursor after a timeline reload', () => {
    const stalePage: Subject<{ items: PassportRideOccurrence[]; nextCursor: string | null }> =
      new Subject<{ items: PassportRideOccurrence[]; nextCursor: string | null }>();
    occurrencesPort.list.mockReturnValue(of({ items: [firstOccurrence], nextCursor: 'stale-cursor' }));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');
    occurrencesPort.list
      .mockReturnValueOnce(stalePage.asObservable())
      .mockReturnValueOnce(of({ items: [secondOccurrence], nextCursor: 'fresh-cursor' }));

    facade.loadMoreTimeline();
    facade.reloadTimeline();
    stalePage.next({ items: [firstOccurrence], nextCursor: null });

    expect(facade.occurrences()).toEqual([secondOccurrence]);
    expect(facade.nextTimelineCursor()).toBe('fresh-cursor');
    expect(facade.timelineLoadingMore()).toBe(false);
  });

  it('does not restore a deleted occurrence from an invalidated full-timeline reload', () => {
    const staleReload: Subject<{ items: PassportRideOccurrence[]; nextCursor: string | null }> =
      new Subject<{ items: PassportRideOccurrence[]; nextCursor: string | null }>();
    occurrencesPort.list
      .mockReturnValueOnce(of({ items: [firstOccurrence, secondOccurrence], nextCursor: null }))
      .mockReturnValueOnce(staleReload.asObservable())
      .mockReturnValueOnce(of({ items: [secondOccurrence], nextCursor: null }));
    occurrencesPort.delete.mockReturnValue(of(undefined));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');

    facade.reloadTimeline();
    facade.deleteOccurrence(firstOccurrence);
    staleReload.next({ items: [firstOccurrence, secondOccurrence], nextCursor: null });

    expect(facade.occurrences()).toEqual([secondOccurrence]);
    expect(facade.timelineLoading()).toBe(false);
    expect(facade.editDrafts()['occurrence-1']).toBeUndefined();
  });

  it('ignores a full-timeline reload from an earlier load after returning to the same visit', () => {
    const staleReload: Subject<{ items: PassportRideOccurrence[]; nextCursor: string | null }> =
      new Subject<{ items: PassportRideOccurrence[]; nextCursor: string | null }>();
    const currentReload: Subject<{ items: PassportRideOccurrence[]; nextCursor: string | null }> =
      new Subject<{ items: PassportRideOccurrence[]; nextCursor: string | null }>();
    occurrencesPort.list
      .mockReturnValueOnce(of({ items: [firstOccurrence], nextCursor: null }))
      .mockReturnValueOnce(staleReload)
      .mockReturnValueOnce(of({ items: [secondOccurrence], nextCursor: null }))
      .mockReturnValueOnce(of({ items: [firstOccurrence], nextCursor: null }))
      .mockReturnValueOnce(currentReload);
    visitsPort.getVisit
      .mockReturnValueOnce(of(visit))
      .mockReturnValueOnce(of({ ...visit, id: 'visit-2' }))
      .mockReturnValueOnce(of(visit));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);

    facade.load('visit-1', 'fr');
    facade.reloadTimeline();
    facade.load('visit-2', 'fr');
    facade.load('visit-1', 'fr');
    facade.reloadTimeline();
    staleReload.next({ items: [secondOccurrence], nextCursor: null });
    staleReload.complete();

    expect(facade.timelineLoading()).toBe(true);
    expect(facade.occurrences()).toEqual([firstOccurrence]);
    expect(occurrencesPort.list).toHaveBeenCalledTimes(5);
  });

  it('reloads the timeline after an ambiguous delete failure to reconcile server state', () => {
    occurrencesPort.list
      .mockReturnValueOnce(of({ items: [firstOccurrence, secondOccurrence], nextCursor: null }))
      .mockReturnValueOnce(of({ items: [secondOccurrence], nextCursor: null }));
    occurrencesPort.delete.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 0 })));
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');

    facade.deleteOccurrence(firstOccurrence);

    expect(occurrencesPort.delete).toHaveBeenCalledWith('visit-1', 'occurrence-1', 1);
    expect(occurrencesPort.list).toHaveBeenCalledTimes(2);
    expect(facade.occurrences()).toEqual([secondOccurrence]);
  });

  it('does not mutate an occurrence whose target only survives as a historical snapshot', () => {
    const historicalOccurrence: PassportRideOccurrence = {
      ...firstOccurrence,
      target: {
        name: 'Attraction disparue',
        category: 'Attraction',
        lifecycleStatus: 'Removed',
        isHistoricalSnapshot: true
      }
    };
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    facade.load('visit-1', 'fr');

    facade.updateOccurrence(historicalOccurrence, {
      status: 'Attempted',
      localTime: '',
      isApproximate: false,
      privateNote: '',
      confirmHistoricalConflict: false
    });
    facade.duplicateOccurrence(historicalOccurrence);

    const unresolvedOccurrence: PassportRideOccurrence = { ...firstOccurrence, target: null };
    facade.updateOccurrence(unresolvedOccurrence, {
      status: 'Attempted',
      localTime: '',
      isApproximate: false,
      privateNote: '',
      confirmHistoricalConflict: false
    });
    facade.duplicateOccurrence(unresolvedOccurrence);

    const reclassifiedOccurrence: PassportRideOccurrence = {
      ...firstOccurrence,
      target: {
        name: 'Ancienne attraction',
        category: 'Show',
        lifecycleStatus: null,
        isHistoricalSnapshot: false
      }
    };
    facade.updateOccurrence(reclassifiedOccurrence, {
      status: 'Attempted',
      localTime: '',
      isApproximate: false,
      privateNote: '',
      confirmHistoricalConflict: false
    });
    facade.duplicateOccurrence(reclassifiedOccurrence);

    expect(occurrencesPort.update).not.toHaveBeenCalled();
    expect(occurrencesPort.addBatch).not.toHaveBeenCalled();
  });

  it('keeps the private timeline usable when public park metadata is no longer available', () => {
    const facade: PassportVisitEditorStateFacade = TestBed.inject(PassportVisitEditorStateFacade);
    parksPort.getParkById.mockReturnValue(throwError(() => new Error('hidden')));
    zonesPort.getParkZonesByParkId.mockReturnValue(throwError(() => new Error('hidden')));
    attractionsPort.getParkItemsByParkIdPage.mockReturnValue(throwError(() => new Error('hidden')));

    facade.load('visit-1', 'fr');

    expect(facade.loading()).toBe(false);
    expect(facade.parkName()).toBe('park-1');
    expect(facade.occurrences()).toHaveLength(2);
    expect(facade.attractionErrorKey()).toBe('passport.editor.errors.attractions');
  });
});

function createVisit(): PassportVisit {
  return {
    id: 'visit-1',
    parkId: 'park-1',
    date: { year: 2026, month: 9, day: 3, precision: 'Day', isApproximate: false },
    timeZoneId: 'Europe/Paris',
    serviceDayConvention: 'VisitStartLocalDate',
    status: 'Draft',
    privacy: 'Private',
    title: null,
    privateNote: null,
    version: 1,
    createdAtUtc: '2026-09-03T00:00:00Z',
    updatedAtUtc: '2026-09-03T00:00:00Z',
    completedAtUtc: null
  };
}

function createOccurrence(id: string, parkItemId: string, sortPosition: number): PassportRideOccurrence {
  return {
    id,
    visitId: 'visit-1',
    parkId: 'park-1',
    parkItemId,
    sortPosition,
    moment: { localTime: '10:30:00', isApproximate: true },
    status: 'Completed',
    source: 'Manual',
    historicalConsistency: 'Verified',
    privateNote: null,
    countsAsRide: true,
    version: 1,
    createdAtUtc: '2026-09-03T00:00:00Z',
    updatedAtUtc: '2026-09-03T00:00:00Z'
  };
}
