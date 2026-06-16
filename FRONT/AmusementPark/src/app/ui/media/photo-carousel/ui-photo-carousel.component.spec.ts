import { SimpleChange } from '@angular/core';

import { UiPhotoCarouselComponent } from './ui-photo-carousel.component';
import { UiPhotoCarouselImage } from '../models/ui-photo-carousel.model';

describe('UiPhotoCarouselComponent', () => {
  it('keeps displayed photos stable between reads until the state changes', () => {
    const component = new UiPhotoCarouselComponent();
    component.photos = [
      buildPhoto('photo-1', 'coasters', '2024'),
      buildPhoto('photo-2', 'shows', '2023'),
      buildPhoto('photo-3', 'coasters', '2024')
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
      buildPhoto('photo-1', 'coasters', '2024'),
      buildPhoto('photo-2', 'coasters', '2024'),
      buildPhoto('photo-3', 'coasters', '2024')
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

  it('builds year and tag axes with counts', () => {
    const component = new UiPhotoCarouselComponent();
    component.photos = [
      buildPhoto('photo-1', 'coasters', '2024'),
      buildPhoto('photo-2', 'shows', '2023'),
      buildPhoto('photo-3', 'coasters', '2024')
    ];
    component.defaultDisplayLimit = 0;

    component.ngOnChanges({
      photos: new SimpleChange(undefined, component.photos, true),
      defaultDisplayLimit: new SimpleChange(undefined, component.defaultDisplayLimit, true)
    });

    expect(component.yearOptions.map((option) => ({ key: option.key, count: option.count }))).toEqual([
      { key: '2024', count: 2 },
      { key: '2023', count: 1 }
    ]);
    expect(component.tagOptions.map((option) => ({ key: option.key, count: option.count }))).toEqual([
      { key: 'coasters', count: 2 },
      { key: 'shows', count: 1 }
    ]);
  });

  it('filters photos by selected year and tag', () => {
    const component = new UiPhotoCarouselComponent();
    component.photos = [
      buildPhoto('photo-1', 'coasters', '2024'),
      buildPhoto('photo-2', 'shows', '2024'),
      buildPhoto('photo-3', 'coasters', '2023')
    ];
    component.defaultDisplayLimit = 0;

    component.ngOnChanges({
      photos: new SimpleChange(undefined, component.photos, true),
      defaultDisplayLimit: new SimpleChange(undefined, component.defaultDisplayLimit, true)
    });

    component.selectYear('2024');
    component.selectTag('coasters');

    expect(component.displayedPhotos.map((photo: UiPhotoCarouselImage) => photo.imageId)).toEqual(['photo-1']);
  });

  it('selects a geolocated photo from a map marker click', () => {
    const component = new UiPhotoCarouselComponent();
    component.photos = [
      buildPhoto('photo-1', 'coasters', '2024', 50.1, 3.1),
      buildPhoto('photo-2', 'shows', '2024', 50.2, 3.2)
    ];
    component.defaultDisplayLimit = 0;

    component.ngOnChanges({
      photos: new SimpleChange(undefined, component.photos, true),
      defaultDisplayLimit: new SimpleChange(undefined, component.defaultDisplayLimit, true)
    });

    component.onMapMarkerClick(component.mapMarkers[1]);

    expect(component.activePhoto?.imageId).toBe('photo-2');
    expect(component.selectedMarkerId).toBe('photo-2');
  });
});

function buildPhoto(
  imageId: string,
  categoryKey: string,
  year: string,
  latitude: number | null = null,
  longitude: number | null = null
): UiPhotoCarouselImage {
  return {
    id: imageId,
    imageId,
    alt: imageId,
    categoryKey,
    categoryLabelKey: `category.${categoryKey}`,
    description: null,
    year,
    yearLabel: year,
    tagKeys: [categoryKey],
    tagLabels: [{ key: categoryKey, label: categoryKey, labelKey: null }],
    latitude,
    longitude,
    width: 1200,
    height: 800,
    takenOn: `${year}-01-01T00:00:00Z`
  };
}
