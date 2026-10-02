import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal, WritableSignal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { CookieConsentService } from '@core/privacy/cookie-consent.service';
import { TranslationService } from '@app/services/translation.service';
import { CookieConsentBannerComponent } from './cookie-consent-banner.component';

describe('CookieConsentBannerComponent', () => {
  let fixture: ComponentFixture<CookieConsentBannerComponent>;
  let bannerVisible: WritableSignal<boolean>;
  let consentRequired: boolean;
  let acceptOptionalCookies: ReturnType<typeof vi.fn>;
  let continueWithNecessaryCookiesOnly: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    bannerVisible = signal<boolean>(true);
    consentRequired = true;
    acceptOptionalCookies = vi.fn();
    continueWithNecessaryCookiesOnly = vi.fn();
    await TestBed.configureTestingModule({
      imports: [CookieConsentBannerComponent, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        { provide: TranslationService, useValue: { getCurrentLang: (): string => 'fr' } },
        {
          provide: CookieConsentService,
          useValue: {
            get isConsentRequired(): boolean { return consentRequired; },
            isBannerVisible: bannerVisible,
            acceptOptionalCookies,
            continueWithNecessaryCookiesOnly
          }
        }
      ]
    }).compileComponents();
  });

  it('includes consent in the initial render even before restoring a previous visitor choice', () => {
    bannerVisible.set(false);
    fixture = TestBed.createComponent(CookieConsentBannerComponent);
    const component = fixture.componentInstance as unknown as { isVisible: () => boolean };

    expect(component.isVisible()).toBe(true);
  });

  it('keeps the first-visit banner after hydration and both consent actions work', async () => {
    fixture = TestBed.createComponent(CookieConsentBannerComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const banner: HTMLElement = fixture.nativeElement.querySelector('.app-cookie-consent');
    expect(banner).not.toBeNull();
    const buttons: NodeListOf<HTMLButtonElement> = banner.querySelectorAll('button');
    buttons[0].click();
    buttons[1].click();
    expect(acceptOptionalCookies).toHaveBeenCalledOnce();
    expect(continueWithNecessaryCookiesOnly).toHaveBeenCalledOnce();
    expect(banner.querySelector('a')?.getAttribute('href')).toBe('/fr/privacy');
  });

  it('removes the initial banner after hydration when a choice already exists', async () => {
    bannerVisible.set(false);
    fixture = TestBed.createComponent(CookieConsentBannerComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.app-cookie-consent')).toBeNull();
  });

  it('does not render a banner when optional-cookie consent is disabled', async () => {
    consentRequired = false;
    bannerVisible.set(false);
    fixture = TestBed.createComponent(CookieConsentBannerComponent);
    const component = fixture.componentInstance as unknown as { isVisible: () => boolean };
    expect(component.isVisible()).toBe(false);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.app-cookie-consent')).toBeNull();
  });
});
