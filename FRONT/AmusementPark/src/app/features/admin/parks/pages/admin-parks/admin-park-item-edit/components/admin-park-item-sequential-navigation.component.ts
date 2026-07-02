import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from '@shared/ui/primitives/button';

import {
  AdminParkItemSequentialNavigationState,
  EMPTY_ADMIN_PARK_ITEM_SEQUENTIAL_NAVIGATION_STATE
} from '@features/admin/park-items/models/admin-park-item-sequential-navigation.model';

@Component({
  selector: 'app-admin-park-item-sequential-navigation',
  template: `
    @if (shouldDisplay) {
      <div class="admin-park-item-sequential-navigation" aria-live="polite">
        <button
          appUiButton
          type="button"
          class="p-button-sm p-button-outlined p-button-secondary"
          icon="pi pi-chevron-left"
          [label]="'pagination.previous' | translate"
          [disabled]="previousDisabled"
          (click)="previous.emit()">
        </button>

        <span class="admin-park-item-sequential-navigation__position">
          @if (state.isLoading) {
            <i class="pi pi-spin pi-spinner" aria-hidden="true"></i>
          } @else if (hasKnownPosition) {
            <strong>{{ state.currentPosition }} / {{ state.totalItems }}</strong>
            <small>+{{ state.remainingItems }}</small>
          } @else {
            <strong>—</strong>
          }
        </span>

        <button
          appUiButton
          type="button"
          class="p-button-sm p-button-outlined p-button-secondary"
          icon="pi pi-chevron-right"
          iconPos="right"
          [label]="'pagination.next' | translate"
          [disabled]="nextDisabled"
          (click)="next.emit()">
        </button>
      </div>
    }
  `,
  styles: [`
    :host {
      display: block;
    }

    .admin-park-item-sequential-navigation {
      display: flex;
      flex-wrap: wrap;
      justify-content: flex-end;
      align-items: center;
      gap: 0.75rem;
      margin-top: 0.85rem;
      padding: 0.75rem;
      border: 1px solid var(--surface-border);
      border-radius: 14px;
      background: var(--surface-50);
    }

    .admin-park-item-sequential-navigation__position {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 0.45rem;
      min-width: 7rem;
      padding: 0.5rem 0.75rem;
      border-radius: 999px;
      background: var(--surface-0);
      color: var(--text-color-secondary);
      font-weight: 700;
    }

    .admin-park-item-sequential-navigation__position strong {
      color: var(--text-color);
    }

    .admin-park-item-sequential-navigation__position small {
      color: var(--text-color-secondary);
      font-weight: 600;
    }

    @media (max-width: 767px) {
      .admin-park-item-sequential-navigation {
        align-items: stretch;
        flex-direction: column;
      }

      .admin-park-item-sequential-navigation__position {
        width: 100%;
      }

      :host ::ng-deep .admin-park-item-sequential-navigation .p-button {
        width: 100%;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonDirective, TranslateModule]
})
export class AdminParkItemSequentialNavigationComponent {
  @Input() state: AdminParkItemSequentialNavigationState = EMPTY_ADMIN_PARK_ITEM_SEQUENTIAL_NAVIGATION_STATE;
  @Input() isEditMode: boolean = false;
  @Input() isSaving: boolean = false;
  @Input() isDirty: boolean = false;

  @Output() previous: EventEmitter<void> = new EventEmitter<void>();
  @Output() next: EventEmitter<void> = new EventEmitter<void>();

  get shouldDisplay(): boolean {
    return this.isEditMode || this.state.isLoading || this.state.totalItems > 1;
  }

  get hasKnownPosition(): boolean {
    return this.state.totalItems > 0 && this.state.currentPosition > 0;
  }

  get previousDisabled(): boolean {
    return this.isNavigationDisabled || !this.state.previousItemId;
  }

  get nextDisabled(): boolean {
    return this.isNavigationDisabled || !this.state.nextItemId;
  }

  private get isNavigationDisabled(): boolean {
    return this.isSaving || this.isDirty || this.state.isLoading;
  }
}
