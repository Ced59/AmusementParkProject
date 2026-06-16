import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';

import { ImagesApiService } from '@data-access/images/images-api.service';
import { LcpImagePreloadService } from './lcp-image-preload.service';

describe('LcpImagePreloadService', () => {
  let service: LcpImagePreloadService;
  let testDocument: Document;
  let imagesApiService: jasmine.SpyObj<ImagesApiService>;

  beforeEach(() => {
    testDocument = document.implementation.createHTMLDocument('LCP preload test');
    imagesApiService = jasmine.createSpyObj<ImagesApiService>('ImagesApiService', [
      'resolveImageUrl',
      'buildImageSrcSet'
    ]);
    imagesApiService.resolveImageUrl.and.returnValue('/api/images/img-1?width=960&v=2');
    imagesApiService.buildImageSrcSet.and.returnValue('/api/images/img-1?width=320&v=2 320w, /api/images/img-1?width=960&v=2 960w');

    TestBed.configureTestingModule({
      providers: [
        LcpImagePreloadService,
        { provide: DOCUMENT, useValue: testDocument },
        { provide: ImagesApiService, useValue: imagesApiService }
      ]
    });

    service = TestBed.inject(LcpImagePreloadService);
  });

  it('adds a responsive high-priority image preload to the document head', () => {
    service.preloadImage({
      imageId: 'img-1',
      fallbackWidth: 960,
      responsiveWidths: [320, 960],
      sizes: '(max-width: 900px) 100vw, 900px'
    });

    const preloadLink: HTMLLinkElement | null = testDocument.head.querySelector('link[data-app-lcp-image-preload="true"]');

    expect(preloadLink).not.toBeNull();
    expect(preloadLink?.getAttribute('rel')).toBe('preload');
    expect(preloadLink?.getAttribute('as')).toBe('image');
    expect(preloadLink?.getAttribute('href')).toBe('/api/images/img-1?width=960&v=2');
    expect(preloadLink?.getAttribute('fetchpriority')).toBe('high');
    expect(preloadLink?.getAttribute('imagesrcset')).toBe('/api/images/img-1?width=320&v=2 320w, /api/images/img-1?width=960&v=2 960w');
    expect(preloadLink?.getAttribute('imagesizes')).toBe('(max-width: 900px) 100vw, 900px');
    expect(imagesApiService.resolveImageUrl).toHaveBeenCalledOnceWith('img-1', { width: 960 });
    expect(imagesApiService.buildImageSrcSet).toHaveBeenCalledOnceWith('img-1', [320, 960]);
  });

  it('replaces stale LCP image preloads when the hero image changes', () => {
    service.preloadImage({
      imageId: 'img-1',
      fallbackWidth: 960,
      responsiveWidths: [960],
      sizes: '100vw'
    });

    imagesApiService.resolveImageUrl.and.returnValue('/api/images/img-2?width=960&v=2');
    imagesApiService.buildImageSrcSet.and.returnValue('/api/images/img-2?width=960&v=2 960w');

    service.preloadImage({
      imageId: 'img-2',
      fallbackWidth: 960,
      responsiveWidths: [960],
      sizes: '100vw'
    });

    const preloadLinks: HTMLLinkElement[] = Array.from(testDocument.head.querySelectorAll('link[data-app-lcp-image-preload="true"]'));

    expect(preloadLinks.length).toBe(1);
    expect(preloadLinks[0]?.getAttribute('href')).toBe('/api/images/img-2?width=960&v=2');
  });

  it('clears the LCP image preload when no hero image is available', () => {
    service.preloadImage({
      imageId: 'img-1',
      fallbackWidth: 960,
      responsiveWidths: [960],
      sizes: '100vw'
    });

    service.preloadImage({
      imageId: null,
      fallbackWidth: 960,
      responsiveWidths: [960],
      sizes: '100vw'
    });

    expect(testDocument.head.querySelector('link[data-app-lcp-image-preload="true"]')).toBeNull();
  });
});
