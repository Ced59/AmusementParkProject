import type { MockedObject } from 'vitest';
import { ChangeDetectorRef, SimpleChange } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateService } from '@ngx-translate/core';

import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies,
} from '@app/testing/common-test-providers';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { ImageDisplayComponent } from './image-display.component';

describe('ImageDisplayComponent', () => {
  function createChangeDetectorRef(): MockedObject<ChangeDetectorRef> {
    return {
      markForCheck: vi.fn().mockName('ChangeDetectorRef.markForCheck'),
    } as unknown as MockedObject<ChangeDetectorRef>;
  }

  it('builds responsive srcset from the resolved image id', () => {
    const imagesApiService: MockedObject<ImagesApiService> = {
      resolveImageUrl: vi.fn().mockName('ImagesApiService.resolveImageUrl'),
      buildImageSrcSet: vi.fn().mockName('ImagesApiService.buildImageSrcSet'),
    } as unknown as MockedObject<ImagesApiService>;
    imagesApiService.resolveImageUrl.mockReturnValue('/api/images/img-1');
    imagesApiService.buildImageSrcSet.mockReturnValue(
      '/api/images/img-1?width=320 320w',
    );
    const component = new ImageDisplayComponent(
      imagesApiService,
      createChangeDetectorRef(),
    );

    component.imageId = 'img-1';
    component.responsiveWidths = [320];
    component.ngOnChanges({
      imageId: new SimpleChange(null, 'img-1', true),
      responsiveWidths: new SimpleChange(null, [320], true),
    });

    expect(component.resolvedImageUrl).toBe('/api/images/img-1');
    expect(component.resolvedImageSrcSet).toBe(
      '/api/images/img-1?width=320 320w',
    );
    expect(component.resolvedImageSizes).toBe('100vw');
    expect(imagesApiService.resolveImageUrl).toHaveBeenCalledTimes(1);
    expect(imagesApiService.resolveImageUrl).toHaveBeenCalledWith('img-1', {
      width: null,
    });
    expect(imagesApiService.buildImageSrcSet).toHaveBeenCalledTimes(1);
    expect(imagesApiService.buildImageSrcSet).toHaveBeenCalledWith('img-1', [
      320,
    ]);
  });

  it('uses a dimensioned image url for the fallback src when requested', () => {
    const imagesApiService: MockedObject<ImagesApiService> = {
      resolveImageUrl: vi.fn().mockName('ImagesApiService.resolveImageUrl'),
      buildImageSrcSet: vi.fn().mockName('ImagesApiService.buildImageSrcSet'),
    } as unknown as MockedObject<ImagesApiService>;
    imagesApiService.resolveImageUrl.mockReturnValue(
      '/api/images/img-1?width=960&v=2',
    );
    imagesApiService.buildImageSrcSet.mockReturnValue(
      '/api/images/img-1?width=960&v=2 960w',
    );
    const component = new ImageDisplayComponent(
      imagesApiService,
      createChangeDetectorRef(),
    );

    component.imageId = 'img-1';
    component.srcWidth = 960;
    component.ngOnChanges({
      imageId: new SimpleChange(null, 'img-1', true),
      srcWidth: new SimpleChange(null, 960, true),
    });

    expect(component.resolvedImageUrl).toBe('/api/images/img-1?width=960&v=2');
    expect(imagesApiService.resolveImageUrl).toHaveBeenCalledTimes(1);
    expect(imagesApiService.resolveImageUrl).toHaveBeenCalledWith('img-1', {
      width: 960,
    });
  });

  it('omits sizes when no responsive srcset can be built', () => {
    const imagesApiService: MockedObject<ImagesApiService> = {
      resolveImageUrl: vi.fn().mockName('ImagesApiService.resolveImageUrl'),
      buildImageSrcSet: vi.fn().mockName('ImagesApiService.buildImageSrcSet'),
    } as unknown as MockedObject<ImagesApiService>;
    imagesApiService.resolveImageUrl.mockReturnValue(
      'https://example.com/image.png',
    );
    imagesApiService.buildImageSrcSet.mockReturnValue(null);
    const component = new ImageDisplayComponent(
      imagesApiService,
      createChangeDetectorRef(),
    );

    component.imagePathOrUrl = 'https://example.com/image.png';
    component.ngOnChanges({
      imagePathOrUrl: new SimpleChange(
        null,
        'https://example.com/image.png',
        true,
      ),
    });

    expect(component.resolvedImageSrcSet).toBeNull();
    expect(component.resolvedImageSizes).toBeNull();
  });

  it('retries managed image binaries twice before keeping the fallback', () => {
    vi.useFakeTimers();
    try {
      const imagesApiService: MockedObject<ImagesApiService> = {
        resolveImageUrl: vi.fn().mockName('ImagesApiService.resolveImageUrl'),
        buildImageSrcSet: vi
          .fn()
          .mockName('ImagesApiService.buildImageSrcSet'),
      } as unknown as MockedObject<ImagesApiService>;
      imagesApiService.resolveImageUrl.mockImplementation(
        (_value, options) =>
          `/api/images/binary/img-1?width=960&retry=${options?.retryAttempt ?? 0}`,
      );
      imagesApiService.buildImageSrcSet.mockImplementation(
        (_value, _widths, options) =>
          `/api/images/binary/img-1?width=640&retry=${options?.retryAttempt ?? 0} 640w`,
      );
      const changeDetectorRef: MockedObject<ChangeDetectorRef> =
        createChangeDetectorRef();
      const component = new ImageDisplayComponent(
        imagesApiService,
        changeDetectorRef,
      );
      component.imageId = 'img-1';
      component.srcWidth = 960;
      component.ngOnChanges({
        imageId: new SimpleChange(null, 'img-1', true),
      });

      component.onImageError();
      expect(component.imageLoadFailed).toBe(true);

      vi.advanceTimersByTime(350);
      expect(component.imageLoadFailed).toBe(false);
      expect(component.resolvedImageUrl).toContain('retry=1');
      expect(component.resolvedImageSrcSet).toContain('retry=1');

      component.onImageError();
      vi.advanceTimersByTime(700);
      expect(component.imageLoadFailed).toBe(false);
      expect(component.resolvedImageUrl).toContain('retry=2');

      component.onImageError();
      vi.runAllTimers();
      expect(component.imageLoadFailed).toBe(true);
      expect(imagesApiService.resolveImageUrl).toHaveBeenCalledTimes(3);
      expect(changeDetectorRef.markForCheck).toHaveBeenCalledTimes(2);
    } finally {
      vi.useRealTimers();
    }
  });

  it('does not retry unmanaged external images', () => {
    vi.useFakeTimers();
    try {
      const imagesApiService: MockedObject<ImagesApiService> = {
        resolveImageUrl: vi.fn().mockName('ImagesApiService.resolveImageUrl'),
        buildImageSrcSet: vi
          .fn()
          .mockName('ImagesApiService.buildImageSrcSet'),
      } as unknown as MockedObject<ImagesApiService>;
      imagesApiService.resolveImageUrl.mockReturnValue(
        'https://example.com/image.png',
      );
      imagesApiService.buildImageSrcSet.mockReturnValue(null);
      const component = new ImageDisplayComponent(
        imagesApiService,
        createChangeDetectorRef(),
      );
      component.imagePathOrUrl = 'https://example.com/image.png';
      component.ngOnChanges({
        imagePathOrUrl: new SimpleChange(
          null,
          'https://example.com/image.png',
          true,
        ),
      });

      component.onImageError();
      vi.runAllTimers();

      expect(component.imageLoadFailed).toBe(true);
      expect(imagesApiService.resolveImageUrl).toHaveBeenCalledTimes(1);
    } finally {
      vi.useRealTimers();
    }
  });

  it('cancels a pending retry when the image source changes', () => {
    vi.useFakeTimers();
    try {
      const imagesApiService: MockedObject<ImagesApiService> = {
        resolveImageUrl: vi.fn().mockName('ImagesApiService.resolveImageUrl'),
        buildImageSrcSet: vi
          .fn()
          .mockName('ImagesApiService.buildImageSrcSet'),
      } as unknown as MockedObject<ImagesApiService>;
      imagesApiService.resolveImageUrl.mockImplementation(
        (value) => `/api/images/binary/${value}`,
      );
      imagesApiService.buildImageSrcSet.mockImplementation(
        (value) => `/api/images/binary/${value}?width=640 640w`,
      );
      const component = new ImageDisplayComponent(
        imagesApiService,
        createChangeDetectorRef(),
      );
      component.imageId = 'img-1';
      component.ngOnChanges({
        imageId: new SimpleChange(null, 'img-1', true),
      });
      component.onImageError();

      component.imageId = 'img-2';
      component.ngOnChanges({
        imageId: new SimpleChange('img-1', 'img-2', false),
      });
      vi.runAllTimers();

      expect(component.imageLoadFailed).toBe(false);
      expect(component.resolvedImageUrl).toBe('/api/images/binary/img-2');
      expect(imagesApiService.resolveImageUrl).toHaveBeenCalledTimes(2);
    } finally {
      vi.useRealTimers();
    }
  });

  it('renders localized fallback alt text through the view component', async () => {
    const imagesApiService: MockedObject<ImagesApiService> = {
      resolveImageUrl: vi.fn().mockName('ImagesApiService.resolveImageUrl'),
      buildImageSrcSet: vi.fn().mockName('ImagesApiService.buildImageSrcSet'),
    } as unknown as MockedObject<ImagesApiService>;
    imagesApiService.resolveImageUrl.mockReturnValue('/api/images/img-1');
    imagesApiService.buildImageSrcSet.mockReturnValue(null);

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, ImageDisplayComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: ImagesApiService, useValue: imagesApiService },
      ],
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      images: {
        fallbackAlt: 'Image AMUSEMENT-PARKS.fun',
      },
    });
    translateService.use('fr');

    const fixture: ComponentFixture<ImageDisplayComponent> =
      TestBed.createComponent(ImageDisplayComponent);
    const component: ImageDisplayComponent = fixture.componentInstance;
    component.imagePathOrUrl = 'img-1';
    component.alt = ' ';
    component.ngOnChanges({
      imagePathOrUrl: new SimpleChange(null, 'img-1', true),
    });

    fixture.detectChanges();

    const image: HTMLImageElement = fixture.debugElement.query(
      By.css('img'),
    ).nativeElement;
    expect(image.getAttribute('alt')).toBe('Image AMUSEMENT-PARKS.fun');
  });
});
