import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { of } from 'rxjs';

import { TechnicalContentBlock, TechnicalPage } from '@app/models/technical-pages/technical-page';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import {
  PUBLIC_TECHNICAL_PAGES_API_SERVICE_PORT,
  PUBLIC_TECHNICAL_PAGES_IMAGES_API_SERVICE_PORT,
  PublicTechnicalPagesApiServicePort,
  PublicTechnicalPagesImagesApiServicePort
} from '../state/public-technical-pages-data.ports';
import { TechnicalPageDetailPageComponent } from './technical-page-detail-page.component';

describe('TechnicalPageDetailPageComponent', (): void => {
  it('loads the detail and resolves its image through the dedicated ports', (): void => {
    const page: TechnicalPage = { slug: 'lift' } as TechnicalPage;
    const apiPort: jasmine.SpyObj<PublicTechnicalPagesApiServicePort> =
      jasmine.createSpyObj<PublicTechnicalPagesApiServicePort>('PublicTechnicalPagesApiServicePort', ['getBySlug']);
    const imagesPort: jasmine.SpyObj<PublicTechnicalPagesImagesApiServicePort> =
      jasmine.createSpyObj<PublicTechnicalPagesImagesApiServicePort>('PublicTechnicalPagesImagesApiServicePort', ['resolveImageUrl']);
    apiPort.getBySlug.and.returnValue(of(page));
    imagesPort.resolveImageUrl.and.returnValue('https://cdn.example/lift.webp');

    TestBed.configureTestingModule({
      imports: [TechnicalPageDetailPageComponent],
      providers: [
        { provide: PUBLIC_TECHNICAL_PAGES_API_SERVICE_PORT, useValue: apiPort },
        { provide: PUBLIC_TECHNICAL_PAGES_IMAGES_API_SERVICE_PORT, useValue: imagesPort },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ lang: 'fr', slug: 'lift' }) },
            paramMap: of(convertToParamMap({ lang: 'fr', slug: 'lift' })),
            parent: null
          }
        },
        { provide: Router, useValue: { url: '/fr/technical/lift' } },
        { provide: TranslationService, useValue: { getCurrentLang: (): string => 'fr' } },
        {
          provide: SeoService,
          useValue: jasmine.createSpyObj<SeoService>('SeoService', ['applyTechnicalPageSeo', 'applyNotFoundSeo'])
        },
        { provide: SsrHttpStatusService, useValue: jasmine.createSpyObj<SsrHttpStatusService>('SsrHttpStatusService', ['setStatus']) }
      ]
    }).overrideComponent(TechnicalPageDetailPageComponent, {
      set: { template: '' }
    });

    const fixture: ComponentFixture<TechnicalPageDetailPageComponent> = TestBed.createComponent(TechnicalPageDetailPageComponent);
    fixture.detectChanges();
    const component: TechnicalPageDetailPageComponent = fixture.componentInstance;
    const block: TechnicalContentBlock = { imageId: 'image-id' } as TechnicalContentBlock;

    expect(apiPort.getBySlug).toHaveBeenCalledOnceWith('lift');
    expect((component as unknown as { imageUrl(value: TechnicalContentBlock): string | null }).imageUrl(block))
      .toBe('https://cdn.example/lift.webp');
    expect(imagesPort.resolveImageUrl).toHaveBeenCalledWith('image-id', { width: 1280 });
  });
});
