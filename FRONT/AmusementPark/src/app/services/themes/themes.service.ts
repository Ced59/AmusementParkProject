import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly storageKey: string = 'amusement-park-theme';

  constructor(@Inject(PLATFORM_ID) private readonly platformId: object) {
    if (isPlatformBrowser(this.platformId)) {
      this.initializeTheme();
    }
  }

  initializeTheme(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    const savedTheme: string | null = localStorage.getItem(this.storageKey);

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

    const prefersDark: boolean = window.matchMedia('(prefers-color-scheme: dark)').matches;
    this.changeTheme(prefersDark ? 'dark' : 'light');
  }

  getCurrentTheme(): 'light' | 'dark' {
    if (!isPlatformBrowser(this.platformId)) {
      return 'dark';
    }

    return document.body.classList.contains('light-mode') ? 'light' : 'dark';
  }

  changeTheme(genericTheme: string): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    const normalizedTheme: 'light' | 'dark' = genericTheme === 'light' ? 'light' : 'dark';
    const documentElement: HTMLElement = document.documentElement;
    const body: HTMLElement = document.body;

    documentElement.classList.remove('light-mode', 'dark-mode');
    documentElement.classList.add(`${normalizedTheme}-mode`);

    body.classList.remove('light-mode', 'dark-mode');
    body.classList.add(`${normalizedTheme}-mode`);

    localStorage.setItem(this.storageKey, normalizedTheme);
  }
}
