import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { Park } from '@app/models/parks/park';
import { ParkDistanceResponse } from '@app/models/parks/park-distance';
import { ParkExplorer } from '@app/models/parks/park-explorer';
import { ParkFounder } from '@app/models/parks/park-founder';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkOperator } from '@app/models/parks/park-operator';
import { ParkZone } from '@app/models/parks/park-zone';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParkFoundersApiService } from '@data-access/parks/park-founders-api.service';
import { ParkOperatorsApiService } from '@data-access/parks/park-operators-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';

export interface ParkDetailParksPort {
  getParkById(id: string, options?: AnonymousHttpOptions): Observable<Park>;
  getParkExplorer(parkId: string, options?: AnonymousHttpOptions): Observable<ParkExplorer>;
  getNearestParks(sourceParkId: string, limit?: number, maxDistanceKilometers?: number | null, options?: AnonymousHttpOptions): Observable<ParkDistanceResponse>;
}

export interface ParkDetailImagesPort {
  getImages(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory, page?: number, size?: number, options?: AnonymousHttpOptions): Observable<ImageDto[]>;
  getAdminImageTags(options?: AnonymousHttpOptions): Observable<ImageTagDto[]>;
}

export interface ParkDetailItemsPort {
  getParkItemsByParkId(parkId: string, options?: AnonymousHttpOptions): Observable<ParkItem[]>;
}

export interface ParkDetailZonesPort {
  getParkZonesByParkId(parkId: string, options?: AnonymousHttpOptions): Observable<ParkZone[]>;
}

export interface ParkDetailFoundersPort {
  getParkFounderById(id: string): Observable<ParkFounder>;
}

export interface ParkDetailOperatorsPort {
  getParkOperatorById(id: string): Observable<ParkOperator>;
}

export const PARK_DETAIL_PARKS_PORT = new InjectionToken<ParkDetailParksPort>('PARK_DETAIL_PARKS_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});

export const PARK_DETAIL_IMAGES_PORT = new InjectionToken<ParkDetailImagesPort>('PARK_DETAIL_IMAGES_PORT', {
  providedIn: 'root',
  factory: () => inject(ImagesApiService)
});

export const PARK_DETAIL_ITEMS_PORT = new InjectionToken<ParkDetailItemsPort>('PARK_DETAIL_ITEMS_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkItemsApiService)
});

export const PARK_DETAIL_ZONES_PORT = new InjectionToken<ParkDetailZonesPort>('PARK_DETAIL_ZONES_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkZonesApiService)
});

export const PARK_DETAIL_FOUNDERS_PORT = new InjectionToken<ParkDetailFoundersPort>('PARK_DETAIL_FOUNDERS_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkFoundersApiService)
});

export const PARK_DETAIL_OPERATORS_PORT = new InjectionToken<ParkDetailOperatorsPort>('PARK_DETAIL_OPERATORS_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkOperatorsApiService)
});
