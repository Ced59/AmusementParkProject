import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { of, Subject } from 'rxjs';

import { StandaloneAttraction } from '@app/models/standalone-attractions/standalone-attraction';
import { MeasurementPreferenceService } from '@app/services/measurements/measurement-preference.service';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { StandaloneAttractionsApiService } from '@data-access/standalone-attractions/standalone-attractions-api.service';
import { MeasurementSystem } from '@shared/models/measurements/measurement-system.model';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { MapMarkerPopupActionService } from '@shared/services/maps/map-marker-popup-action.service';
import { StandaloneAttractionDetailPageComponent } from './standalone-attraction-detail-page.component';
import { StandaloneAttractionDetailReferenceFacade } from './standalone-attraction-detail-reference.facade';
import { StandaloneAttractionVisitorInformationFacade } from './standalone-attraction-visitor-information.facade';

describe('StandaloneAttractionDetailPageComponent access conditions', () => {
  let fixture: ComponentFixture<StandaloneAttractionDetailPageComponent>;
  let response: Subject<StandaloneAttraction>;
  let languageChanged: Subject<string>;
  let preferredSystem: ReturnType<typeof signal<MeasurementSystem>>;

  beforeEach(async () => {
    response = new Subject<StandaloneAttraction>();
    languageChanged = new Subject<string>();
    preferredSystem = signal<MeasurementSystem>('Metric');
    await TestBed.configureTestingModule({
      imports: [StandaloneAttractionDetailPageComponent, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: {
          snapshot: { paramMap: convertToParamMap({ lang: 'fr', id: 'standalone-1' }) },
          paramMap: of(convertToParamMap({ id: 'standalone-1' })),
          parent: null
        } },
        { provide: StandaloneAttractionsApiService, useValue: { getById: () => response } },
        { provide: ImagesApiService, useValue: { getImages: () => of([]) } },
        { provide: TranslationService, useValue: { getCurrentLang: () => 'fr', languageChanged } },
        { provide: MeasurementPreferenceService, useValue: { preferredSystem } },
        { provide: CountryDisplayService, useValue: { resolveLocalizedCountryName: () => 'France' } },
        { provide: SeoService, useValue: { applyStandaloneAttractionSeo: () => undefined } },
        { provide: MapMarkerPopupActionService, useValue: {} }
      ]
    }).overrideComponent(StandaloneAttractionDetailPageComponent, {
      set: {
        providers: [
          { provide: StandaloneAttractionDetailReferenceFacade, useValue: {
            manufacturerName: signal(null), loadManufacturer: () => undefined
          } },
          { provide: StandaloneAttractionVisitorInformationFacade, useValue: {
            openingHours: signal(null), openingHoursState: signal({ kind: 'empty' }),
            pricing: signal(null), pricingState: signal({ kind: 'empty' }),
            weather: signal(null), weatherState: signal({ kind: 'empty' }),
            load: () => undefined
          } }
        ]
      }
    }).compileComponents();

    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('fr', { parkItems: { detail: { accessConditionsTitle: 'Conditions d’accès' } } });
    translate.setTranslation('en', { parkItems: { detail: { accessConditionsTitle: 'Access requirements' } } });
    translate.use('fr');
    fixture = TestBed.createComponent(StandaloneAttractionDetailPageComponent);
    fixture.detectChanges();
  });

  it('renders heights, ages, accompaniment, restrictions and custom conditions from the public response', () => {
    response.next(createAttraction());
    fixture.detectChanges();

    const panel: HTMLElement = fixture.nativeElement.querySelector('.access-panel');
    expect(panel).not.toBeNull();
    expect(panel.textContent).toContain('Conditions d’accès');
    expect(panel.textContent).toContain('130 cm');
    expect(panel.textContent).toContain('90 cm');
    expect(panel.textContent).toContain('3 ans');
    expect(panel.textContent).toContain('16 ans');
    expect(panel.textContent).toContain('Interdit pendant la grossesse');
    expect(panel.textContent).toContain('Poids total maximal');
    expect(panel.textContent).toContain('130 kg par luge.');
    expect(panel.textContent).not.toContain('internal-source-reference');
  });

  it('updates localized conditions and height units without reloading the attraction', () => {
    response.next(createAttraction());
    fixture.detectChanges();
    TestBed.inject(TranslateService).use('en');
    languageChanged.next('en');
    preferredSystem.set('Imperial');
    fixture.detectChanges();

    const panel: HTMLElement = fixture.nativeElement.querySelector('.access-panel');
    expect(panel.textContent).toContain('Access requirements');
    expect(panel.textContent).toContain('4 ft 3 in');
    expect(panel.textContent).toContain('2 ft 11 in');
    expect(panel.textContent).toContain('3 years');
    expect(panel.textContent).toContain('16 years');
    expect(panel.textContent).toContain('Not permitted during pregnancy');
    expect(panel.textContent).toContain('Maximum combined weight');
    expect(panel.textContent).not.toContain('Poids total maximal');
  });

  it('reacts to metric and imperial preferences in technical characteristics without reloading', () => {
    response.next(createAttraction());
    fixture.detectChanges();
    const sidebar: HTMLElement = fixture.nativeElement.querySelector('.standalone-sidebar');
    expect(sidebar.textContent).toContain('30,5 m');
    expect(sidebar.textContent).toContain('96,6 km/h');

    preferredSystem.set('Imperial');
    fixture.detectChanges();
    expect(sidebar.textContent).toContain('100 ft');
    expect(sidebar.textContent).toContain('60 mph');
    expect(sidebar.textContent).toContain('90 s');
    expect(sidebar.textContent).not.toContain('km/h');

    preferredSystem.set('Metric');
    fixture.detectChanges();
    expect(sidebar.textContent).toContain('96,6 km/h');
    expect(sidebar.textContent).not.toContain('mph');
  });

  it.each([null, {}, { accessConditions: null }, { accessConditions: [] }])(
    'omits the section when attraction details have no access conditions: %j', (attractionDetails) => {
      response.next({ ...createAttraction(), attractionDetails });
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('app-attraction-access-conditions')).toBeNull();
    }
  );
});

function createAttraction(): StandaloneAttraction {
  return {
    id: 'standalone-1', name: 'Attraction isolée', type: 'RollerCoaster',
    isVisible: true, adminReviewStatus: 'Validated',
    attractionDetails: {
      lengthInMeters: 30.48,
      speedInKmH: 96.56064,
      durationInSeconds: 90,
      accessConditions: [
        { type: 'MinHeight', value: 130, unit: 'Centimeter', displayOrder: 1 },
        { type: 'MinHeightAccompanied', value: 90, unit: 'Centimeter', requiresAccompaniment: true, displayOrder: 2 },
        { type: 'MinAgeAccompanied', value: 3, unit: 'Year', requiresAccompaniment: true, minimumCompanionAge: 16, displayOrder: 3 },
        { type: 'Custom', customTypeLabel: [
          { languageCode: 'fr', value: 'Poids total maximal' },
          { languageCode: 'en', value: 'Maximum combined weight' }
        ], description: [
          { languageCode: 'fr', value: '130 kg par luge.' },
          { languageCode: 'en', value: '130 kg per sled.' }
        ], sourceReference: 'internal-source-reference', displayOrder: 4 },
        { type: 'PregnancyRestriction', label: [
          { languageCode: 'fr', value: 'Interdit pendant la grossesse' },
          { languageCode: 'en', value: 'Not permitted during pregnancy' }
        ], displayOrder: 5 }
      ]
    }
  };
}
