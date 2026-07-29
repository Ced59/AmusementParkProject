import type { MockedObject } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { of } from 'rxjs';

import {
  TechnicalContentBlock,
  TechnicalPage,
} from '@app/models/technical-pages/technical-page';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import {
  PUBLIC_TECHNICAL_PAGES_API_SERVICE_PORT,
  PUBLIC_TECHNICAL_PAGES_IMAGES_API_SERVICE_PORT,
  PublicTechnicalPagesApiServicePort,
  PublicTechnicalPagesImagesApiServicePort,
} from '../state/public-technical-pages-data.ports';
import { TechnicalPageDetailPageComponent } from './technical-page-detail-page.component';

describe('TechnicalPageDetailPageComponent', (): void => {
  it('loads the detail and resolves its image through the dedicated ports', (): void => {
    const page: TechnicalPage = { slug: 'lift' } as TechnicalPage;
    const apiPort: MockedObject<PublicTechnicalPagesApiServicePort> = {
      getBySlug: vi
        .fn()
        .mockName('PublicTechnicalPagesApiServicePort.getBySlug'),
    } as unknown as MockedObject<PublicTechnicalPagesApiServicePort>;
    const imagesPort: MockedObject<PublicTechnicalPagesImagesApiServicePort> = {
      resolveImageUrl: vi
        .fn()
        .mockName('PublicTechnicalPagesImagesApiServicePort.resolveImageUrl'),
    } as unknown as MockedObject<PublicTechnicalPagesImagesApiServicePort>;
    apiPort.getBySlug.mockReturnValue(of(page));
    imagesPort.resolveImageUrl.mockReturnValue('https://cdn.example/lift.webp');

    TestBed.configureTestingModule({
      imports: [TechnicalPageDetailPageComponent],
      providers: [
        { provide: PUBLIC_TECHNICAL_PAGES_API_SERVICE_PORT, useValue: apiPort },
        {
          provide: PUBLIC_TECHNICAL_PAGES_IMAGES_API_SERVICE_PORT,
          useValue: imagesPort,
        },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap({ lang: 'fr', slug: 'lift' }),
            },
            paramMap: of(convertToParamMap({ lang: 'fr', slug: 'lift' })),
            parent: null,
          },
        },
        { provide: Router, useValue: { url: '/fr/technical/lift' } },
        {
          provide: TranslationService,
          useValue: { getCurrentLang: (): string => 'fr' },
        },
        {
          provide: SeoService,
          useValue: {
            applyTechnicalPageSeo: vi
              .fn()
              .mockName('SeoService.applyTechnicalPageSeo'),
            applyNotFoundSeo: vi.fn().mockName('SeoService.applyNotFoundSeo'),
          },
        },
        {
          provide: SsrHttpStatusService,
          useValue: {
            setStatus: vi.fn().mockName('SsrHttpStatusService.setStatus'),
          },
        },
      ],
    }).overrideComponent(TechnicalPageDetailPageComponent, {
      set: { template: '' },
    });

    const fixture: ComponentFixture<TechnicalPageDetailPageComponent> =
      TestBed.createComponent(TechnicalPageDetailPageComponent);
    fixture.detectChanges();
    const component: TechnicalPageDetailPageComponent =
      fixture.componentInstance;
    const block: TechnicalContentBlock = {
      imageId: 'image-id',
    } as TechnicalContentBlock;

    expect(apiPort.getBySlug).toHaveBeenCalledTimes(1);

    expect(apiPort.getBySlug).toHaveBeenCalledWith('lift');
    expect(
      (
        component as unknown as {
          imageUrl(value: TechnicalContentBlock): string | null;
        }
      ).imageUrl(block),
    ).toBe('https://cdn.example/lift.webp');
    expect(imagesPort.resolveImageUrl).toHaveBeenCalledWith('image-id', {
      width: 1280,
    });
  });
});
