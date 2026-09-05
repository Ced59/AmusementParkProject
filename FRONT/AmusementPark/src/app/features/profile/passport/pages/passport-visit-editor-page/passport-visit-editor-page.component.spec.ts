import { DestroyRef, WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, ParamMap, Router } from '@angular/router';
import { BehaviorSubject } from 'rxjs';

import { TranslationService } from '@app/services/translation.service';
import { PassportVisitEditorStateFacade } from '../../state/passport-visit-editor-state.facade';
import { PassportVisitEditorPageComponent } from './passport-visit-editor-page.component';

describe('PassportVisitEditorPageComponent responsive contract', () => {
  const styles: string = (
    PassportVisitEditorPageComponent as unknown as { ɵcmp: { styles: string[] } }
  ).ɵcmp.styles.join('\n');

  it('constrains every main layout branch and long label to the viewport', () => {
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('grid-template-columns: minmax(0, 0.95fr) minmax(0, 1.05fr)');
  });

  it('switches to a single column and reserves the fixed mobile navigation safe area', () => {
    expect(styles).toContain('@media (max-width: 900px)');
    expect(styles).toContain('@media (max-width: 620px)');
    expect(styles).toContain('padding-bottom: calc(5.75rem + env(safe-area-inset-bottom))');
    expect(styles).toContain('grid-template-columns: 1fr');
  });

  it('keeps 320px-class controls usable without swipe-only actions', () => {
    expect(styles).toContain('@media (max-width: 390px)');
    expect(styles).toContain('@media (max-width: 340px)');
    expect(styles).toContain('min-height: 2.75rem');
    expect(styles).toContain('outline: 3px solid');
    expect(styles).toContain('grid-template-columns: repeat(5, minmax(0, 1fr))');
    expect(styles).toContain('.passport-assessment__actions');
    expect(styles).toContain('.passport-ride-assessment__actions');
    expect(styles).toContain('.passport-ride-assessment__ratings');
    expect(styles).toContain('.passport-visit__form');
    expect(styles).toContain('.passport-visit__actions');
    expect(styles).toContain('.passport-visit__details');
    expect(styles).toContain('.passport-editor__statistics-actions');
    expect(styles).toContain('.passport-occurrence__statistics');
    expect(styles).toContain('.passport-visit-deletion__impact');
    expect(styles).toContain('.passport-visit-deletion__actions');
  });

  it('opens park, year and attraction statistics inside the localized private profile', () => {
    const navigate = vi.fn();
    const component = Object.create(PassportVisitEditorPageComponent.prototype) as {
      currentLanguage: () => string;
      router: Pick<Router, 'navigate'>;
      openParkStatistics(parkId: string): void;
      openYearStatistics(year: number): void;
      openItemStatistics(parkItemId: string): void;
    };
    component.currentLanguage = () => 'fr';
    component.router = { navigate } as Pick<Router, 'navigate'>;

    component.openParkStatistics('park-1');
    component.openYearStatistics(2025);
    component.openItemStatistics('item-1');

    expect(navigate).toHaveBeenNthCalledWith(1, ['/', 'fr', 'profile', 'passport', 'parks', 'park-1']);
    expect(navigate).toHaveBeenNthCalledWith(2, ['/', 'fr', 'profile', 'passport', 'years', 2025]);
    expect(navigate).toHaveBeenNthCalledWith(3, ['/', 'fr', 'profile', 'passport', 'items', 'item-1']);
  });

  it('forwards visit date precision and numeric fields without owning validation rules', () => {
    const updateVisitMetadataDraft = vi.fn();
    const component = Object.create(PassportVisitEditorPageComponent.prototype) as {
      facade: Pick<PassportVisitEditorStateFacade, 'updateVisitMetadataDraft'>;
      updateVisitPrecision(event: Event): void;
      updateVisitYear(event: Event): void;
    };
    component.facade = { updateVisitMetadataDraft };
    const precision: HTMLSelectElement = document.createElement('select');
    precision.innerHTML = '<option value="Year">Year</option>';
    precision.value = 'Year';
    const year: HTMLInputElement = document.createElement('input');
    year.value = '1998';

    component.updateVisitPrecision({ target: precision } as unknown as Event);
    component.updateVisitYear({ target: year } as unknown as Event);

    expect(updateVisitMetadataDraft).toHaveBeenNthCalledWith(1, { precision: 'Year' });
    expect(updateVisitMetadataDraft).toHaveBeenNthCalledWith(2, { year: 1998 });
  });

  it('forwards the selected park rating without deriving business rules in the component', () => {
    const updateParkAssessmentDraft = vi.fn();
    const component = Object.create(PassportVisitEditorPageComponent.prototype) as {
      facade: Pick<PassportVisitEditorStateFacade, 'updateParkAssessmentDraft'>;
      selectAssessmentValue(value: number): void;
    };
    component.facade = { updateParkAssessmentDraft };

    component.selectAssessmentValue(4.5);

    expect(updateParkAssessmentDraft).toHaveBeenCalledWith({ value: 4.5 });
  });

  it('keeps saved assessments readable when the visit is no longer editable', () => {
    const component = Object.create(PassportVisitEditorPageComponent.prototype) as {
      shouldDisplayAssessment(
        status: 'Draft' | 'Completed' | 'Archived' | null,
        hasAssessment: boolean
      ): boolean;
    };

    expect(component.shouldDisplayAssessment('Draft', false)).toBe(true);
    expect(component.shouldDisplayAssessment('Completed', true)).toBe(true);
    expect(component.shouldDisplayAssessment('Archived', true)).toBe(true);
    expect(component.shouldDisplayAssessment('Completed', false)).toBe(false);
  });

  it('keeps a saved occurrence note readable without reopening a locked visit', () => {
    const component = Object.create(PassportVisitEditorPageComponent.prototype) as {
      shouldDisplayReadOnlyOccurrenceNote(
        status: 'Draft' | 'Completed' | 'Archived' | null,
        privateNote: string | null
      ): boolean;
    };

    expect(component.shouldDisplayReadOnlyOccurrenceNote('Completed', 'Tour nocturne')).toBe(true);
    expect(component.shouldDisplayReadOnlyOccurrenceNote('Archived', 'Souvenir')).toBe(true);
    expect(component.shouldDisplayReadOnlyOccurrenceNote('Draft', 'Encore modifiable')).toBe(false);
    expect(component.shouldDisplayReadOnlyOccurrenceNote('Completed', '   ')).toBe(false);
  });

  it('forwards private assessment comments on input so newer text survives in-flight saves', () => {
    const updateParkAssessmentDraft = vi.fn();
    const component = Object.create(PassportVisitEditorPageComponent.prototype) as {
      facade: Pick<PassportVisitEditorStateFacade, 'updateParkAssessmentDraft'>;
      updateAssessmentComment(event: Event): void;
    };
    component.facade = { updateParkAssessmentDraft };
    const textarea: HTMLTextAreaElement = document.createElement('textarea');
    textarea.value = 'Souvenir plus précis';

    component.updateAssessmentComment({ target: textarea } as unknown as Event);

    expect(updateParkAssessmentDraft).toHaveBeenCalledWith({ privateComment: 'Souvenir plus précis' });
  });

  it('forwards a ride rating and its private comment to the facade', () => {
    const updateRideAssessmentDraft = vi.fn();
    const component = Object.create(PassportVisitEditorPageComponent.prototype) as {
      facade: Pick<PassportVisitEditorStateFacade, 'updateRideAssessmentDraft'>;
      selectRideAssessmentValue(occurrenceId: string, value: number): void;
      updateRideAssessmentComment(occurrenceId: string, event: Event): void;
    };
    component.facade = { updateRideAssessmentDraft };
    const textarea: HTMLTextAreaElement = document.createElement('textarea');
    textarea.value = 'Tour du soir';

    component.selectRideAssessmentValue('occurrence-1', 4.5);
    component.updateRideAssessmentComment('occurrence-1', { target: textarea } as unknown as Event);

    expect(updateRideAssessmentDraft).toHaveBeenNthCalledWith(1, 'occurrence-1', { value: 4.5 });
    expect(updateRideAssessmentDraft).toHaveBeenNthCalledWith(2, 'occurrence-1', {
      privateComment: 'Tour du soir'
    });
  });

  it('falls back to the translated unknown label for unsupported lifecycle statuses', () => {
    const component = Object.create(PassportVisitEditorPageComponent.prototype) as {
      lifecycleLabelKey(status: string | null): string;
    };

    expect(component.lifecycleLabelKey('Operating')).toBe('passport.editor.lifecycle.Operating');
    expect(component.lifecycleLabelKey('open')).toBe('passport.editor.lifecycle.Unknown');
    expect(component.lifecycleLabelKey('  ')).toBe('passport.editor.lifecycle.Unknown');
  });

  it('formats server-provided historical dates without a local timezone shift', () => {
    const component = Object.create(PassportVisitEditorPageComponent.prototype) as {
      currentLanguage: () => string;
      historicalDateLabel(value: string | null | undefined): string;
    };
    component.currentLanguage = () => 'fr';

    expect(component.historicalDateLabel('2010-12-31')).toBe('31 décembre 2010');
    expect(component.historicalDateLabel(null)).toBe('');
  });

  it('forwards count input immediately so an in-flight add cannot discard the latest value', () => {
    const updateSelection = vi.fn();
    const component = Object.create(PassportVisitEditorPageComponent.prototype) as {
      facade: Pick<PassportVisitEditorStateFacade, 'updateSelection'>;
      updateSelectionCount(parkItemId: string, event: Event): void;
    };
    component.facade = { updateSelection };
    const input: HTMLInputElement = document.createElement('input');
    input.value = '4';

    component.updateSelectionCount('ride-1', { target: input } as unknown as Event);

    expect(updateSelection).toHaveBeenCalledWith('ride-1', { count: 4 });
  });

  it('forwards time input immediately so an in-flight add cannot discard the latest value', () => {
    const updateSelection = vi.fn();
    const component = Object.create(PassportVisitEditorPageComponent.prototype) as {
      facade: Pick<PassportVisitEditorStateFacade, 'updateSelection'>;
      updateSelectionTime(parkItemId: string, event: Event): void;
    };
    component.facade = { updateSelection };
    const input: HTMLInputElement = document.createElement('input');
    input.value = '14:30';

    component.updateSelectionTime('ride-1', { target: input } as unknown as Event);

    expect(updateSelection).toHaveBeenCalledWith('ride-1', { localTime: '14:30' });
  });

  it('forwards occurrence time input immediately so an in-flight update preserves the latest draft', () => {
    const updateOccurrenceDraft = vi.fn();
    const component = Object.create(PassportVisitEditorPageComponent.prototype) as {
      facade: Pick<PassportVisitEditorStateFacade, 'updateOccurrenceDraft'>;
      updateEditTime(occurrenceId: string, event: Event): void;
    };
    component.facade = { updateOccurrenceDraft };
    const input: HTMLInputElement = document.createElement('input');
    input.value = '16:45';

    component.updateEditTime('occurrence-1', { target: input } as unknown as Event);

    expect(updateOccurrenceDraft).toHaveBeenCalledWith('occurrence-1', { localTime: '16:45' });
  });

  it('reloads localized state and navigation when the parent language route changes', () => {
    const languageParams: BehaviorSubject<ParamMap> = new BehaviorSubject<ParamMap>(
      convertToParamMap({ lang: 'fr' })
    );
    const parentRoute: Partial<ActivatedRoute> = {
      snapshot: { paramMap: convertToParamMap({ lang: 'fr' }) } as ActivatedRoute['snapshot'],
      paramMap: languageParams.asObservable()
    };
    const visitParams: BehaviorSubject<ParamMap> = new BehaviorSubject<ParamMap>(
      convertToParamMap({ visitId: 'visit-1' })
    );
    const route: Partial<ActivatedRoute> = {
      snapshot: { paramMap: convertToParamMap({ visitId: 'visit-1' }) } as ActivatedRoute['snapshot'],
      paramMap: visitParams.asObservable(),
      parent: parentRoute as ActivatedRoute
    };
    const facade: Pick<
      PassportVisitEditorStateFacade,
      'load' | 'changeLanguage' | 'retryLoad' | 'deletedVisitId'
    > = {
      load: vi.fn(),
      changeLanguage: vi.fn(),
      retryLoad: vi.fn(),
      deletedVisitId: (() => null) as PassportVisitEditorStateFacade['deletedVisitId']
    };
    const router: Pick<Router, 'navigate'> = { navigate: vi.fn().mockResolvedValue(true) };
    const translationService: Pick<TranslationService, 'getCurrentLang'> = {
      getCurrentLang: vi.fn().mockReturnValue('fr')
    };
    const destroyRef: DestroyRef = {
      destroyed: false,
      onDestroy: vi.fn().mockReturnValue((): void => undefined)
    };
    const component: PassportVisitEditorPageComponent = TestBed.runInInjectionContext(
      (): PassportVisitEditorPageComponent => new PassportVisitEditorPageComponent(
        facade as PassportVisitEditorStateFacade,
        route as ActivatedRoute,
        router as Router,
        translationService as TranslationService,
        destroyRef
      )
    );
    const controls = component as unknown as {
      searchControl: { setValue(value: string): void; value: string };
      zoneControl: { setValue(value: string): void; value: string };
      deleteConfirmationId: WritableSignal<string | null>;
      assessmentDeleteConfirmation: WritableSignal<boolean>;
      rideAssessmentDeleteConfirmationId: WritableSignal<string | null>;
    };
    controls.searchControl.setValue('ancienne recherche');
    controls.zoneControl.setValue('ancienne-zone');
    controls.deleteConfirmationId.set('occurrence-1');
    controls.assessmentDeleteConfirmation.set(true);
    controls.rideAssessmentDeleteConfirmationId.set('occurrence-1');

    languageParams.next(convertToParamMap({ lang: 'de' }));
    visitParams.next(convertToParamMap({ visitId: 'visit-2' }));
    (component as unknown as { goBack(): void }).goBack();
    (component as unknown as { retryLoad(): void }).retryLoad();

    expect(facade.load).toHaveBeenNthCalledWith(1, 'visit-1', 'fr');
    expect(facade.changeLanguage).toHaveBeenCalledWith('de');
    expect(facade.load).toHaveBeenNthCalledWith(2, 'visit-2', 'de');
    expect(controls.searchControl.value).toBe('');
    expect(controls.zoneControl.value).toBe('');
    expect(controls.deleteConfirmationId()).toBeNull();
    expect(controls.assessmentDeleteConfirmation()).toBe(false);
    expect(controls.rideAssessmentDeleteConfirmationId()).toBeNull();
    expect((component as unknown as { currentLanguage: WritableSignal<string> }).currentLanguage()).toBe('de');
    expect(router.navigate).toHaveBeenCalledWith(['/', 'de', 'profile']);
    expect(facade.retryLoad).toHaveBeenCalledOnce();
  });
});
