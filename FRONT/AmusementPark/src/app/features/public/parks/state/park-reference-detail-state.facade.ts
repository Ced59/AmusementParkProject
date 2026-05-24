import { DestroyRef, Injectable, Signal, computed, signal } from '@angular/core';
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
import { ImagesApiService } from '@data-access/images/images-api.service';
import { ManufacturersApiService } from '@data-access/manufacturers/manufacturers-api.service';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParkFoundersApiService } from '@data-access/parks/park-founders-api.service';
import { ParkOperatorsApiService } from '@data-access/parks/park-operators-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { mapNullable } from '@shared/utils/mapping';
import {
  mapAttractionManufacturerToReferenceDetailViewModel,
  mapParkFounderToReferenceDetailViewModel,
  mapParkOperatorToReferenceDetailViewModel
} from '../mappers/park-reference-detail-view.mapper';
import { ParkReferenceDetailViewModel, ParkReferenceKind } from '../models/park-reference-detail-view.model';

interface ParkReferenceDetailSourceData {
  kind: ParkReferenceKind;
  founder: ParkFounder | null;
  operator: ParkOperator | null;
  manufacturer: AttractionManufacturer | null;
  images: ImageDto[];
  attractions: ParkItemAdminRow[];
}

@Injectable()
export class ParkReferenceDetailStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ParkReferenceDetailSourceData>();
  private readonly currentLanguageSignal = signal('en');

  public readonly state = this.screenStateStore.state;
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
        mapAttractionManufacturerToReferenceDetailViewModel(manufacturer, currentLanguage, sourceData.images, sourceData.attractions));
    }

    return mapNullable(sourceData.operator, (operator: ParkOperator) =>
      mapParkOperatorToReferenceDetailViewModel(operator, currentLanguage, sourceData.images));
  });

  constructor(
    private readonly parkFoundersApiService: ParkFoundersApiService,
    private readonly parkOperatorsApiService: ParkOperatorsApiService,
    private readonly manufacturersApiService: ManufacturersApiService,
    private readonly imagesApiService: ImagesApiService,
    private readonly parkItemsApiService: ParkItemsApiService,
    private readonly destroyRef: DestroyRef,
    private readonly ssrHttpStatusService: SsrHttpStatusService
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  loadReference(kind: ParkReferenceKind, id: string): void {
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
          attractions: []
        });
      },
      error: (error: unknown) => {
        console.error('Error loading park founder', error);

        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

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
          attractions: []
        });
      },
      error: (error: unknown) => {
        console.error('Error loading park operator', error);

        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

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
      attractions: this.getManufacturerAttractions(id)
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ({ manufacturer, images, attractions }: { manufacturer: AttractionManufacturer; images: ImageDto[]; attractions: ParkItemAdminRow[] }) => {
        this.screenStateStore.setReady({
          kind: 'manufacturer',
          founder: null,
          operator: null,
          manufacturer,
          images,
          attractions
        });
      },
      error: (error: unknown) => {
        console.error('Error loading attraction manufacturer', error);

        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

        this.screenStateStore.setError('parks.reference.errorMessage', previousData);
      }
    });
  }

  private getReferenceImages(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory): Observable<ImageDto[]> {
    return this.imagesApiService.getImages(ownerType, ownerId, category, 1, 100).pipe(
      catchError((error: unknown) => {
        console.warn('Unable to load reference images', error);
        return of([] as ImageDto[]);
      })
    );
  }

  private getManufacturerAttractions(manufacturerId: string): Observable<ParkItemAdminRow[]> {
    return this.parkItemsApiService.getParkItemsPaginated(1, 100, null, null, {
      manufacturerId,
      isVisible: true,
      category: 'Attraction'
    }).pipe(
      map((response: ApiResponse<ParkItemAdminRow>) => response.data ?? []),
      catchError((error: unknown) => {
        console.warn('Unable to load manufacturer attractions', error);
        return of([] as ParkItemAdminRow[]);
      })
    );
  }
}
