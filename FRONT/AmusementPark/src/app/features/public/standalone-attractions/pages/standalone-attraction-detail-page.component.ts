import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { StandaloneAttraction } from '@app/models/standalone-attractions/standalone-attraction';
import { TranslationService } from '@app/services/translation.service';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { StandaloneAttractionsApiService } from '@data-access/standalone-attractions/standalone-attractions-api.service';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { applySsrPublicDataErrorStatus } from '@core/ssr/ssr-public-error-status';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { ScreenState } from '@shared/models/contracts';
import { SafeRichHtmlPipe } from '@shared/pipes';
import { LeafletMapComponent } from '@shared/components/leaflet-map/leaflet-map.component';
import { MapMarker } from '@app/models/map/map-marker';
import { MapMarkerPopupActionService } from '@shared/services/maps/map-marker-popup-action.service';
import { UiButtonDirective, UiChipComponent, UiKickerComponent } from '@ui/primitives';
import { resolveLocalizedText } from '@shared/utils/localization/localized-text.helpers';
import { buildPublicRoutePath, buildPublicStandaloneAttractionRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';

interface StandaloneAttractionPublicCopy {
  standalone: string;
  technicalSheet: string;
  officialWebsite: string;
  history: string;
  backToParks: string;
  location: string;
  details: string;
  description: string;
  visibleDataOnly: string;
  country: string;
  city: string;
  address: string;
  type: string;
  subtype: string;
  model: string;
  status: string;
  length: string;
  speed: string;
  duration: string;
  manufacturer: string;
  coordinates: string;
  noDescription: string;
  mapTitle: string;
  mapSubtitle: string;
  directions: string;
}

const PUBLIC_COPY: Record<string, StandaloneAttractionPublicCopy> = {
  fr: {
    standalone: 'Attraction isolée',
    technicalSheet: 'Fiche attraction',
    officialWebsite: 'Site officiel',
    history: 'Histoire',
    backToParks: 'Retour aux parcs',
    location: 'Localisation',
    details: 'Détails',
    description: 'Description',
    visibleDataOnly: 'Données publiques disponibles',
    country: 'Pays',
    city: 'Ville',
    address: 'Adresse',
    type: 'Type',
    subtype: 'Sous-type',
    model: 'Modèle',
    status: 'Statut',
    length: 'Longueur',
    speed: 'Vitesse',
    duration: 'Durée',
    manufacturer: 'Constructeur',
    coordinates: 'Coordonnées',
    noDescription: 'Aucune description publique n’est encore disponible.',
    mapTitle: 'Carte',
    mapSubtitle: 'Retrouve l’attraction et ouvre un itinéraire vers ses coordonnées.',
    directions: 'Y aller'
  },
  en: {
    standalone: 'Standalone attraction',
    technicalSheet: 'Attraction sheet',
    officialWebsite: 'Official website',
    history: 'History',
    backToParks: 'Back to parks',
    location: 'Location',
    details: 'Details',
    description: 'Description',
    visibleDataOnly: 'Available public data',
    country: 'Country',
    city: 'City',
    address: 'Address',
    type: 'Type',
    subtype: 'Subtype',
    model: 'Model',
    status: 'Status',
    length: 'Length',
    speed: 'Speed',
    duration: 'Duration',
    manufacturer: 'Manufacturer',
    coordinates: 'Coordinates',
    noDescription: 'No public description is available yet.',
    mapTitle: 'Map',
    mapSubtitle: 'Find the attraction and open directions to its coordinates.',
    directions: 'Get directions'
  },
  es: {
    standalone: 'Atracción aislada',
    technicalSheet: 'Ficha de atracción',
    officialWebsite: 'Sitio oficial',
    history: 'Historia',
    backToParks: 'Volver a parques',
    location: 'Ubicación',
    details: 'Detalles',
    description: 'Descripción',
    visibleDataOnly: 'Datos públicos disponibles',
    country: 'País',
    city: 'Ciudad',
    address: 'Dirección',
    type: 'Tipo',
    subtype: 'Subtipo',
    model: 'Modelo',
    status: 'Estado',
    length: 'Longitud',
    speed: 'Velocidad',
    duration: 'Duración',
    manufacturer: 'Fabricante',
    coordinates: 'Coordenadas',
    noDescription: 'Aún no hay descripción pública disponible.',
    mapTitle: 'Mapa',
    mapSubtitle: 'Localiza la atracción y abre una ruta hasta sus coordenadas.',
    directions: 'Cómo llegar'
  },
  de: {
    standalone: 'Eigenständige Attraktion',
    technicalSheet: 'Attraktionsprofil',
    officialWebsite: 'Offizielle Website',
    history: 'Geschichte',
    backToParks: 'Zurück zu Parks',
    location: 'Standort',
    details: 'Details',
    description: 'Beschreibung',
    visibleDataOnly: 'Verfügbare öffentliche Daten',
    country: 'Land',
    city: 'Stadt',
    address: 'Adresse',
    type: 'Typ',
    subtype: 'Untertyp',
    model: 'Modell',
    status: 'Status',
    length: 'Länge',
    speed: 'Geschwindigkeit',
    duration: 'Dauer',
    manufacturer: 'Hersteller',
    coordinates: 'Koordinaten',
    noDescription: 'Noch keine öffentliche Beschreibung verfügbar.',
    mapTitle: 'Karte',
    mapSubtitle: 'Finde die Attraktion und öffne eine Route zu ihren Koordinaten.',
    directions: 'Route öffnen'
  },
  it: {
    standalone: 'Attrazione isolata',
    technicalSheet: 'Scheda attrazione',
    officialWebsite: 'Sito ufficiale',
    history: 'Storia',
    backToParks: 'Torna ai parchi',
    location: 'Posizione',
    details: 'Dettagli',
    description: 'Descrizione',
    visibleDataOnly: 'Dati pubblici disponibili',
    country: 'Paese',
    city: 'Città',
    address: 'Indirizzo',
    type: 'Tipo',
    subtype: 'Sottotipo',
    model: 'Modello',
    status: 'Stato',
    length: 'Lunghezza',
    speed: 'Velocità',
    duration: 'Durata',
    manufacturer: 'Costruttore',
    coordinates: 'Coordinate',
    noDescription: 'Non è ancora disponibile una descrizione pubblica.',
    mapTitle: 'Mappa',
    mapSubtitle: 'Trova l’attrazione e apri un itinerario verso le sue coordinate.',
    directions: 'Indicazioni'
  },
  nl: {
    standalone: 'Losstaande attractie',
    technicalSheet: 'Attractiefiche',
    officialWebsite: 'Officiële website',
    history: 'Geschiedenis',
    backToParks: 'Terug naar parken',
    location: 'Locatie',
    details: 'Details',
    description: 'Beschrijving',
    visibleDataOnly: 'Beschikbare openbare gegevens',
    country: 'Land',
    city: 'Stad',
    address: 'Adres',
    type: 'Type',
    subtype: 'Subtype',
    model: 'Model',
    status: 'Status',
    length: 'Lengte',
    speed: 'Snelheid',
    duration: 'Duur',
    manufacturer: 'Bouwer',
    coordinates: 'Coördinaten',
    noDescription: 'Er is nog geen openbare beschrijving beschikbaar.',
    mapTitle: 'Kaart',
    mapSubtitle: 'Vind de attractie en open een route naar de coördinaten.',
    directions: 'Route openen'
  },
  pl: {
    standalone: 'Samodzielna atrakcja',
    technicalSheet: 'Karta atrakcji',
    officialWebsite: 'Oficjalna strona',
    history: 'Historia',
    backToParks: 'Powrót do parków',
    location: 'Lokalizacja',
    details: 'Szczegóły',
    description: 'Opis',
    visibleDataOnly: 'Dostępne dane publiczne',
    country: 'Kraj',
    city: 'Miasto',
    address: 'Adres',
    type: 'Typ',
    subtype: 'Podtyp',
    model: 'Model',
    status: 'Status',
    length: 'Długość',
    speed: 'Prędkość',
    duration: 'Czas trwania',
    manufacturer: 'Producent',
    coordinates: 'Współrzędne',
    noDescription: 'Brak jeszcze publicznego opisu.',
    mapTitle: 'Mapa',
    mapSubtitle: 'Znajdź atrakcję i otwórz trasę do jej współrzędnych.',
    directions: 'Wyznacz trasę'
  },
  pt: {
    standalone: 'Atração isolada',
    technicalSheet: 'Ficha da atração',
    officialWebsite: 'Site oficial',
    history: 'História',
    backToParks: 'Voltar aos parques',
    location: 'Localização',
    details: 'Detalhes',
    description: 'Descrição',
    visibleDataOnly: 'Dados públicos disponíveis',
    country: 'País',
    city: 'Cidade',
    address: 'Endereço',
    type: 'Tipo',
    subtype: 'Subtipo',
    model: 'Modelo',
    status: 'Estado',
    length: 'Comprimento',
    speed: 'Velocidade',
    duration: 'Duração',
    manufacturer: 'Fabricante',
    coordinates: 'Coordenadas',
    noDescription: 'Ainda não há descrição pública disponível.',
    mapTitle: 'Mapa',
    mapSubtitle: 'Encontra a atração e abre uma rota até às coordenadas.',
    directions: 'Obter direções'
  }
};

@Component({
  selector: 'app-standalone-attraction-detail-page',
  standalone: true,
  imports: [CommonModule, RouterLink, PageStateComponent, ImageDisplayComponent, SafeRichHtmlPipe, LeafletMapComponent, UiButtonDirective, UiChipComponent, UiKickerComponent],
  templateUrl: './standalone-attraction-detail-page.component.html',
  styleUrl: './standalone-attraction-detail-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StandaloneAttractionDetailPageComponent implements OnInit {
  protected readonly state = signal<ScreenState<StandaloneAttraction, string>>({ kind: 'loading' });
  protected readonly attraction = signal<StandaloneAttraction | null>(null);
  protected readonly photos = signal<ImageDto[]>([]);
  protected readonly currentLanguage = signal<string>('en');
  protected readonly heroImage = computed<ImageDto | null>(() => this.photos()[0] ?? null);
  protected readonly hasCoordinates = computed<boolean>(() => {
    const current: StandaloneAttraction | null = this.attraction();
    return Number.isFinite(current?.latitude) && Number.isFinite(current?.longitude);
  });
  protected readonly mapCenter = computed<[number, number]>(() => {
    const current: StandaloneAttraction | null = this.attraction();
    return [current?.latitude ?? 46.8, current?.longitude ?? 2.2];
  });
  protected readonly mapMarkers = computed<MapMarker[]>(() => {
    const current: StandaloneAttraction | null = this.attraction();
    if (!current || !this.hasCoordinates()) {
      return [];
    }

    return [this.mapMarkerPopupActionService.enrich({
      id: current.id ?? 'standalone-attraction',
      lat: current.latitude!,
      lng: current.longitude!,
      title: current.name,
      subtitle: this.getHeroLocation(current),
      iconKind: 'rollerCoaster'
    }, {
      directions: { latitude: current.latitude!, longitude: current.longitude!, label: current.name },
      directionsLabel: this.t('directions')
    })];
  });
  protected readonly description = computed<string>(() => {
    const current: StandaloneAttraction | null = this.attraction();
    return resolveLocalizedText(current?.descriptions, this.currentLanguage(), '');
  });
  protected readonly detailsRows = computed<Array<{ label: string; value: string }>>(() => {
    const current: StandaloneAttraction | null = this.attraction();

    if (!current) {
      return [];
    }

    return [
      { label: this.t('type'), value: current.type },
      { label: this.t('subtype'), value: current.subtype ?? '' },
      { label: this.t('model'), value: current.attractionDetails?.model ?? '' },
      { label: this.t('status'), value: current.attractionDetails?.status ?? '' },
      { label: this.t('manufacturer'), value: current.attractionDetails?.manufacturerId ?? '' },
      { label: this.t('length'), value: this.formatMetric(current.attractionDetails?.lengthInMeters, 'm') },
      { label: this.t('speed'), value: this.formatMetric(current.attractionDetails?.speedInKmH, 'km/h') },
      { label: this.t('duration'), value: this.formatMetric(current.attractionDetails?.durationInSeconds, 's') }
    ].filter((row: { label: string; value: string }) => row.value.length > 0);
  });

  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly apiService: StandaloneAttractionsApiService,
    private readonly imagesApiService: ImagesApiService,
    private readonly translationService: TranslationService,
    private readonly seoService: SeoService,
    private readonly ssrHttpStatusService: SsrHttpStatusService,
    private readonly mapMarkerPopupActionService: MapMarkerPopupActionService
  ) {
  }

  ngOnInit(): void {
    const initialLanguage: string = resolveLanguageFromActivatedRoute(this.route, this.translationService.getCurrentLang() || 'en');
    this.currentLanguage.set(initialLanguage);

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((language: string) => {
      this.currentLanguage.set(language);
      this.applySeo();
    });

    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap) => {
      const id: string | null = params.get('id');

      if (!id) {
        return;
      }

      this.load(id);
    });
  }

  protected t(key: keyof StandaloneAttractionPublicCopy): string {
    const language: string = PUBLIC_COPY[this.currentLanguage()] ? this.currentLanguage() : 'en';
    return PUBLIC_COPY[language][key];
  }

  protected getLocationRows(current: StandaloneAttraction): Array<{ label: string; value: string }> {
    return [
      { label: this.t('country'), value: current.countryCode ?? '' },
      { label: this.t('city'), value: current.city ?? '' },
      { label: this.t('address'), value: [current.street, current.postalCode].filter(Boolean).join(', ') },
      { label: this.t('coordinates'), value: this.formatCoordinates(current) }
    ].filter((row: { label: string; value: string }) => row.value.length > 0);
  }

  protected getHeroLocation(current: StandaloneAttraction): string {
    return [current.city, current.countryCode]
      .filter((value: string | null | undefined): value is string => !!value)
      .join(', ');
  }

  protected getWebsiteUrl(current: StandaloneAttraction): string | null {
    const value: string = current.websiteUrl?.trim() ?? '';

    if (!/^https?:\/\//i.test(value)) {
      return null;
    }

    return value;
  }

  protected historyLink(current: StandaloneAttraction): string[] {
    const commands: string[] | null = buildPublicStandaloneAttractionRouteCommands({
      language: this.currentLanguage(),
      attractionId: current.id,
      attractionName: current.name
    });

    return commands ? [...commands, 'history'] : ['/', this.currentLanguage(), 'attraction', current.id ?? '', 'attraction', 'history'];
  }

  private load(id: string): void {
    this.state.set({ kind: 'loading', data: this.attraction() ?? undefined });

    this.apiService.getById(id, anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (attraction: StandaloneAttraction) => {
        this.attraction.set(attraction);
        this.state.set({ kind: 'ready', data: attraction });
        this.applySeo();
        this.loadHeroImage(attraction);
      },
      error: (error: unknown) => {
        applySsrPublicDataErrorStatus(error, this.ssrHttpStatusService);
        this.state.set({ kind: 'error', error: 'standaloneAttractions.error' });
      }
    });
  }

  private loadHeroImage(attraction: StandaloneAttraction): void {
    if (!attraction.id) {
      return;
    }

    this.imagesApiService.getImages(
      ImageOwnerType.STANDALONE_ATTRACTION,
      attraction.id,
      ImageCategory.STANDALONE_ATTRACTION,
      1,
      1,
      anonymousHttpOptions()
    ).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (photos: ImageDto[]) => {
        this.photos.set(photos);
        this.applySeo();
      },
      error: () => {
        this.photos.set([]);
      }
    });
  }

  private applySeo(): void {
    const current: StandaloneAttraction | null = this.attraction();

    if (!current) {
      return;
    }

    this.seoService.applyStandaloneAttractionSeo(
      current,
      this.currentLanguage(),
      this.router.url,
      buildPublicRoutePath(buildPublicStandaloneAttractionRouteCommands({
        language: this.currentLanguage(),
        attractionId: current.id,
        attractionName: current.name
      })),
      this.heroImage()?.id ?? null
    );
  }

  private formatMetric(value: number | null | undefined, suffix: string): string {
    if (value === null || value === undefined || !Number.isFinite(value)) {
      return '';
    }

    return `${new Intl.NumberFormat(this.currentLanguage()).format(value)} ${suffix}`;
  }

  private formatCoordinates(current: StandaloneAttraction): string {
    if (current.latitude === null || current.latitude === undefined || current.longitude === null || current.longitude === undefined) {
      return '';
    }

    return `${current.latitude.toFixed(5)}, ${current.longitude.toFixed(5)}`;
  }
}
