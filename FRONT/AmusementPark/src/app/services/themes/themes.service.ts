import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly themeLinkElement: HTMLLinkElement | null = null;
  // Mapping générique : "light" et "dark" vers les dossiers réels des thèmes
  private themeMap: { [key: string]: string } = {
    'light': 'lara-light-purple',
    'dark': 'lara-dark-purple'
  };

  constructor(@Inject(PLATFORM_ID) private platformId: Object) {
    if (isPlatformBrowser(this.platformId)) {
      this.themeLinkElement = document.getElementById('theme-css') as HTMLLinkElement;
      this.setThemeBasedOnSystem();
    }
  }

  setThemeBasedOnSystem(): void {
    if (isPlatformBrowser(this.platformId) && this.themeLinkElement) {
      const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
      const genericTheme = prefersDark ? 'dark' : 'light';
      this.changeTheme(genericTheme);
    }
  }

  changeTheme(genericTheme: string): void {
    if (isPlatformBrowser(this.platformId) && this.themeLinkElement) {
      const themeDir = this.themeMap[genericTheme] || this.themeMap['light'];
      this.themeLinkElement.href = `assets/themes/${themeDir}/theme.css`;

      if (genericTheme === 'light') {
        document.body.classList.add('light-mode');
        document.body.classList.remove('dark-mode');
      } else {
        document.body.classList.add('dark-mode');
        document.body.classList.remove('light-mode');
      }
    }
  }
}
