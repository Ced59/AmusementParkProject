import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';

import { ParkAdmissionPriceOffer } from '@app/models/parks/park-pricing';
import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies,
} from '@app/testing/common-test-providers';
import {
  AdminParkPricingOffer,
  AdminParkPricingOfferEditorComponent,
} from './admin-park-pricing-offer-editor.component';

describe('AdminParkPricingOfferEditorComponent', () => {
  let fixture: ComponentFixture<AdminParkPricingOfferEditorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminParkPricingOfferEditorComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('en', {
      adminParkPricing: {
        actions: { removeOffer: 'Remove', copyAllLanguages: 'Copy' },
        channels: { online: 'Online', gate: 'Gate' },
        fields: {}, hints: { dynamicBounds: 'Optional bounds' }, kinds: {},
        modes: { none: 'None', Fixed: 'Fixed', Range: 'Range', Dynamic: 'Dynamic' },
        offer: { newOffer: 'New offer' }, placeholders: {},
      },
    });
    translateService.use('en');

    fixture = TestBed.createComponent(AdminParkPricingOfferEditorComponent);
    fixture.componentRef.setInput('kind', 'admission');
    fixture.componentRef.setInput('offer', createOffer());
    fixture.componentInstance.offerChange.subscribe((offer: AdminParkPricingOffer): void => {
      fixture.componentRef.setInput('offer', offer);
    });
    fixture.detectChanges();
  });

  it('shows the fields adapted to fixed, range and dynamic modes', () => {
    expect(priceInputs()).toHaveLength(1);

    const onlineMode: HTMLSelectElement = (fixture.nativeElement as HTMLElement)
      .querySelectorAll<HTMLSelectElement>('select')[0];
    onlineMode.value = 'Range';
    onlineMode.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(priceInputs()).toHaveLength(2);

    onlineMode.value = 'Dynamic';
    onlineMode.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(priceInputs()).toHaveLength(2);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Optional bounds');
  });

  function priceInputs(): HTMLInputElement[] {
    return Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLInputElement>('input[step="0.01"]'),
    );
  }
});

function createOffer(): ParkAdmissionPriceOffer {
  return {
    code: 'adult',
    audienceCategory: 'adult',
    labels: [],
    onlinePrice: { mode: 'Fixed', amount: 49 },
    gatePrice: null,
    validFrom: null,
    validTo: null,
    purchaseUrl: null,
    conditions: [],
    sortOrder: 0,
  };
}
