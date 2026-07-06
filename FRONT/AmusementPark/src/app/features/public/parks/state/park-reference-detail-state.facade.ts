import {
  DestroyRef,
  Injectable,
  Signal,
  computed,
  signal,
  Inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, forkJoin, map, of, Observable } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { ParkFounder } from '@app/models/parks/park-founder';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { ParkOperator } from '@app/models/parks/park-operator';
import { ApiResponse } from '@app/models/shared/api_reponse';
import { PaginationContract } from '@shared/models/contracts';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { applySsrPublicDataErrorStatus } from '@core/ssr/ssr-public-error-status';
import { mapNullable } from '@shared/utils/mapping';
import {
  mapAttractionManufacturerToReferenceDetailViewModel,
  mapParkFounderToReferenceDetailViewModel,
  mapParkOperatorToReferenceDetailViewModel
} from '../mappers/park-reference-detail-view.mapper';
import { ParkReferenceDetailViewModel, ParkReferenceKind } from '../models/park-reference-detail-view.model';

import {
  PARK_REFERENCE_DETAIL_STATE_IMAGES_API_SERVICE_PORT,
  ParkReferenceDetailStateImagesApiServicePort,
  PARK_REFERENCE_DETAIL_STATE_MANUFACTURERS_API_SERVICE_PORT,
  ParkReferenceDetailStateManufacturersApiServicePort,
  PARK_REFERENCE_DETAIL_STATE_PARK_ITEMS_API_SERVICE_PORT,
  ParkReferenceDetailStateParkItemsApiServicePort,
  PARK_REFERENCE_DETAIL_STATE_PARK_FOUNDERS_API_SERVICE_PORT,
  ParkReferenceDetailStateParkFoundersApiServicePort,
  PARK_REFERENCE_DETAIL_STATE_PARK_OPERATORS_API_SERVICE_PORT,
  ParkReferenceDetailStateParkOperatorsApiServicePort
} from './park-reference-detail-state-data.ports';
interface ParkReferenceDetailSourceData {
  kind: ParkReferenceKind;
  founder: ParkFounder | null;
  operator: ParkOperator | null;
  manufacturer: AttractionManufacturer | null;
  images: ImageDto[];
  attractions: ParkItemAdminRow[];
  attractionsPagination: PaginationContract | null;
}

interface ManufacturerAttractionsPage {
  attractions: ParkItemAdminRow[];
  pagination: PaginationContract;
}

@Injectable()
export class ParkReferenceDetailStateFacade {
  private static readonly ManufacturerAttractionsPageSize: number = 12;

  private readonly screenStateStore = new SignalScreenStateStore<ParkReferenceDetailSourceData>();
  private readonly currentLanguageSignal = signal('en');
  private readonly attractionsLoadingSignal = signal<boolean>(false);
  private attractionsLoadSequence: number = 0;

  public readonly state = this.screenStateStore.state;
  public readonly attractionsLoading: Signal<boolean> = this.attractionsLoadingSignal.asReadonly();
  public readonly reference: Signal<ParkReferenceDetailViewModel | null> = computed(() => {
    const sourceData: ParkReferenceDetailSourceData | undefined = this.screenStateStore.data();
    const currentLanguage: string = this.currentLanguageSignal();

    if (!sourceData) {
      return null;
    }

    if (sourceData.kind === 'founder') {
      return mapNullable(sourceData.founder, (founder: ParkFounder) =>
        mapParkFounderToReferenceDetailViewModel(founder, currentLanguage, sourceData.images));
    }

    if (sourceData.kind === 'manufacturer') {
      return mapNullable(sourceData.manufacturer, (manufacturer: AttractionManufacturer) =>
        mapAttractionManufacturerToReferenceDetailViewModel(manufacturer, currentLanguage, sourceData.images, sourceData.attractions, sourceData.attractionsPagination));
    }

    return mapNullable(sourceData.operator, (operator: ParkOperator) =>
      mapParkOperatorToReferenceDetailViewModel(operator, currentLanguage, sourceData.images));
  });

  constructor(
    @Inject(PARK_REFERENCE_DETAIL_STATE_PARK_FOUNDERS_API_SERVICE_PORT) private readonly parkFoundersApiService: ParkReferenceDetailStateParkFoundersApiServicePort,
    @Inject(PARK_REFERENCE_DETAIL_STATE_PARK_OPERATORS_API_SERVICE_PORT) private readonly parkOperatorsApiService: ParkReferenceDetailStateParkOperatorsApiServicePort,
    @Inject(PARK_REFERENCE_DETAIL_STATE_MANUFACTURERS_API_SERVICE_PORT) private readonly manufacturersApiService: ParkReferenceDetailStateManufacturersApiServicePort,
    @Inject(PARK_REFERENCE_DETAIL_STATE_IMAGES_API_SERVICE_PORT) private readonly imagesApiService: ParkReferenceDetailStateImagesApiServicePort,
    @Inject(PARK_REFERENCE_DETAIL_STATE_PARK_ITEMS_API_SERVICE_PORT) private readonly parkItemsApiService: ParkReferenceDetailStateParkItemsApiServicePort,
    private readonly destroyRef: DestroyRef,
    private readonly ssrHttpStatusService: SsrHttpStatusService
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  loadReference(kind: ParkReferenceKind, id: string): void {
    this.attractionsLoadingSignal.set(false);

    if (kind === 'founder') {
      this.loadFounder(id);
      return;
    }

    if (kind === 'manufacturer') {
      this.loadManufacturer(id);
      return;
    }

    this.loadOperator(id);
  }

  private loadFounder(id: string): void {
    const previousData: ParkReferenceDetailSourceData | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    forkJoin({
      founder: this.parkFoundersApiService.getParkFounderById(id),
      images: this.getReferenceImages(ImageOwnerType.PARK_FOUNDER, id, ImageCategory.FOUNDER)
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ({ founder, images }: { founder: ParkFounder; images: ImageDto[] }) => {
        this.screenStateStore.setReady({
          kind: 'founder',
          founder,
          operator: null,
          manufacturer: null,
          images,
          attractions: [],
          attractionsPagination: null
        });
      },
      error: (error: unknown) => {
        console.error('Error loading park founder', error);
        applySsrPublicDataErrorStatus(error, this.ssrHttpStatusService);

        this.screenStateStore.setError('parks.reference.errorMessage', previousData);
      }
    });
  }

  private loadOperator(id: string): void {
    const previousData: ParkReferenceDetailSourceData | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    forkJoin({
      operator: this.parkOperatorsApiService.getParkOperatorById(id),
      images: this.getReferenceImages(ImageOwnerType.PARK_OPERATOR, id, ImageCategory.OPERATOR)
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ({ operator, images }: { operator: ParkOperator; images: ImageDto[] }) => {
        this.screenStateStore.setReady({
          kind: 'operator',
          founder: null,
          operator,
          manufacturer: null,
          images,
          attractions: [],
          attractionsPagination: null
        });
      },
      error: (error: unknown) => {
        console.error('Error loading park operator', error);
        applySsrPublicDataErrorStatus(error, this.ssrHttpStatusService);

        this.screenStateStore.setError('parks.reference.errorMessage', previousData);
      }
    });
  }

  private loadManufacturer(id: string): void {
    const previousData: ParkReferenceDetailSourceData | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    forkJoin({
      manufacturer: this.manufacturersApiService.getAttractionManufacturerById(id),
      images: this.getReferenceImages(ImageOwnerType.ATTRACTION_MANUFACTURER, id, ImageCategory.MANUFACTURER),
      attractionsPage: this.getManufacturerAttractions(id, 1, ParkReferenceDetailStateFacade.ManufacturerAttractionsPageSize)
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ({ manufacturer, images, attractionsPage }: { manufacturer: AttractionManufacturer; images: ImageDto[]; attractionsPage: ManufacturerAttractionsPage }) => {
        this.attractionsLoadingSignal.set(false);
        this.screenStateStore.setReady({
          kind: 'manufacturer',
          founder: null,
          operator: null,
          manufacturer,
          images,
          attractions: attractionsPage.attractions,
          attractionsPagination: attractionsPage.pagination
        });
      },
      error: (error: unknown) => {
        console.error('Error loading attraction manufacturer', error);
        applySsrPublicDataErrorStatus(error, this.ssrHttpStatusService);

        this.attractionsLoadingSignal.set(false);
        this.screenStateStore.setError('parks.reference.errorMessage', previousData);
      }
    });
  }

  loadManufacturerAttractionsPage(page: number, size: number): void {
    const currentData: ParkReferenceDetailSourceData | undefined = this.screenStateStore.data();
    const manufacturerId: string | null = currentData?.kind === 'manufacturer'
      ? currentData.manufacturer?.id ?? null
      : null;

    if (!currentData || !manufacturerId) {
      return;
    }

    const sequence: number = this.attractionsLoadSequence + 1;
    this.attractionsLoadSequence = sequence;
    this.attractionsLoadingSignal.set(true);

    this.getManufacturerAttractions(manufacturerId, Math.max(page, 1), Math.max(size, 1))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (attractionsPage: ManufacturerAttractionsPage): void => {
          if (sequence !== this.attractionsLoadSequence) {
            return;
          }

          this.attractionsLoadingSignal.set(false);
          this.screenStateStore.setReady({
            ...currentData,
            attractions: attractionsPage.attractions,
            attractionsPagination: attractionsPage.pagination
          });
        },
        error: (error: unknown): void => {
          if (sequence !== this.attractionsLoadSequence) {
            return;
          }

          console.warn('Unable to load manufacturer attractions page', error);
          this.attractionsLoadingSignal.set(false);
        }
      });
  }

  private getReferenceImages(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory): Observable<ImageDto[]> {
    return this.imagesApiService.getImages(ownerType, ownerId, category, 1, 100, anonymousHttpOptions()).pipe(
      catchError((error: unknown) => {
        console.warn('Unable to load reference images', error);
        return of([] as ImageDto[]);
      })
    );
  }

  private getManufacturerAttractions(manufacturerId: string, page: number, size: number): Observable<ManufacturerAttractionsPage> {
    return this.parkItemsApiService.getParkItemsPaginated(page, size, null, null, {
      manufacturerId,
      isVisible: true,
      category: 'Attraction'
    }, null, anonymousHttpOptions()).pipe(
      map((response: ApiResponse<ParkItemAdminRow>) => ({
        attractions: response.data ?? [],
        pagination: response.pagination ?? buildEmptyPagination(page, size)
      })),
      catchError((error: unknown) => {
        console.warn('Unable to load manufacturer attractions', error);
        return of({
          attractions: [],
          pagination: buildEmptyPagination(page, size)
        });
      })
    );
  }
}

function buildEmptyPagination(page: number, size: number): PaginationContract {
  return {
    totalItems: 0,
    totalPages: 0,
    currentPage: page,
    itemsPerPage: size
  };
}
