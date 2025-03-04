import { Component } from '@angular/core';
import { TranslationService } from '../../services/translation.service';

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss']
})
export class SidebarComponent {
  // La sidebar sera collapsée par défaut
  isCollapsed: boolean = true;

  constructor(public translationService: TranslationService) {}

  // Utilisez un getter pour récupérer la langue actuelle
  get currentLang(): string {
    return this.translationService.getCurrentLang() || 'en';
  }

  toggleCollapse(): void {
    this.isCollapsed = !this.isCollapsed;
  }

  handleNavClick(event: Event): void {
    if (this.isCollapsed && window.innerWidth < 768) {
      event.preventDefault();
      this.toggleCollapse();
    }
  }
}
