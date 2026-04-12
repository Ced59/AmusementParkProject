import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { NgClass } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-sidebar-view',
  templateUrl: './sidebar-view.component.html',
  styleUrls: ['./sidebar.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass, RouterLinkActive, RouterLink, TranslateModule]
})
export class SidebarViewComponent {
  @Input() isCollapsed: boolean = true;
  @Input() currentLang: string = 'en';
  @Input() isLoggedIn: boolean = false;
  @Input() isAdmin: boolean = false;

  @Output() navClicked: EventEmitter<Event> = new EventEmitter<Event>();

  handleNavClick(event: Event): void {
    this.navClicked.emit(event);
  }
}
