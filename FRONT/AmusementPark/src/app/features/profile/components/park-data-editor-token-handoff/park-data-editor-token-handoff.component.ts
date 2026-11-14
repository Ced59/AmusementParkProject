import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';

import { ParkDataEditorTokenApiService } from '@data-access/users/park-data-editor-token-api.service';
import { CreatedParkDataEditorTokenResponse } from '@app/models/users/created-park-data-editor-token-response';
import { UiButtonDirective, UiSurfaceDirective } from '@ui/primitives';
import { encryptParkEditorToken, importParkEditorRecipientKey } from './park-data-editor-token-crypto';

@Component({
  selector: 'app-park-data-editor-token-handoff',
  templateUrl: './park-data-editor-token-handoff.component.html',
  styleUrl: './park-data-editor-token-handoff.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule, UiButtonDirective, UiSurfaceDirective]
})
export class ParkDataEditorTokenHandoffComponent {
  private readonly tokenApi: ParkDataEditorTokenApiService = inject(ParkDataEditorTokenApiService);

  protected publicKeyPem: string = '';
  protected readonly busy = signal(false);
  protected readonly encryptedPackage = signal<string | null>(null);
  protected readonly errorKey = signal<string | null>(null);
  protected readonly copied = signal(false);

  protected updatePublicKey(event: Event): void {
    this.publicKeyPem = (event.target as HTMLTextAreaElement).value;
    this.encryptedPackage.set(null);
    this.errorKey.set(null);
    this.copied.set(false);
  }

  protected async createPackage(): Promise<void> {
    if (this.busy()) {
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
    } catch {
      if (created?.token?.id) {
        try {
          await firstValueFrom(this.tokenApi.revoke(created.token.id));
        } catch {
          this.errorKey.set('parkEditorAccess.revokeFailed');
        }
      }
      this.errorKey.set(this.errorKey() ?? 'parkEditorAccess.createFailed');
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
}
