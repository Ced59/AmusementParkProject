import { Component } from '@angular/core';
import { TranslationService } from '../../services/translation.service';

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss']
})
export class SidebarComponent {
  isCollapsed: boolean = false;
  currentLang: string = 'en';

  constructor(private translationService: TranslationService) {
    this.currentLang = this.translationService.getCurrentLang() || 'en';
  }

  toggleCollapse(): void {
    this.isCollapsed = !this.isCollapsed;
  }
}
