import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { UiParkCardComponent } from './ui-park-card.component';

describe('UiParkCardComponent accessible website actions', () => {
  let fixture: ComponentFixture<UiParkCardComponent>;
  const park: ParkCardModel = {
    id: 'park-1', name: 'Example Park', countryCode: null, city: null,
    status: 'Operating', statusLabelKey: null, statusIconClass: null, statusTone: 'soft',
    latitude: null, longitude: null, logoImageId: null, websiteUrl: 'https://example.com',
    locationLine: null, addressLine: null, coordinatesLine: null, shortDescription: null,
    isClosedDefinitively: false, isOpenToVisitors: true
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UiParkCardComponent], providers: provideCommonTestDependencies()
    }).compileComponents();
    const translate: TranslateService = TestBed.inject(TranslateService);
    translate.setTranslation('fr', { parks: { actions: { website: 'Site web' } } });
    translate.use('fr');
    fixture = TestBed.createComponent(UiParkCardComponent);
  });

  it('keeps the visible translated label and distinguishes the destination park', () => {
    fixture.componentRef.setInput('park', park);
    fixture.detectChanges();
    const link: HTMLAnchorElement = fixture.nativeElement.querySelector('.ui-park-card__secondary-cta');
    expect(link.textContent?.trim()).toBe('Site web');
    expect(link.getAttribute('aria-label')).toBe('Site web — Example Park');
    expect(link.getAttribute('href')).toBe('https://example.com');
    fixture.componentRef.setInput('park', { ...park, name: 'Another Park' });
    fixture.detectChanges();
    expect(link.getAttribute('aria-label')).toBe('Site web — Another Park');
  });
});
