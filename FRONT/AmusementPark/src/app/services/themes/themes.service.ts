import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private themeLinkElement: HTMLLinkElement | null = null;

  private readonly themeMap: Record<string, string> = {
    light: 'lara-light-purple',
    dark: 'lara-dark-purple'
  };

  private readonly storageKey = 'amusement-park-theme';

  constructor(@Inject(PLATFORM_ID) private platformId: Object) {
    if (isPlatformBrowser(this.platformId)) {
      this.themeLinkElement = document.getElementById('theme-css') as HTMLLinkElement | null;
      this.initializeTheme();
    }
  }

  initializeTheme(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    const savedTheme = localStorage.getItem(this.storageKey);

    if (savedTheme === 'light' || savedTheme === 'dark') {
      this.changeTheme(savedTheme);
      return;
    }

    this.setThemeBasedOnSystem();
  }

  setThemeBasedOnSystem(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    this.changeTheme(prefersDark ? 'dark' : 'light');
  }

  getCurrentTheme(): 'light' | 'dark' {
    if (!isPlatformBrowser(this.platformId) || !this.themeLinkElement) {
      return 'dark';
    }

    return this.themeLinkElement.href.includes('light') ? 'light' : 'dark';
  }

  changeTheme(genericTheme: string): void {
    if (!isPlatformBrowser(this.platformId) || !this.themeLinkElement) {
      return;
    }

    const normalizedTheme = genericTheme === 'light' ? 'light' : 'dark';
    const themeDir = this.themeMap[normalizedTheme];

    this.themeLinkElement.href = `assets/themes/${themeDir}/theme.css`;

    document.body.classList.remove('light-mode', 'dark-mode');
    document.body.classList.add(`${normalizedTheme}-mode`);

    localStorage.setItem(this.storageKey, normalizedTheme);
  }
}
