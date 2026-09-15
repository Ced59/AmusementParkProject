import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { ParkFitTrustNoticeComponent } from './park-fit-trust-notice.component';

describe('ParkFitTrustNoticeComponent', () => {
  let fixture: ComponentFixture<ParkFitTrustNoticeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, ParkFitTrustNoticeComponent],
      providers: [...provideCommonTestDependencies()]
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setDefaultLang('fr');
    translateService.setTranslation('fr', {
      parkFit: {
        trust: {
          title: 'Une boussole, pas une promesse',
          independent: 'Aucun partenariat ni paiement ne modifie l’ordre.',
          official: 'Vérifie les règles sur le site officiel du parc.'
        }
      }
    });
    translateService.use('fr');

    fixture = TestBed.createComponent(ParkFitTrustNoticeComponent);
    fixture.detectChanges();
  });

  it('states the independent ordering and official-verification safeguards', () => {
    const notice: HTMLElement | null = fixture.nativeElement.querySelector('[role="note"]');

    expect(notice?.textContent).toContain('Une boussole, pas une promesse');
    expect(notice?.textContent).toContain('Aucun partenariat ni paiement');
    expect(notice?.textContent).toContain('site officiel du parc');
  });
});
