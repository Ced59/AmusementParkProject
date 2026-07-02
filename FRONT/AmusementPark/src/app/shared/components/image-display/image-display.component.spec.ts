import { SimpleChange } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateService } from '@ngx-translate/core';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { ImageDisplayComponent } from './image-display.component';

describe('ImageDisplayComponent', () => {
  it('builds responsive srcset from the resolved image id', () => {
    const imagesApiService: jasmine.SpyObj<ImagesApiService> = jasmine.createSpyObj<ImagesApiService>('ImagesApiService', [
      'resolveImageUrl',
      'buildImageSrcSet'
    ]);
    imagesApiService.resolveImageUrl.and.returnValue('/api/images/img-1');
    imagesApiService.buildImageSrcSet.and.returnValue('/api/images/img-1?width=320 320w');
    const component = new ImageDisplayComponent(imagesApiService);

    component.imageId = 'img-1';
    component.responsiveWidths = [320];
    component.ngOnChanges({
      imageId: new SimpleChange(null, 'img-1', true),
      responsiveWidths: new SimpleChange(null, [320], true)
    });

    expect(component.resolvedImageUrl).toBe('/api/images/img-1');
    expect(component.resolvedImageSrcSet).toBe('/api/images/img-1?width=320 320w');
    expect(component.resolvedImageSizes).toBe('100vw');
    expect(imagesApiService.resolveImageUrl).toHaveBeenCalledOnceWith('img-1', { width: null });
    expect(imagesApiService.buildImageSrcSet).toHaveBeenCalledOnceWith('img-1', [320]);
  });

  it('uses a dimensioned image url for the fallback src when requested', () => {
    const imagesApiService: jasmine.SpyObj<ImagesApiService> = jasmine.createSpyObj<ImagesApiService>('ImagesApiService', [
      'resolveImageUrl',
      'buildImageSrcSet'
    ]);
    imagesApiService.resolveImageUrl.and.returnValue('/api/images/img-1?width=960&v=2');
    imagesApiService.buildImageSrcSet.and.returnValue('/api/images/img-1?width=960&v=2 960w');
    const component = new ImageDisplayComponent(imagesApiService);

    component.imageId = 'img-1';
    component.srcWidth = 960;
    component.ngOnChanges({
      imageId: new SimpleChange(null, 'img-1', true),
      srcWidth: new SimpleChange(null, 960, true)
    });

    expect(component.resolvedImageUrl).toBe('/api/images/img-1?width=960&v=2');
    expect(imagesApiService.resolveImageUrl).toHaveBeenCalledOnceWith('img-1', { width: 960 });
  });

  it('omits sizes when no responsive srcset can be built', () => {
    const imagesApiService: jasmine.SpyObj<ImagesApiService> = jasmine.createSpyObj<ImagesApiService>('ImagesApiService', [
      'resolveImageUrl',
      'buildImageSrcSet'
    ]);
    imagesApiService.resolveImageUrl.and.returnValue('https://example.com/image.png');
    imagesApiService.buildImageSrcSet.and.returnValue(null);
    const component = new ImageDisplayComponent(imagesApiService);

    component.imagePathOrUrl = 'https://example.com/image.png';
    component.ngOnChanges({
      imagePathOrUrl: new SimpleChange(null, 'https://example.com/image.png', true)
    });

    expect(component.resolvedImageSrcSet).toBeNull();
    expect(component.resolvedImageSizes).toBeNull();
  });

  it('renders localized fallback alt text through the view component', async () => {
    const imagesApiService: jasmine.SpyObj<ImagesApiService> = jasmine.createSpyObj<ImagesApiService>('ImagesApiService', [
      'resolveImageUrl',
      'buildImageSrcSet'
    ]);
    imagesApiService.resolveImageUrl.and.returnValue('/api/images/img-1');
    imagesApiService.buildImageSrcSet.and.returnValue(null);

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, ImageDisplayComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: ImagesApiService, useValue: imagesApiService }
      ]
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      images: {
        fallbackAlt: 'Image AMUSEMENT-PARKS.fun'
      }
    });
    translateService.use('fr');

    const fixture: ComponentFixture<ImageDisplayComponent> = TestBed.createComponent(ImageDisplayComponent);
    const component: ImageDisplayComponent = fixture.componentInstance;
    component.imagePathOrUrl = 'img-1';
    component.alt = ' ';
    component.ngOnChanges({
      imagePathOrUrl: new SimpleChange(null, 'img-1', true)
    });

    fixture.detectChanges();

    const image: HTMLImageElement = fixture.debugElement.query(By.css('img')).nativeElement;
    expect(image.getAttribute('alt')).toBe('Image AMUSEMENT-PARKS.fun');
  });
});
