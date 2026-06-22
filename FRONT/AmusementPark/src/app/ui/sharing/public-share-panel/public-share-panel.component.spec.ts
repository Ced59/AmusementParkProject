import { PLATFORM_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { PublicShareQrCodeService } from './public-share-qr-code.service';
import { PublicSharePanelComponent } from './public-share-panel.component';
import { PublicShareTrackingService } from './public-share-tracking.service';

describe('PublicSharePanelComponent', () => {
  let fixture: ComponentFixture<PublicSharePanelComponent>;
  let qrCodeService: jasmine.SpyObj<PublicShareQrCodeService>;
  let trackingService: jasmine.SpyObj<PublicShareTrackingService>;

  beforeEach(async () => {
    qrCodeService = jasmine.createSpyObj<PublicShareQrCodeService>('PublicShareQrCodeService', ['createDataUrl']);
    trackingService = jasmine.createSpyObj<PublicShareTrackingService>('PublicShareTrackingService', ['track']);

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, PublicSharePanelComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: PLATFORM_ID, useValue: 'browser' },
        { provide: PublicShareQrCodeService, useValue: qrCodeService },
        { provide: PublicShareTrackingService, useValue: trackingService }
      ]
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      shareSocial: {
        defaultTitle: 'Partager cette page',
        defaultDescription: 'Tu connais quelqu un qui pourrait utiliser cette page ?',
        defaultText: 'Regarde {{title}} sur AmusementPark.',
        actions: {
          share: 'Partager',
          copy: 'Copier',
          copied: 'Copie',
          linkedin: 'LinkedIn',
          facebook: 'Facebook',
          x: 'X',
          reddit: 'Reddit',
          more: 'Plus',
          less: 'Moins',
          email: 'Email',
          whatsapp: 'WhatsApp',
          telegram: 'Telegram',
          qrCode: 'QR code'
        },
        qr: {
          title: 'Partager via QR code',
          loading: 'Generation du QR code',
          alt: 'QR code pour {{title}}',
          error: 'Impossible de creer le QR code pour le moment.'
        }
      }
    });
    translateService.use('fr');

    fixture = TestBed.createComponent(PublicSharePanelComponent);
    fixture.componentInstance.targetType = 'Park';
    fixture.componentInstance.targetId = 'park-1';
    fixture.componentInstance.targetTitle = 'Mirapolis';
    fixture.detectChanges();
  });

  it('renders the generated QR code when the QR action succeeds', async () => {
    qrCodeService.createDataUrl.and.resolveTo('data:image/png;base64,qr');

    clickButton('Plus');
    fixture.detectChanges();

    clickButton('QR code');
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const image: HTMLImageElement | null = host.querySelector('.public-share-panel__qr img');

    expect(qrCodeService.createDataUrl).toHaveBeenCalled();
    expect(image?.getAttribute('src')).toBe('data:image/png;base64,qr');
    expect(image?.getAttribute('alt')).toBe('QR code pour Mirapolis');
    expect(host.textContent ?? '').not.toContain('Impossible de creer le QR code');
    expect(trackingService.track).toHaveBeenCalledWith(jasmine.objectContaining({
      channel: 'QrCode',
      targetId: 'park-1',
      targetTitle: 'Mirapolis'
    }));
  });

  it('shows an error instead of an empty QR panel when generation fails', async () => {
    qrCodeService.createDataUrl.and.rejectWith(new Error('QR generation failed'));

    clickButton('Plus');
    fixture.detectChanges();

    clickButton('QR code');
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;

    expect(host.querySelector('.public-share-panel__qr img')).toBeNull();
    expect(host.textContent ?? '').toContain('Impossible de creer le QR code pour le moment.');
    expect(trackingService.track).not.toHaveBeenCalled();
  });

  function clickButton(text: string): void {
    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const buttons: HTMLButtonElement[] = Array.from(host.querySelectorAll('button'));
    const button: HTMLButtonElement | undefined = buttons.find((candidate: HTMLButtonElement): boolean => {
      return (candidate.textContent ?? '').includes(text);
    });

    if (!button) {
      throw new Error(`Button with text "${text}" was not found.`);
    }

    button.click();
  }
});
