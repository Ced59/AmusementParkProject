import { SimpleChange } from '@angular/core';

import { UiPhotoCarouselComponent } from './ui-photo-carousel.component';
import { UiPhotoCarouselImage } from '../models/ui-photo-carousel.model';

describe('UiPhotoCarouselComponent', () => {
  it('keeps displayed photos stable between reads until the state changes', () => {
    const component = new UiPhotoCarouselComponent();
    component.photos = [
      buildPhoto('photo-1', 'coasters'),
      buildPhoto('photo-2', 'shows'),
      buildPhoto('photo-3', 'coasters')
    ];
    component.defaultDisplayLimit = 2;
    component.ngOnChanges({
      photos: new SimpleChange(undefined, component.photos, true),
      defaultDisplayLimit: new SimpleChange(undefined, component.defaultDisplayLimit, true)
    });
    component.setLimit(2);

    const initialDisplayedPhotos = component.displayedPhotos;
    const initialFilteredPhotos = component.filteredPhotos;

    expect(component.displayedPhotos).toBe(initialDisplayedPhotos);
    expect(component.filteredPhotos).toBe(initialFilteredPhotos);
    expect(component.displayedPhotos.map((photo: UiPhotoCarouselImage) => photo.imageId)).toEqual(['photo-1', 'photo-2']);

    component.selectCategory('coasters');

    expect(component.filteredPhotos).not.toBe(initialFilteredPhotos);
    expect(component.displayedPhotos.map((photo: UiPhotoCarouselImage) => photo.imageId)).toEqual(['photo-1', 'photo-3']);
  });

  it('clamps the active photo when the display limit changes', () => {
    const component = new UiPhotoCarouselComponent();
    component.photos = [
      buildPhoto('photo-1', 'coasters'),
      buildPhoto('photo-2', 'coasters'),
      buildPhoto('photo-3', 'coasters')
    ];
    component.defaultDisplayLimit = 0;
    component.ngOnChanges({
      photos: new SimpleChange(undefined, component.photos, true),
      defaultDisplayLimit: new SimpleChange(undefined, component.defaultDisplayLimit, true)
    });

    component.selectPhoto(2);
    component.setLimit(1);

    expect(component.activePhotoIndex).toBe(0);
    expect(component.activePhoto?.imageId).toBe('photo-1');
  });
});

function buildPhoto(imageId: string, categoryKey: string): UiPhotoCarouselImage {
  return {
    id: imageId,
    imageId,
    alt: imageId,
    categoryKey,
    categoryLabelKey: `category.${categoryKey}`,
    description: null
  };
}
