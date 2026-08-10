import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';

import { ParkPricingSnapshot } from '@app/models/parks/park-pricing';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AdminParkPricingSnapshotEditorComponent } from './admin-park-pricing-snapshot-editor.component';

describe('AdminParkPricingSnapshotEditorComponent', () => {
  let fixture: ComponentFixture<AdminParkPricingSnapshotEditorComponent>;
  let snapshot: ParkPricingSnapshot;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminParkPricingSnapshotEditorComponent],
      providers: [...provideCommonTestDependencies()],
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('en', {
      adminParkPricing: {
        history: { snapshot: 'Historical record', newSnapshot: 'New record', removeSnapshot: 'Remove', year: 'Year' },
        actions: { addAdmission: 'Add ticket', addAnnualPass: 'Add pass', addParking: 'Add parking', removeOffer: 'Remove offer', copyAllLanguages: 'Copy' },
        sections: { admissionOffers: 'Admission', annualPasses: 'Passes', parkingOffers: 'Parking' },
        fields: {}, channels: {}, modes: {}, kinds: {}, offer: { newOffer: 'New offer' }, placeholders: {}, hints: {},
      },
    });
    translateService.use('en');

    snapshot = {
      year: 2025,
      currencyCode: 'EUR',
      notes: [],
      admissionOffers: [],
      annualPasses: [],
      parkingOffers: [],
    };
    fixture = TestBed.createComponent(AdminParkPricingSnapshotEditorComponent);
    fixture.componentRef.setInput('snapshot', snapshot);
    fixture.detectChanges();
  });

  it('adds a structured historical admission offer', () => {
    let emitted: ParkPricingSnapshot | undefined;
    fixture.componentInstance.snapshotChange.subscribe((value: ParkPricingSnapshot): void => {
      emitted = value;
    });

    clickButton('Add ticket');

    expect(emitted?.currencyCode).toBe('EUR');
    expect(emitted?.admissionOffers).toHaveLength(1);
    expect(emitted?.admissionOffers[0]).toEqual(expect.objectContaining({
      code: 'admission-1',
      audienceCategory: 'adult',
      sortOrder: 1,
    }));
  });

  it('emits the original snapshot with its updated historical currency', () => {
    let emitted: ParkPricingSnapshot | undefined;
    fixture.componentInstance.snapshotChange.subscribe((value: ParkPricingSnapshot): void => {
      emitted = value;
    });
    const currencyInput: HTMLInputElement | null = (fixture.nativeElement as HTMLElement)
      .querySelector('input[maxlength="3"]');

    expect(currencyInput).not.toBeNull();
    if (currencyInput) {
      currencyInput.value = 'HRK';
      currencyInput.dispatchEvent(new Event('input'));
    }

    expect(emitted?.year).toBe(2025);
    expect(emitted?.currencyCode).toBe('HRK');
  });

  function clickButton(label: string): void {
    const button: HTMLButtonElement | undefined = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('button'))
      .find((candidate: HTMLButtonElement): boolean => candidate.textContent?.includes(label) ?? false);
    expect(button).toBeDefined();
    button?.click();
  }
});
