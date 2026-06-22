import { TestBed } from '@angular/core/testing';

import {
  PublicShareQrCodeService,
  PublicShareQrCodeToDataUrl,
  resolveQrCodeToDataUrl
} from './public-share-qr-code.service';

describe('PublicShareQrCodeService', () => {
  it('creates a browser-readable QR code data URL', async () => {
    const service: PublicShareQrCodeService = TestBed.inject(PublicShareQrCodeService);

    const dataUrl: string = await service.createDataUrl('https://amusement-parks.fun/fr/parcs/mirapolis');

    expect(dataUrl).toMatch(/^data:image\/png;base64,/);
  });

  it('resolves a named qrcode export', async () => {
    const toDataURL: jasmine.Spy<PublicShareQrCodeToDataUrl> = jasmine.createSpy('toDataURL').and.resolveTo('data:image/png;base64,named');
    const resolvedToDataUrl: PublicShareQrCodeToDataUrl = resolveQrCodeToDataUrl({ toDataURL });

    await expectAsync(resolvedToDataUrl('https://amusement-parks.fun')).toBeResolvedTo('data:image/png;base64,named');
  });

  it('resolves a default qrcode export', async () => {
    const toDataURL: jasmine.Spy<PublicShareQrCodeToDataUrl> = jasmine.createSpy('toDataURL').and.resolveTo('data:image/png;base64,default');
    const resolvedToDataUrl: PublicShareQrCodeToDataUrl = resolveQrCodeToDataUrl({ default: { toDataURL } });

    await expectAsync(resolvedToDataUrl('https://amusement-parks.fun')).toBeResolvedTo('data:image/png;base64,default');
  });
});
