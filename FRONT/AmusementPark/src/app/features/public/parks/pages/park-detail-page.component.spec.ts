import { NO_ERRORS_SCHEMA, signal } from '@angular/core';
import { DeferBlockBehavior, DeferBlockState, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { EMPTY, of } from 'rxjs';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { LcpImagePreloadService } from '@core/performance/lcp-image-preload.service';
import { AdminContextualBlockRefreshEvents } from '@features/admin/contextual-editing/state/admin-contextual-block-refresh-events';
import { PassportVisitQuickCreateComponent } from '@features/profile/passport/components/passport-visit-quick-create/passport-visit-quick-create.component';
import { PassportVisitQuickCreateStateFacade } from '@features/profile/passport/state/passport-visit-quick-create-state.facade';
import { PublicLiveStateFacade } from '@features/public/live/state/public-live-state.facade';
import { mapParkToDetailViewModel } from '../mappers/park-detail-view.mapper';
import { ParkDetailViewModel } from '../models/park-detail-view.model';
import { ParkDetailStateFacade } from '../state/park-detail-state.facade';
import { ParkDetailPageComponent } from './park-detail-page.component';

describe('ParkDetailPageComponent deferred visit form', () => {
  const park = signal<ParkDetailViewModel | null>(null);
  const applyParkDetailSeo = vi.fn();

  beforeEach(async () => {
    park.set(mapParkToDetailViewModel({ id: 'park-1', name: 'Example Park', status: 'Operating' }, 'fr'));
    applyParkDetailSeo.mockReset();
    const stateFacade = {
      park, state: signal(null), nearbyState: signal(null), weatherState: signal(null),
      openingHoursState: signal(null), weather: signal(null), openingHours: signal(null),
      nearbyParks: signal([]), summary: signal(null), socialImageId: signal(null),
      setCurrentLanguage: vi.fn(), loadPark: vi.fn()
    };
    await TestBed.configureTestingModule({
      imports: [ParkDetailPageComponent],
      deferBlockBehavior: DeferBlockBehavior.Manual,
      providers: [
        { provide: ActivatedRoute, useValue: {
          snapshot: { paramMap: convertToParamMap({ id: 'park-1', lang: 'fr' }) },
          paramMap: of(convertToParamMap({ id: 'park-1' })), parent: null
        } },
        { provide: Router, useValue: { url: '/fr/park/park-1/example-park', navigate: vi.fn() } },
        { provide: TranslationService, useValue: { getCurrentLang: () => 'fr', languageChanged: EMPTY } },
        { provide: SeoService, useValue: { applyParkDetailSeo } },
        { provide: LcpImagePreloadService, useValue: { preloadImage: vi.fn(), clearPreload: vi.fn() } },
        { provide: AdminContextualBlockRefreshEvents, useValue: { appliedBlock$: EMPTY } }
      ]
    }).overrideComponent(ParkDetailPageComponent, { set: {
      imports: [PassportVisitQuickCreateComponent], schemas: [NO_ERRORS_SCHEMA],
      providers: [
        { provide: ParkDetailStateFacade, useValue: stateFacade },
        { provide: PublicLiveStateFacade, useValue: { state: signal(null), watchPark: vi.fn(), refresh: vi.fn() } }
      ]
    } }).overrideComponent(PassportVisitQuickCreateComponent, { set: {
      template: '', imports: [], providers: [{ provide: PassportVisitQuickCreateStateFacade, useValue: {
        createdVisit: signal(null), createdLocalDraftId: signal(null), searchParks: vi.fn(),
        clearCreationResult: vi.fn(), clearParkSearch: vi.fn()
      } }]
    } }).compileComponents();
  });

  it('does not construct the closed form while public park metadata remains available', async () => {
    const fixture = TestBed.createComponent(ParkDetailPageComponent);
    fixture.detectChanges();
    expect(fixture.debugElement.query(By.directive(PassportVisitQuickCreateComponent))).toBeNull();
    expect(await fixture.getDeferBlocks()).toHaveLength(1);
    expect(applyParkDetailSeo).toHaveBeenCalledWith(
      park(), 'fr', '/fr/park/park-1/example-park', '/fr/park/park-1/example-park', null
    );
  });

  it('opens the form for the current park and preserves an unfinished draft when reopened', async () => {
    const fixture = TestBed.createComponent(ParkDetailPageComponent);
    fixture.detectChanges();
    fixture.componentInstance.openVisitDialog();
    fixture.detectChanges();
    const blocks = await fixture.getDeferBlocks();
    await blocks[0].render(DeferBlockState.Complete);
    const dialog: PassportVisitQuickCreateComponent = fixture.debugElement.query(
      By.directive(PassportVisitQuickCreateComponent)
    ).componentInstance;
    expect(dialog.visible).toBe(true);
    expect(dialog.fixedParkId).toBe('park-1');
    expect(dialog.fixedParkName).toBe('Example Park');
    const draft = dialog as unknown as { form: { controls: { title: { setValue(value: string): void; value: string } } } };
    draft.form.controls.title.setValue('My unfinished visit');
    dialog.visibleChange.emit(false);
    fixture.detectChanges();
    expect(dialog.visible).toBe(false);
    fixture.componentInstance.openVisitDialog();
    fixture.detectChanges();
    expect(fixture.debugElement.query(By.directive(PassportVisitQuickCreateComponent)).componentInstance).toBe(dialog);
    expect(dialog.visible).toBe(true);
    expect(draft.form.controls.title.value).toBe('My unfinished visit');
  });

  it('does not offer a deferred form for an unavailable park', async () => {
    park.set(null);
    const fixture = TestBed.createComponent(ParkDetailPageComponent);
    fixture.detectChanges();
    fixture.componentInstance.openVisitDialog();
    fixture.detectChanges();
    expect(await fixture.getDeferBlocks()).toHaveLength(0);
    expect(fixture.debugElement.query(By.directive(PassportVisitQuickCreateComponent))).toBeNull();
  });
});
