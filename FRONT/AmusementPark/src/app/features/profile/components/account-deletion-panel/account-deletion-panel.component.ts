import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, effect, inject, signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { UiFieldInputComponent } from '@ui/forms';
import { UiButtonDirective, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { AccountDeletionStateFacade } from '../../state/account-deletion-state.facade';

@Component({
  selector: 'app-account-deletion-panel',
  templateUrl: './account-deletion-panel.component.html',
  styleUrl: './account-deletion-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AccountDeletionStateFacade],
  imports: [
    TranslateModule,
    UiButtonDirective,
    UiFieldInputComponent,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class AccountDeletionPanelComponent {
  private readonly facade = inject(AccountDeletionStateFacade);
  protected readonly expanded = signal<boolean>(false);
  protected confirmationEmail: string = '';
  protected currentPassword: string = '';
  protected readonly submitting = this.facade.submitting;
  protected readonly errorKey = this.facade.errorKey;

  @Input() accountEmail: string = '';
  @Output() deletionAccepted: EventEmitter<void> = new EventEmitter<void>();

  constructor() {
    effect((): void => {
      if (this.facade.completed()) {
        this.deletionAccepted.emit();
      }
    });
  }

  protected open(): void {
    this.confirmationEmail = '';
    this.currentPassword = '';
    this.expanded.set(true);
  }

  protected cancel(): void {
    if (!this.submitting()) {
      this.expanded.set(false);
    }
  }

  protected submit(): void {
    if (this.confirmationEmail.trim().toLowerCase() !== this.accountEmail.trim().toLowerCase()) {
      this.facade.setValidationError('accountDeletion.errors.emailMismatch');
      return;
    }

    this.facade.request({
      confirmationEmail: this.confirmationEmail.trim(),
      currentPassword: this.currentPassword
    });
  }
}
