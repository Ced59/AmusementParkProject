import { ChangeDetectionStrategy, Component, HostBinding } from '@angular/core';

@Component({
  selector: 'app-admin-card',
  standalone: true,
  template: `
    <div class="admin-card__body">
      <ng-content></ng-content>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminCardComponent {
  @HostBinding('class.admin-card') protected readonly adminCardClass: boolean = true;
}
