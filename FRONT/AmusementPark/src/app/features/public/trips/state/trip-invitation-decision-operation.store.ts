import { Injectable } from '@angular/core';

interface StoredTripInvitationDecisionOperation {
  readonly decision: 'accept' | 'decline';
  readonly operationId: string;
}

@Injectable({ providedIn: 'root' })
export class TripInvitationDecisionOperationStore {
  private readonly keyPrefix: string = 'amusementpark.trip-invitation-decision.v1.';

  read(token: string): StoredTripInvitationDecisionOperation | null {
    const storage: Storage | null = this.getStorage();
    if (!storage) {
      return null;
    }

    try {
      const value: unknown = JSON.parse(storage.getItem(this.buildKey(token)) ?? 'null');
      if (!this.isStoredOperation(value)) {
        return null;
      }

      return value;
    } catch {
      this.clear(token);
      return null;
    }
  }

  write(token: string, decision: 'accept' | 'decline', operationId: string): void {
    const storage: Storage | null = this.getStorage();
    if (!storage) {
      return;
    }

    try {
      storage.setItem(this.buildKey(token), JSON.stringify({ decision, operationId }));
    } catch {
      // A disabled or full session storage must not prevent the decision request.
    }
  }

  clear(token: string): void {
    try {
      this.getStorage()?.removeItem(this.buildKey(token));
    } catch {
      // Best-effort cleanup only.
    }
  }

  private buildKey(token: string): string {
    let firstHash: number = 0x811c9dc5;
    let secondHash: number = 0x9e3779b9;
    for (let index: number = 0; index < token.length; index++) {
      const code: number = token.charCodeAt(index);
      firstHash = Math.imul(firstHash ^ code, 0x01000193);
      secondHash = Math.imul(secondHash ^ code, 0x85ebca6b);
    }

    return `${this.keyPrefix}${(firstHash >>> 0).toString(16)}${(secondHash >>> 0).toString(16)}`;
  }

  private isStoredOperation(value: unknown): value is StoredTripInvitationDecisionOperation {
    if (!value || typeof value !== 'object') {
      return false;
    }

    const candidate: Partial<StoredTripInvitationDecisionOperation> = value;
    return (candidate.decision === 'accept' || candidate.decision === 'decline')
      && typeof candidate.operationId === 'string'
      && candidate.operationId.length > 0
      && candidate.operationId.length <= 200;
  }

  private getStorage(): Storage | null {
    return typeof sessionStorage === 'undefined' ? null : sessionStorage;
  }
}
