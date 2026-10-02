import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { HomeFeaturedParkCardModel } from '@app/models/home/home-featured-park-card.model';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { UiFeaturedParkCardComponent } from './ui-featured-park-card.component';

describe('UiFeaturedParkCardComponent logo delivery', () => {
  it('requests variants matching the small logo frame rather than full photo sizes', async () => {
    await TestBed.configureTestingModule({
      imports: [UiFeaturedParkCardComponent], providers: provideCommonTestDependencies()
    }).compileComponents();
    const fixture = TestBed.createComponent(UiFeaturedParkCardComponent);
    const park: HomeFeaturedParkCardModel = {
      id: 'park-1', name: 'Example Park', type: null, typeLabelKey: '', city: null,
      countryCode: null, countryName: null, locationLine: null, logoImageId: 'logo-1',
      description: null, metrics: [], isManualFeatured: true, isSponsoredFeatured: false,
      detailLink: null, tone: 'primary'
    };
    fixture.componentRef.setInput('park', park);
    fixture.detectChanges();
    const image: ImageDisplayComponent = fixture.debugElement.query(By.directive(ImageDisplayComponent)).componentInstance;
    expect(image.srcWidth).toBe(64);
    expect(image.sizes).toBe('57px');
    expect(image.responsiveWidths).toEqual([64, 128, 192]);
  });
});
