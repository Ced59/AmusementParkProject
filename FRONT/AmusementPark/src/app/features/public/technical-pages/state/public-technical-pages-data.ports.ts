import { inject, InjectionToken } from '@angular/core';

import { ImagesApiService } from '@data-access/images/images-api.service';
import { TechnicalPagesApiService } from '@data-access/technical-pages/technical-pages-api.service';

export interface PublicTechnicalPagesApiServicePort
  extends Pick<TechnicalPagesApiService, 'getAllPublicPages' | 'getBySlug'> {
}

export const PUBLIC_TECHNICAL_PAGES_API_SERVICE_PORT =
  new InjectionToken<PublicTechnicalPagesApiServicePort>('PUBLIC_TECHNICAL_PAGES_API_SERVICE_PORT', {
    providedIn: 'root',
    factory: () => inject(TechnicalPagesApiService)
  });

export interface PublicTechnicalPagesImagesApiServicePort
  extends Pick<ImagesApiService, 'resolveImageUrl'> {
}

export const PUBLIC_TECHNICAL_PAGES_IMAGES_API_SERVICE_PORT =
  new InjectionToken<PublicTechnicalPagesImagesApiServicePort>('PUBLIC_TECHNICAL_PAGES_IMAGES_API_SERVICE_PORT', {
    providedIn: 'root',
    factory: () => inject(ImagesApiService)
  });
