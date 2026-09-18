import { Injectable } from '@angular/core';

interface StoredTripInvitationDecisionOperation {
  readonly decision: 'accept' | 'decline';
  readonly operationId: string;
  readonly subjectFingerprint: string;
}

@Injectable({ providedIn: 'root' })
export class TripInvitationDecisionOperationStore {
  private readonly keyPrefix: string = 'amusementpark.trip-invitation-decision.v2.';

  read(token: string, userId: string): StoredTripInvitationDecisionOperation | null {
    const storage: Storage | null = this.getStorage();
    if (!storage) {
      return null;
    }

    try {
      const value: unknown = JSON.parse(storage.getItem(this.buildKey(token, userId)) ?? 'null');
      if (!this.isStoredOperation(value)
        || value.subjectFingerprint !== this.fingerprint(userId)) {
        return null;
      }

      return value;
    } catch {
      this.clear(token, userId);
      return null;
    }
  }

  write(
    token: string,
    userId: string,
    decision: 'accept' | 'decline',
    operationId: string
  ): void {
    const storage: Storage | null = this.getStorage();
    if (!storage) {
      return;
    }

    try {
      storage.setItem(this.buildKey(token, userId), JSON.stringify({
        decision,
        operationId,
        subjectFingerprint: this.fingerprint(userId)
      }));
    } catch {
      // A disabled or full session storage must not prevent the decision request.
    }
  }

  clear(token: string, userId: string): void {
    try {
      this.getStorage()?.removeItem(this.buildKey(token, userId));
    } catch {
      // Best-effort cleanup only.
    }
  }

  private buildKey(token: string, userId: string): string {
    return `${this.keyPrefix}${this.fingerprint(token)}.${this.fingerprint(userId)}`;
  }

  private fingerprint(value: string): string {
    let firstHash: number = 0x811c9dc5;
    let secondHash: number = 0x9e3779b9;
    for (let index: number = 0; index < value.length; index++) {
      const code: number = value.charCodeAt(index);
      firstHash = Math.imul(firstHash ^ code, 0x01000193);
      secondHash = Math.imul(secondHash ^ code, 0x85ebca6b);
    }

    return `${(firstHash >>> 0).toString(16)}${(secondHash >>> 0).toString(16)}`;
  }

  private isStoredOperation(value: unknown): value is StoredTripInvitationDecisionOperation {
    if (!value || typeof value !== 'object') {
      return false;
    }

    const candidate: Partial<StoredTripInvitationDecisionOperation> = value;
    return (candidate.decision === 'accept' || candidate.decision === 'decline')
      && typeof candidate.operationId === 'string'
      && candidate.operationId.length > 0
      && candidate.operationId.length <= 200
      && typeof candidate.subjectFingerprint === 'string'
      && candidate.subjectFingerprint.length > 0;
  }

  private getStorage(): Storage | null {
    return typeof sessionStorage === 'undefined' ? null : sessionStorage;
  }
}
