import { SimpleChange } from '@angular/core';

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
});
