import { Directive, HostBinding, Input } from '@angular/core';

@Directive({
  selector: 'button[appAdminButton], a[appAdminButton]',
  standalone: true
})
export class AdminButtonDirective {
  @Input() tone: 'primary' | 'secondary' | 'success' | 'warning' | 'danger' = 'primary';
  @Input() size: 'regular' | 'sm' = 'regular';

  @HostBinding('class.admin-button') protected readonly adminButtonClass: boolean = true;
  @HostBinding('class.admin-button--secondary') protected get secondaryClass(): boolean {
    return this.tone === 'secondary';
  }
  @HostBinding('class.admin-button--success') protected get successClass(): boolean {
    return this.tone === 'success';
  }
  @HostBinding('class.admin-button--warning') protected get warningClass(): boolean {
    return this.tone === 'warning';
  }
  @HostBinding('class.admin-button--danger') protected get dangerClass(): boolean {
    return this.tone === 'danger';
  }
  @HostBinding('class.admin-button--sm') protected get smallClass(): boolean {
    return this.size === 'sm';
  }
}
