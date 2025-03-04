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
  currentLang: string = 'en';

  constructor(private translationService: TranslationService) {
    this.currentLang = this.translationService.getCurrentLang() || 'en';
  }

  toggleCollapse(): void {
    this.isCollapsed = !this.isCollapsed;
  }

  // Gestionnaire pour les clics sur les liens de navigation
  handleNavClick(event: Event): void {
    if (this.isCollapsed && window.innerWidth < 768) {
      event.preventDefault();
      this.toggleCollapse();
    }
  }
}
