import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EventEmitter } from '@angular/core';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, convertToParamMap, ParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { BehaviorSubject, of } from 'rxjs';

import { Park } from '@app/models/parks/park';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ParkWeatherForecast } from '@app/models/parks/park-weather';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { ParkWeatherPageComponent } from './park-weather-page.component';

class FakeTranslationService {
  public readonly languageChanged: EventEmitter<string> = new EventEmitter<string>();

  getCurrentLang(): string {
    return 'fr';
  }
}

describe('ParkWeatherPageComponent', () => {
  let fixture: ComponentFixture<ParkWeatherPageComponent>;
  let parksApiService: jasmine.SpyObj<ParksApiService>;
  let seoService: jasmine.SpyObj<SeoService>;
  let routeParamMap: BehaviorSubject<ParamMap>;

  beforeEach(async () => {
    routeParamMap = new BehaviorSubject<ParamMap>(convertToParamMap({ id: 'park-1', lang: 'fr' }));
    parksApiService = jasmine.createSpyObj<ParksApiService>('ParksApiService', [
      'getParkDetailSummary',
      'getParkWeather',
      'getParkWeatherHistoricalComparisons'
    ]);
    seoService = jasmine.createSpyObj<SeoService>('SeoService', ['applyParkWeatherSeo']);

    parksApiService.getParkDetailSummary.and.returnValue(of(createSummary()));
    parksApiService.getParkWeather.and.returnValue(of(createForecast()));
    parksApiService.getParkWeatherHistoricalComparisons.and.returnValue(of({
      parkId: 'park-1',
      years: [],
      attribution: createAttribution()
    }));

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, ParkWeatherPageComponent],
      providers: [
        ...provideCommonTestDependencies(),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ id: 'park-1', lang: 'fr' }) },
            parent: null,
            paramMap: routeParamMap.asObservable()
          }
        },
        { provide: ParksApiService, useValue: parksApiService },
        { provide: SeoService, useValue: seoService },
        { provide: SsrHttpStatusService, useValue: jasmine.createSpyObj<SsrHttpStatusService>('SsrHttpStatusService', ['setNotFound']) },
        { provide: TranslationService, useClass: FakeTranslationService }
      ]
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      parkWeather: {
        page: {
          kicker: 'Météo',
          title: 'Météo à 7 jours de {{name}}',
          lead: 'Vérifie la météo de {{name}} pour ta visite.',
          backToPark: 'Retour à {{name}}',
          forecastLabel: 'Prévisions météo à 7 jours',
          loadingTitle: 'Chargement de la météo',
          loadingMessage: 'Les prévisions sont en cours de chargement.',
          emptyTitle: 'Aucune prévision disponible',
          emptyMessage: 'Aucune donnée météo n’est encore disponible pour ce parc.',
          errorTitle: 'Météo indisponible'
        },
        today: { title: 'Météo du jour' },
        conditions: { cloudy: 'Nuageux', unknown: 'Météo indisponible' },
        chart: {
          maximum: 'Max',
          minimum: 'Min',
          condition: 'Météo',
          temperature: 'Temp.',
          precipitationSum: 'Cumul'
        },
        fields: {
          precipitationProbability: 'Risque de pluie',
          wind: 'Vent'
        },
        history: {
          title: 'Comparer avec les années précédentes',
          subtitle: 'Compare avec les années précédentes pour mieux anticiper ta visite.',
          loading: 'Chargement des comparaisons météo.',
          empty: 'Aucune observation historique n’est disponible pour ces dates.',
          errorMessage: 'Les comparaisons météo n’ont pas pu être chargées.',
          yearSingular: 'Il y a {{count}} an',
          yearPlural: 'Il y a {{count}} ans',
          rain: 'Pluie {{value}}',
          wind: 'Vent {{value}}'
        },
        attribution: {
          full: 'Données météo fournies par {{provider}} sous licence :'
        },
        errorMessage: 'Les données météo n’ont pas pu être chargées.'
      },
      shareSocial: {
        weather: {
          title: 'Partager cette météo',
          description: 'Envoie les prévisions de {{title}} à quelqu’un qui prépare sa visite.',
          text: 'Vérifie la météo de {{title}} sur AmusementPark.'
        },
        defaultTitle: 'Partager cette page',
        defaultDescription: 'Tu connais quelqu’un qui pourrait utiliser cette page ?',
        defaultText: 'Regarde {{title}} sur AmusementPark.',
        actions: {
          share: 'Partager',
          copy: 'Copier',
          copied: 'Copié',
          linkedin: 'LinkedIn',
          facebook: 'Facebook',
          x: 'X',
          reddit: 'Reddit',
          more: 'Plus',
          less: 'Moins',
          email: 'Email',
          whatsapp: 'WhatsApp',
          telegram: 'Telegram',
          qrCode: 'QR code'
        },
        qr: {
          title: 'Partager via QR code',
          loading: 'Génération du QR code',
          alt: 'QR code pour {{title}}'
        }
      }
    });
    translateService.use('fr');

    fixture = TestBed.createComponent(ParkWeatherPageComponent);
    fixture.detectChanges();
  });

  it('renders the weather page lead with the park name', () => {
    const textContent: string = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(textContent).toContain('Vérifie la météo de Bellewaerde pour ta visite.');
    expect(textContent).not.toContain('{{name}}');
  });

  it('renders the weather sharing panel for the park', () => {
    const sharePanel: PublicSharePanelComponent = fixture.debugElement
      .query(By.directive(PublicSharePanelComponent))
      .componentInstance as PublicSharePanelComponent;

    expect(sharePanel.targetType).toBe('Park');
    expect(sharePanel.targetId).toBe('park-1');
    expect(sharePanel.targetTitle).toBe('Bellewaerde');
    expect(sharePanel.titleKey).toBe('shareSocial.weather.title');
    expect(sharePanel.descriptionKey).toBe('shareSocial.weather.description');
    expect(sharePanel.textKey).toBe('shareSocial.weather.text');
  });

  it('requests historical comparisons for the displayed forecast dates', () => {
    const details: HTMLDetailsElement | null = (fixture.nativeElement as HTMLElement).querySelector('details.park-weather-history');

    expect(details).not.toBeNull();
    details!.open = true;
    details!.dispatchEvent(new Event('toggle'));
    fixture.detectChanges();

    const args: Parameters<ParksApiService['getParkWeatherHistoricalComparisons']> = parksApiService.getParkWeatherHistoricalComparisons.calls.mostRecent().args;
    expect(args[0]).toBe('park-1');
    expect(args[1]).toBe(2);
    expect(args[2]).toBe(10);
    expect(args[3]).toEqual(['2026-06-20', '2026-06-21']);
  });
});

function createSummary(): ParkDetailSummary {
  return {
    park: createPark(),
    mainImage: null,
    references: {},
    stats: {
      totalItems: 0,
      zoneCount: 0,
      attractionCount: 0,
      restaurantCount: 0,
      showCount: 0,
      shopCount: 0,
      hotelCount: 0,
      countsByCategory: {}
    }
  };
}

function createPark(): Park {
  return {
    id: 'park-1',
    name: 'Bellewaerde',
    countryCode: 'BE',
    latitude: 50.845,
    longitude: 2.945,
    isVisible: true
  };
}

function createForecast(): ParkWeatherForecast {
  return {
    parkId: 'park-1',
    days: [
      {
        localDate: '2026-06-20',
        dataKind: 'Forecast',
        weatherCode: 3,
        temperatureMinCelsius: 18,
        temperatureMaxCelsius: 28,
        precipitationProbabilityMaxPercent: 22,
        precipitationSumMillimeters: 0,
        windSpeedMaxKilometersPerHour: 12.6,
        fetchedAtUtc: '2026-06-20T08:00:00Z'
      },
      {
        localDate: '2026-06-21',
        dataKind: 'Forecast',
        weatherCode: 2,
        temperatureMinCelsius: 16,
        temperatureMaxCelsius: 24,
        precipitationProbabilityMaxPercent: 12,
        precipitationSumMillimeters: 0,
        windSpeedMaxKilometersPerHour: 10.1,
        fetchedAtUtc: '2026-06-20T08:00:00Z'
      }
    ],
    attribution: createAttribution()
  };
}

function createAttribution(): ParkWeatherForecast['attribution'] {
  return {
    providerName: 'Open-Meteo',
    providerUrl: 'https://open-meteo.com',
    licenseName: 'CC BY 4.0',
    licenseUrl: 'https://creativecommons.org/licenses/by/4.0/'
  };
}
