import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { ParkDataEditorTokenApiService } from '@data-access/users/park-data-editor-token-api.service';
import { CreatedParkDataEditorTokenResponse } from '@app/models/users/created-park-data-editor-token-response';
import { ParkDataEditorToken } from '@app/models/users/user-admin-responses';
import { UiButtonDirective, UiSurfaceDirective } from '@ui/primitives';
import { encryptParkEditorToken, importParkEditorRecipientKey } from './park-data-editor-token-crypto';

@Component({
  selector: 'app-park-data-editor-token-handoff',
  templateUrl: './park-data-editor-token-handoff.component.html',
  styleUrl: './park-data-editor-token-handoff.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, TranslateModule, UiButtonDirective, UiSurfaceDirective]
})
export class ParkDataEditorTokenHandoffComponent {
  private readonly tokenApi: ParkDataEditorTokenApiService = inject(ParkDataEditorTokenApiService);
  private readonly translate: TranslateService = inject(TranslateService);

  protected publicKeyPem: string = '';
  protected readonly busy = signal(false);
  protected readonly encryptedPackage = signal<string | null>(null);
  protected readonly errorKey = signal<string | null>(null);
  protected readonly copied = signal(false);
  protected readonly activeTokens = signal<ParkDataEditorToken[]>([]);
  protected readonly tokensVisible = signal(false);
  protected readonly loadingTokens = signal(false);
  protected readonly revokingTokenId = signal<string | null>(null);

  protected updatePublicKey(event: Event): void {
    this.publicKeyPem = (event.target as HTMLTextAreaElement).value;
    this.encryptedPackage.set(null);
    this.errorKey.set(null);
    this.copied.set(false);
  }

  protected async createPackage(): Promise<void> {
    if (this.busy() || this.revokingTokenId()) {
      return;
    }

    this.encryptedPackage.set(null);
    this.copied.set(false);
    this.errorKey.set(null);

    let recipientKey: CryptoKey;
    let subtle: SubtleCrypto;
    try {
      subtle = globalThis.crypto.subtle;
      recipientKey = await importParkEditorRecipientKey(this.publicKeyPem, subtle);
    } catch {
      this.errorKey.set('parkEditorAccess.invalidKey');
      return;
    }

    this.busy.set(true);
    let created: CreatedParkDataEditorTokenResponse | null = null;
    try {
      created = await firstValueFrom(this.tokenApi.createToken());
      const encrypted: string = await encryptParkEditorToken(created.plainTextToken, recipientKey, subtle);
      this.encryptedPackage.set(encrypted);
    } catch (error: unknown) {
      if (created?.token?.id) {
        try {
          await firstValueFrom(this.tokenApi.revoke(created.token.id));
        } catch {
          this.errorKey.set('parkEditorAccess.revokeFailed');
        }
      }
      this.errorKey.set(this.errorKey() ?? (created
        ? 'parkEditorAccess.encryptionFailed'
        : this.createErrorKey(error)));
      if (this.errorKey() === 'parkEditorAccess.activeLimit') {
        void this.loadTokens();
      }
    } finally {
      created = null;
      this.busy.set(false);
    }
  }

  protected async copyPackage(): Promise<void> {
    const encrypted: string | null = this.encryptedPackage();
    if (!encrypted) {
      return;
    }

    try {
      await globalThis.navigator.clipboard.writeText(encrypted);
      this.copied.set(true);
    } catch {
      this.errorKey.set('parkEditorAccess.copyFailed');
    }
  }

  protected async loadTokens(): Promise<void> {
    if (this.loadingTokens()) {
      return;
    }
    this.tokensVisible.set(true);
    this.loadingTokens.set(true);
    try {
      const tokens: ParkDataEditorToken[] = await firstValueFrom(this.tokenApi.listTokens());
      this.activeTokens.set(tokens.filter((token: ParkDataEditorToken) => token.isActive));
    } catch {
      this.errorKey.set(this.errorKey() ?? 'parkEditorAccess.tokensLoadFailed');
    } finally {
      this.loadingTokens.set(false);
    }
  }

  protected async revokeToken(tokenId: string): Promise<void> {
    if (this.busy() || this.revokingTokenId()
      || !this.activeTokens().some((token: ParkDataEditorToken) => token.id === tokenId)
      || !globalThis.confirm(this.translate.instant('parkEditorAccess.confirmRevoke'))) {
      return;
    }
    this.revokingTokenId.set(tokenId);
    try {
      await firstValueFrom(this.tokenApi.revoke(tokenId));
      this.activeTokens.update((tokens: ParkDataEditorToken[]) => tokens.filter((token: ParkDataEditorToken) => token.id !== tokenId));
      this.errorKey.set(null);
    } catch {
      this.errorKey.set('parkEditorAccess.tokenRevokeFailed');
    } finally {
      this.revokingTokenId.set(null);
    }
  }

  private createErrorKey(error: unknown): string {
    if (!(error instanceof HttpErrorResponse)) {
      return 'parkEditorAccess.createFailed';
    }
    const code: unknown = error.error?.errorCode;
    if (code === 'park-data-editor-token.active-limit-reached') {
      return 'parkEditorAccess.activeLimit';
    }
    if (error.status === 401) {
      return 'parkEditorAccess.sessionExpired';
    }
    if (error.status === 403) {
      return 'parkEditorAccess.roleRequired';
    }
    if (error.status === 429) {
      return 'parkEditorAccess.rateLimited';
    }
    if (error.status === 0) {
      return 'parkEditorAccess.networkFailed';
    }
    return 'parkEditorAccess.createFailed';
  }
}
