import { NO_ERRORS_SCHEMA } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ImageCategory } from '@app/models/images/image-category';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { SafeExternalUrlPipe, SafeRichHtmlPipe } from '@shared/pipes';
import { mapParkToDetailViewModel } from '../mappers/park-detail-view.mapper';
import { ParkDetailViewComponent } from './park-detail-view.component';

describe('ParkDetailViewComponent gallery accessible name', () => {
  it('uses the visible caption without replacing it with a different name', async () => {
    await TestBed.configureTestingModule({
      imports: [ParkDetailViewComponent], providers: provideCommonTestDependencies()
    }).overrideComponent(ParkDetailViewComponent, {
      set: { imports: [TranslateModule, RouterLink, SafeExternalUrlPipe, SafeRichHtmlPipe], schemas: [NO_ERRORS_SCHEMA] }
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
  });
});
