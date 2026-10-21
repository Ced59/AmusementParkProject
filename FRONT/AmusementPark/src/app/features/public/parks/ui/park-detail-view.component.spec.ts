import { NO_ERRORS_SCHEMA } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ImageCategory } from '@app/models/images/image-category';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { SafeExternalUrlPipe, SafeRichHtmlPipe } from '@shared/pipes';
import { mapParkToDetailViewModel } from '../mappers/park-detail-view.mapper';
import { ParkDetailViewComponent } from './park-detail-view.component';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { PARK_DETAIL_HERO_IMAGE_SIZES, PARK_DETAIL_HERO_IMAGE_WIDTH, PARK_DETAIL_MAIN_PHOTO_SIZES } from '../models/park-detail-image-layout';

describe('ParkDetailViewComponent gallery accessible name', () => {
  it('shows the localized country in the hero location without a raw country-code badge', async () => {
    await TestBed.configureTestingModule({
      imports: [ParkDetailViewComponent], providers: provideCommonTestDependencies()
    }).overrideComponent(ParkDetailViewComponent, {
      set: { imports: [TranslateModule, RouterLink, SafeExternalUrlPipe, SafeRichHtmlPipe, ImageDisplayComponent], schemas: [NO_ERRORS_SCHEMA] }
    }).compileComponents();
    const fixture = TestBed.createComponent(ParkDetailViewComponent);
    const park = mapParkToDetailViewModel({
      id: 'park-1', name: 'Example Park', countryCode: 'PT', city: 'Quarteira',
      status: 'Operating', latitude: 37, longitude: -8
    }, 'pl');
    fixture.componentRef.setInput('park', { ...park, countryName: 'Portugalia', locationLine: 'Quarteira · Portugalia' });
    fixture.detectChanges();
    const hero: HTMLElement = fixture.nativeElement.querySelector('.park-banner__meta');
    expect(hero.textContent).toContain('Quarteira · Portugalia');
    expect(hero.textContent).not.toMatch(/\bPT\b/);
  });

  it('preserves the gallery name and distinguishes an actual hero photo from the logo fallback', async () => {
    await TestBed.configureTestingModule({
      imports: [ParkDetailViewComponent], providers: provideCommonTestDependencies()
    }).overrideComponent(ParkDetailViewComponent, {
      set: { imports: [TranslateModule, RouterLink, SafeExternalUrlPipe, SafeRichHtmlPipe, ImageDisplayComponent], schemas: [NO_ERRORS_SCHEMA] }
    }).compileComponents();
    const translate: TranslateService = TestBed.inject(TranslateService);
    translate.setTranslation('fr', { parks: { photos: { title: 'Photos' }, imagesPage: { kicker: 'Galerie du parc' } } });
    translate.use('fr');
    const fixture = TestBed.createComponent(ParkDetailViewComponent);
    const park = mapParkToDetailViewModel({ id: 'park-1', name: 'Example Park', status: 'Operating', latitude: 48, longitude: 2 }, 'fr');
    park.imagesLink = ['/', 'fr', 'park', 'park-1', 'example-park', 'images'];
    park.primaryPhoto = {
      id: 'image-1', imageId: 'image-1', alt: 'Example Park', category: ImageCategory.PARK,
      categoryKey: 'park', categoryLabelKey: 'parks.photos.title', description: null,
      year: '', yearLabel: '', tagKeys: [], tagLabels: []
    };
    fixture.componentRef.setInput('park', park);
    fixture.detectChanges();
    const link: HTMLAnchorElement = fixture.nativeElement.querySelector('.park-main-photo__link');
    expect(link.textContent).toContain('Photos');
    expect(link.textContent).toContain('Galerie du parc');
    expect(link.hasAttribute('aria-label')).toBe(false);
    expect(link.getAttribute('href')).toContain('/images');
    expect(link.querySelector('img')?.getAttribute('sizes')).toBe(PARK_DETAIL_MAIN_PHOTO_SIZES);
    expect(fixture.componentInstance.heroImageSizes).toBe(PARK_DETAIL_HERO_IMAGE_SIZES);
    expect(fixture.componentInstance.heroImageSrcWidth).toBe(PARK_DETAIL_HERO_IMAGE_WIDTH);
    expect(fixture.nativeElement.querySelector('.park-banner').classList.contains('park-banner--has-photo')).toBe(false);
    fixture.componentRef.setInput('park', { ...park, heroImageId: 'image-1', logoImageId: 'logo-1' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.park-banner').classList.contains('park-banner--has-photo')).toBe(true);
    const hero: HTMLImageElement = fixture.nativeElement.querySelector('.park-banner__photo img');
    expect(hero.getAttribute('loading')).toBe('eager');
    expect(hero.getAttribute('fetchpriority')).toBe('high');
    expect(hero.getAttribute('sizes')).toBe(PARK_DETAIL_HERO_IMAGE_SIZES);
    fixture.componentRef.setInput('park', { ...park, heroImageId: 'logo-1', logoImageId: 'logo-1' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.park-banner').classList.contains('park-banner--has-photo')).toBe(false);
    expect(fixture.nativeElement.querySelector('.park-logo-frame app-image-display')).not.toBeNull();
  });
});
