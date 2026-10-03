import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { AccountDeletionRequest } from '@app/models/users/account-deletion-request.model';
import { ACCOUNT_DELETION_DATA_PORT } from './account-deletion-data.port';

@Injectable()
export class AccountDeletionStateFacade {
  private readonly dataPort = inject(ACCOUNT_DELETION_DATA_PORT);
  private readonly destroyRef = inject(DestroyRef);
  private readonly submittingState = signal<boolean>(false);
  private readonly completedState = signal<boolean>(false);
  private readonly errorKeyState = signal<string | null>(null);

  public readonly submitting = this.submittingState.asReadonly();
  public readonly completed = this.completedState.asReadonly();
  public readonly errorKey = this.errorKeyState.asReadonly();

  public request(request: AccountDeletionRequest): void {
    if (this.submittingState()) {
      return;
    }

    this.submittingState.set(true);
    this.completedState.set(false);
    this.errorKeyState.set(null);
    this.dataPort.deleteCurrentAccount(request).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.submittingState.set(false))
    ).subscribe({
      next: () => this.completedState.set(true),
      error: () => this.errorKeyState.set('accountDeletion.errors.request')
    });
  }

  public setValidationError(errorKey: string): void {
    this.errorKeyState.set(errorKey);
  }
}
