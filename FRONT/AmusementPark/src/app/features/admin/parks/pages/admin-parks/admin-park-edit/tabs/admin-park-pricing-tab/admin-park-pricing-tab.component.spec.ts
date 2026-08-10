import type { MockedObject } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

import { ParkPricing } from '@app/models/parks/park-pricing';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { AdminParkEditStateFacade } from '@features/admin/parks/state/admin-park-edit-state.facade';
import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies,
} from '@app/testing/common-test-providers';
import { AdminParkPricingTabComponent } from './admin-park-pricing-tab.component';

describe('AdminParkPricingTabComponent', () => {
  let fixture: ComponentFixture<AdminParkPricingTabComponent>;
  let facade: MockedObject<AdminParkEditStateFacade>;

  beforeEach(async () => {
    facade = {
      pricingLoading: signal(false),
      pricingSaving: signal(false),
      loadPricing: vi.fn().mockResolvedValue(createPricing()),
      savePricing: vi.fn().mockResolvedValue(createPricing()),
    } as unknown as MockedObject<AdminParkEditStateFacade>;

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminParkPricingTabComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: AdminParkEditStateFacade, useValue: facade },
        {
          provide: ToastMessageService,
          useValue: { add: vi.fn() },
        },
      ],
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('en', {
      adminParkPricing: {
        title: 'Park pricing', subtitle: 'Pricing editor', tab: 'Pricing',
        actions: {
          reload: 'Reload', save: 'Save', removeOffer: 'Remove',
          addAdmission: 'Add ticket', addAnnualPass: 'Add pass',
          addParking: 'Add parking', copyAllLanguages: 'Copy',
        },
        sections: {
          general: 'General', admissionOffers: 'Admission',
          annualPasses: 'Passes', parkingOffers: 'Parking',
        },
        fields: {}, kinds: {}, offer: { newOffer: 'New offer' },
        channels: {}, modes: {}, placeholders: {},
        hints: { admissionOffers: '', annualPasses: '', parkingOffers: '' },
        empty: {
          admissionOffers: 'No tickets', annualPasses: 'No passes',
          parkingOffers: 'No parking',
        },
        messages: { loading: 'Loading', savedSummary: 'Saved', savedDetail: 'Saved' },
      },
    });
    translateService.use('en');

    fixture = TestBed.createComponent(AdminParkPricingTabComponent);
    fixture.componentRef.setInput('parkId', 'park-1');
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('loads pricing through the edit state facade', () => {
    expect(facade.loadPricing).toHaveBeenCalledWith('park-1');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No tickets');
  });

  it('adds and removes an admission offer from the structured editor', () => {
    clickButton('Add ticket');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('app-admin-park-pricing-offer-editor')).toHaveLength(1);

    clickButton('Remove');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('app-admin-park-pricing-offer-editor')).toHaveLength(0);
  });

  it('keeps an unsaved offer editor mounted while its code changes', () => {
    clickButton('Add ticket');
    fixture.detectChanges();

    const editorBefore: HTMLElement | null = (fixture.nativeElement as HTMLElement)
      .querySelector('app-admin-park-pricing-offer-editor');
    const codeInput: HTMLInputElement | null = editorBefore?.querySelector(
      '.pricing-offer-editor__fields--identity input[type="text"]',
    ) ?? null;
    expect(editorBefore).not.toBeNull();
    expect(codeInput).not.toBeNull();

    if (codeInput) {
      codeInput.value = 'adult-updated';
      codeInput.dispatchEvent(new Event('input'));
    }
    fixture.detectChanges();

    const editorAfter: HTMLElement | null = (fixture.nativeElement as HTMLElement)
      .querySelector('app-admin-park-pricing-offer-editor');
    expect(editorAfter).toBe(editorBefore);
    expect(editorAfter?.textContent).toContain('adult-updated');
  });

  function clickButton(label: string): void {
    const buttons: HTMLButtonElement[] = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('button'),
    );
    const button: HTMLButtonElement | undefined = buttons.find(
      (candidate: HTMLButtonElement): boolean => candidate.textContent?.includes(label) ?? false,
    );
    expect(button).toBeDefined();
    button?.click();
  }
});

function createPricing(): ParkPricing {
  return {
    parkId: 'park-1',
    currencyCode: 'EUR',
    notes: [],
    admissionOffers: [],
    annualPasses: [],
    parkingOffers: [],
  };
}
