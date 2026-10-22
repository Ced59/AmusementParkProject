import { NO_ERRORS_SCHEMA, SimpleChange } from '@angular/core';
import { NgIf } from '@angular/common';
import { CdkTrapFocus, InteractivityChecker } from '@angular/cdk/a11y';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { LeafletMapComponent } from '@shared/components/leaflet-map/leaflet-map.component';

import { UiPhotoCarouselComponent } from './ui-photo-carousel.component';
import { UiPhotoCarouselImage } from '../models/ui-photo-carousel.model';

describe('UiPhotoCarouselComponent', () => {
  it('shows descriptions and credits without technical details in the gallery, map and lightbox', async () => {
    await TestBed.configureTestingModule({
      imports: [UiPhotoCarouselComponent, TranslateModule.forRoot()],
      providers: [provideRouter([])]
    }).overrideComponent(UiPhotoCarouselComponent, {
      remove: { imports: [ImageDisplayComponent, LeafletMapComponent] },
      add: { schemas: [NO_ERRORS_SCHEMA] }
    }).compileComponents();
    const fixture = TestBed.createComponent(UiPhotoCarouselComponent);
    const photo: UiPhotoCarouselImage = {
      ...buildPhoto('photo-1', 'coasters', '2024', 50.1, 3.1),
      description: 'Le château au coucher du soleil',
      credit: 'Camille — Parc des merveilles',
      originalFileName: 'DSC_1234_PRIVATE.jpg',
      contentType: 'image/jpeg',
      sizeInBytes: 1048576,
      uploadedAt: '2025-02-03T04:05:06Z',
      cameraMaker: 'Technical camera maker',
      cameraModel: 'Technical camera model',
      focalLength: 50,
      aperture: 2.8,
      exposureTime: 0.004,
      iso: 400
    };
    try {
      fixture.componentRef.setInput('photos', [photo, buildPhoto('photo-2', 'shows', '2024')]);
      fixture.detectChanges();
      const root: HTMLElement = fixture.nativeElement;
      expect(root.querySelector('.photo-carousel__caption')?.textContent).toContain(photo.description);
      expect(root.querySelector('.photo-carousel__credit')?.textContent).toContain(photo.credit);
      expect(root.querySelector('.photo-carousel-metadata')).toBeNull();
      expect(fixture.componentInstance.mapMarkers[0].title).toBe(photo.description);
      expect(fixture.componentInstance.mapMarkers[0].details).toEqual([photo.credit, 'coasters']);

      fixture.componentInstance.openLightbox(0);
      fixture.detectChanges();
      expect(root.querySelector('.photo-lightbox__caption')?.textContent).toContain(photo.description);
      expect(root.querySelector('.photo-lightbox__credit')?.textContent).toContain(photo.credit);
      for (const technicalDetail of [photo.originalFileName, photo.contentType, photo.cameraMaker, photo.cameraModel,
        '1200 x 800px', 'ISO 400', 'f/2.8', '1/250s', '50.10000, 3.10000', photo.uploadedAt]) {
        expect(root.textContent).not.toContain(technicalDetail);
      }

      fixture.componentInstance.nextPhoto();
      fixture.detectChanges();
      expect(root.querySelector('.photo-carousel__credit')).toBeNull();
      expect(root.querySelector('.photo-lightbox__credit')).toBeNull();
      expect(photo.originalFileName).toBe('DSC_1234_PRIVATE.jpg');
      expect(photo.iso).toBe(400);
    } finally {
      fixture.destroy();
    }
  });

  it('captures, loops and restores modal keyboard focus using the existing CDK', async () => {
    await TestBed.configureTestingModule({
      imports: [UiPhotoCarouselComponent],
      providers: [{ provide: InteractivityChecker, useValue: {
        // The unit DOM has no layout; browser visibility is exercised by CI.
        isFocusable: (element: HTMLElement): boolean => element.tagName === 'BUTTON' && !element.hasAttribute('disabled'),
        isTabbable: (element: HTMLElement): boolean => element.tagName === 'BUTTON' && !element.hasAttribute('disabled')
      } }]
    }).overrideComponent(UiPhotoCarouselComponent, { set: {
      imports: [NgIf, CdkTrapFocus],
      template: `<button class="opener" (click)="openLightbox(0)">Open</button>
        <div *ngIf="lightboxOpen" cdkTrapFocus [cdkTrapFocusAutoCapture]="true">
          <button class="close" cdkFocusInitial (click)="closeLightbox()">Close</button>
          <button class="last" (click)="nextPhoto()">Next</button>
        </div>`
    } }).compileComponents();
    const fixture = TestBed.createComponent(UiPhotoCarouselComponent);
    try {
      fixture.componentInstance.photos = [buildPhoto('photo-1', 'coasters', '2024')];
      fixture.componentInstance.ngOnChanges({ photos: new SimpleChange(undefined, fixture.componentInstance.photos, true) });
      fixture.detectChanges();
      const root: HTMLElement = fixture.nativeElement;
      const opener: HTMLButtonElement = root.querySelector<HTMLButtonElement>('.opener')!;
      opener.focus();
      opener.click();
      fixture.detectChanges();
      await fixture.whenStable();
      const close: HTMLButtonElement = root.querySelector<HTMLButtonElement>('.close')!;
      const last: HTMLButtonElement = root.querySelector<HTMLButtonElement>('.last')!;
      const anchors: NodeListOf<HTMLElement> = root.querySelectorAll<HTMLElement>('.cdk-focus-trap-anchor');
      expect(document.activeElement).toBe(close);
      expect(anchors.length).toBe(2);
      anchors[0].focus();
      expect(document.activeElement).toBe(last);
      anchors[1].focus();
      expect(document.activeElement).toBe(close);
      close.click();
      fixture.detectChanges();
      await fixture.whenStable();
      expect(root.querySelector('.cdk-focus-trap-anchor')).toBeNull();
      expect(document.activeElement).toBe(opener);
    } finally {
      fixture.destroy();
    }
  });

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
