import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { ImageDisplayViewComponent } from './image-display-view.component';

describe('ImageDisplayViewComponent', () => {
  let fixture: ComponentFixture<ImageDisplayViewComponent>;
  let component: ImageDisplayViewComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ImageDisplayViewComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ImageDisplayViewComponent);
    component = fixture.componentInstance;
  });

  it('renders responsive image attributes when provided', () => {
    component.showImage = true;
    component.resolvedImageUrl = '/api/images/img-1';
    component.resolvedImageSrcSet = '/api/images/img-1?width=320 320w, /api/images/img-1?width=640 640w';
    component.resolvedImageSizes = '(max-width: 900px) 100vw, 900px';

    fixture.detectChanges();

    const image: HTMLImageElement = fixture.debugElement.query(By.css('img')).nativeElement;
    expect(image.getAttribute('srcset')).toBe('/api/images/img-1?width=320 320w, /api/images/img-1?width=640 640w');
    expect(image.getAttribute('sizes')).toBe('(max-width: 900px) 100vw, 900px');
  });

  it('renders fetch priority when provided for LCP images', () => {
    component.showImage = true;
    component.resolvedImageUrl = 'data:image/gif;base64,R0lGODlhAQABAAAAACw=';
    component.loading = 'eager';
    component.fetchPriority = 'high';

    fixture.detectChanges();

    const image: HTMLImageElement = fixture.debugElement.query(By.css('img')).nativeElement;
    expect(image.getAttribute('loading')).toBe('eager');
    expect(image.getAttribute('fetchpriority')).toBe('high');
  });

  it('does not render srcset or sizes attributes when they are absent', () => {
    component.showImage = true;
    component.resolvedImageUrl = 'https://example.com/image.png';

    fixture.detectChanges();

    const image: HTMLImageElement = fixture.debugElement.query(By.css('img')).nativeElement;
    expect(image.hasAttribute('srcset')).toBeFalse();
    expect(image.hasAttribute('sizes')).toBeFalse();
  });

  it('falls back to a non-empty alt text when the provided alt is blank', () => {
    component.showImage = true;
    component.resolvedImageUrl = 'https://example.com/image.png';
    component.alt = '   ';
    component.fallbackAlt = 'Image AMUSEMENT-PARKS.fun';

    fixture.detectChanges();

    const image: HTMLImageElement = fixture.debugElement.query(By.css('img')).nativeElement;
    expect(image.getAttribute('alt')).toBe('Image AMUSEMENT-PARKS.fun');
  });
});
