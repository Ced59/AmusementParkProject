import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, Input, PLATFORM_ID, signal } from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import {
  SocialShareChannel,
  SocialShareTargetType
} from '@app/models/social-share/social-share.models';
import { UiSurfaceDirective } from '@ui/primitives';
import { PublicShareQrCodeService } from './public-share-qr-code.service';
import { PublicShareTrackingService } from './public-share-tracking.service';

type PrimaryNetwork = 'LinkedIn' | 'Facebook' | 'X' | 'Reddit';
type SecondaryNetwork = 'Email' | 'WhatsApp' | 'Telegram';

interface ShareAction<TChannel extends SocialShareChannel = SocialShareChannel> {
  readonly channel: TChannel;
  readonly iconClass: string;
  readonly labelKey: string;
}

@Component({
  selector: 'app-public-share-panel',
  templateUrl: './public-share-panel.component.html',
  styleUrl: './public-share-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    TranslateModule,
    UiSurfaceDirective
  ]
})
export class PublicSharePanelComponent {
  @Input() targetType: SocialShareTargetType = 'Page';
  @Input() targetId: string | null = null;
  @Input() targetTitle: string | null = null;
  @Input() titleKey: string = 'shareSocial.defaultTitle';
  @Input() descriptionKey: string = 'shareSocial.defaultDescription';
  @Input() textKey: string = 'shareSocial.defaultText';
  @Input() textParams: Record<string, string | number | null | undefined> = {};

  protected readonly showMore = signal(false);
  protected readonly showQr = signal(false);
  protected readonly qrCodeDataUrl = signal<string | null>(null);
  protected readonly qrCodeError = signal(false);
  protected readonly qrCodeLoading = signal(false);
  protected readonly copyConfirmed = signal(false);

  protected readonly primaryNetworks: readonly ShareAction<PrimaryNetwork>[] = [
    { channel: 'LinkedIn', iconClass: 'pi pi-linkedin', labelKey: 'shareSocial.actions.linkedin' },
    { channel: 'Facebook', iconClass: 'pi pi-facebook', labelKey: 'shareSocial.actions.facebook' },
    { channel: 'X', iconClass: 'pi pi-twitter', labelKey: 'shareSocial.actions.x' },
    { channel: 'Reddit', iconClass: 'pi pi-reddit', labelKey: 'shareSocial.actions.reddit' }
  ];

  protected readonly secondaryNetworks: readonly ShareAction<SecondaryNetwork>[] = [
    { channel: 'Email', iconClass: 'pi pi-envelope', labelKey: 'shareSocial.actions.email' },
    { channel: 'WhatsApp', iconClass: 'pi pi-whatsapp', labelKey: 'shareSocial.actions.whatsapp' },
    { channel: 'Telegram', iconClass: 'pi pi-send', labelKey: 'shareSocial.actions.telegram' }
  ];

  private readonly isBrowser: boolean;
  private copyResetTimer: number | null = null;

  constructor(
    @Inject(DOCUMENT) private readonly document: Document,
    @Inject(PLATFORM_ID) platformId: object,
    private readonly translateService: TranslateService,
    private readonly qrCodeService: PublicShareQrCodeService,
    private readonly trackingService: PublicShareTrackingService
  ) {
    this.isBrowser = isPlatformBrowser(platformId);
  }

  protected get canUseNativeShare(): boolean {
    return this.isBrowser && typeof navigator.share === 'function';
  }

  protected get shareUrl(): string {
    if (!this.isBrowser) {
      return '';
    }

    const url: URL = new URL(this.document.location.href);
    url.hash = '';
    return url.toString();
  }

  protected get resolvedTargetTitle(): string {
    const inputTitle: string = this.targetTitle?.trim() ?? '';

    if (inputTitle.length > 0) {
      return inputTitle;
    }

    if (this.isBrowser && this.document.title.trim().length > 0) {
      return this.document.title.trim();
    }

    return 'AmusementPark';
  }

  protected toggleMore(): void {
    this.showMore.update((value: boolean) => !value);
  }

  protected async shareNative(): Promise<void> {
    if (!this.canUseNativeShare || this.shareUrl.length === 0) {
      return;
    }

    try {
      await navigator.share({
        title: this.resolvedTargetTitle,
        text: this.resolveShareText(),
        url: this.shareUrl
      });

      this.track('Native');
    } catch {
      return;
    }
  }

  protected async copyLink(): Promise<void> {
    if (!this.isBrowser || this.shareUrl.length === 0) {
      return;
    }

    try {
      await this.writeToClipboard(this.shareUrl);
      this.copyConfirmed.set(true);
      this.track('Copy');

      if (this.copyResetTimer !== null) {
        window.clearTimeout(this.copyResetTimer);
      }

      this.copyResetTimer = window.setTimeout(() => this.copyConfirmed.set(false), 2200);
    } catch {
      this.copyConfirmed.set(false);
    }
  }

  protected async toggleQrCode(): Promise<void> {
    const nextVisible: boolean = !this.showQr();
    this.showQr.set(nextVisible);
    const url: string = this.shareUrl;

    if (!nextVisible) {
      return;
    }

    if (!this.isBrowser || url.length === 0) {
      this.qrCodeError.set(true);
      return;
    }

    if (!this.qrCodeDataUrl()) {
      this.qrCodeError.set(false);
      this.qrCodeLoading.set(true);

      try {
        const qrDataUrl: string = await this.qrCodeService.createDataUrl(url);
        this.qrCodeDataUrl.set(qrDataUrl);
      } catch {
        this.qrCodeDataUrl.set(null);
        this.qrCodeError.set(true);
        return;
      } finally {
        this.qrCodeLoading.set(false);
      }
    }

    this.track('QrCode');
  }

  protected buildShareHref(channel: SocialShareChannel): string {
    const url: string = this.shareUrl;
    const title: string = this.resolvedTargetTitle;
    const text: string = this.resolveShareText();

    if (!url) {
      return '#';
    }

    if (channel === 'Email') {
      return `mailto:?subject=${encodeURIComponent(title)}&body=${encodeURIComponent(`${text}\n${url}`)}`;
    }

    if (channel === 'WhatsApp') {
      return `https://wa.me/?text=${encodeURIComponent(`${text} ${url}`)}`;
    }

    if (channel === 'Telegram') {
      return `https://t.me/share/url?url=${encodeURIComponent(url)}&text=${encodeURIComponent(text)}`;
    }

    if (channel === 'LinkedIn') {
      return `https://www.linkedin.com/sharing/share-offsite/?url=${encodeURIComponent(url)}`;
    }

    if (channel === 'Facebook') {
      return `https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(url)}`;
    }

    if (channel === 'X') {
      return `https://twitter.com/intent/tweet?url=${encodeURIComponent(url)}&text=${encodeURIComponent(text)}`;
    }

    if (channel === 'Reddit') {
      return `https://www.reddit.com/submit?url=${encodeURIComponent(url)}&title=${encodeURIComponent(title)}`;
    }

    return '#';
  }

  protected trackLink(channel: PrimaryNetwork | SecondaryNetwork): void {
    this.track(channel);
  }

  private resolveShareText(): string {
    const params: Record<string, string | number | null | undefined> = {
      title: this.resolvedTargetTitle,
      ...this.textParams
    };
    const translatedText: string = this.translateService.instant(this.textKey, params) as string;

    if (translatedText && translatedText !== this.textKey) {
      return translatedText;
    }

    return this.resolvedTargetTitle;
  }

  private async writeToClipboard(value: string): Promise<void> {
    if (navigator.clipboard?.writeText) {
      await navigator.clipboard.writeText(value);
      return;
    }

    const textArea: HTMLTextAreaElement = this.document.createElement('textarea');
    textArea.value = value;
    textArea.setAttribute('readonly', 'true');
    textArea.style.position = 'fixed';
    textArea.style.left = '-9999px';
    this.document.body.appendChild(textArea);
    textArea.select();
    this.document.execCommand('copy');
    this.document.body.removeChild(textArea);
  }

  private track(channel: SocialShareChannel): void {
    if (!this.isBrowser || this.shareUrl.length === 0) {
      return;
    }

    this.trackingService.track({
      targetType: this.targetType,
      targetId: this.normalizeOptional(this.targetId),
      targetTitle: this.normalizeOptional(this.resolvedTargetTitle),
      url: this.shareUrl,
      languageCode: this.normalizeOptional(this.translateService.currentLang),
      channel
    });
  }

  private normalizeOptional(value: string | null | undefined): string | null {
    const normalizedValue: string = value?.trim() ?? '';
    return normalizedValue.length > 0 ? normalizedValue : null;
  }
}
