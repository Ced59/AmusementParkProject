import { Injectable } from '@angular/core';
import type { QRCodeToDataURLOptions } from 'qrcode';

export type PublicShareQrCodeToDataUrl = (text: string, options?: QRCodeToDataURLOptions) => Promise<string>;

export interface PublicShareQrCodeModule {
  readonly toDataURL?: unknown;
  readonly default?: PublicShareQrCodeModule | null;
}

const qrCodeOptions: QRCodeToDataURLOptions = {
  width: 240,
  margin: 2,
  errorCorrectionLevel: 'M',
  color: {
    dark: '#0f172a',
    light: '#ffffff'
  }
};

@Injectable({
  providedIn: 'root'
})
export class PublicShareQrCodeService {
  async createDataUrl(url: string): Promise<string> {
    const qrCodeModule: PublicShareQrCodeModule = await import('qrcode') as unknown as PublicShareQrCodeModule;
    const toDataURL: PublicShareQrCodeToDataUrl = resolveQrCodeToDataUrl(qrCodeModule);

    return toDataURL(url, qrCodeOptions);
  }
}

export function resolveQrCodeToDataUrl(qrCodeModule: PublicShareQrCodeModule): PublicShareQrCodeToDataUrl {
  const namedExport: PublicShareQrCodeToDataUrl | null = resolveQrCodeFunction(qrCodeModule, qrCodeModule.toDataURL);

  if (namedExport) {
    return namedExport;
  }

  const defaultExport: PublicShareQrCodeModule | null | undefined = qrCodeModule.default;

  if (defaultExport) {
    const defaultToDataUrl: PublicShareQrCodeToDataUrl | null = resolveQrCodeFunction(defaultExport, defaultExport.toDataURL);

    if (defaultToDataUrl) {
      return defaultToDataUrl;
    }
  }

  throw new Error('QR code generator is not available.');
}

function resolveQrCodeFunction(owner: PublicShareQrCodeModule, candidate: unknown): PublicShareQrCodeToDataUrl | null {
  if (typeof candidate !== 'function') {
    return null;
  }

  return candidate.bind(owner) as PublicShareQrCodeToDataUrl;
}
