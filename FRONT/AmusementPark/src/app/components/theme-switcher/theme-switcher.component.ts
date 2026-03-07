import { Component, OnInit, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ThemeService } from '../../services/themes/themes.service';

@Component({
  selector: 'app-theme-switcher',
  templateUrl: './theme-switcher.component.html',
  styleUrls: ['./theme-switcher.component.scss']
})
export class ThemeSwitcherComponent implements OnInit {
  currentTheme: 'light' | 'dark' = 'dark';
  displayThemeDialog = false;

  constructor(
    private themeService: ThemeService,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.currentTheme = this.themeService.getCurrentTheme();
    }
  }

  openThemeDialog(): void {
    this.displayThemeDialog = true;
  }

  changeTheme(themeName: 'light' | 'dark'): void {
    this.currentTheme = themeName;
    this.themeService.changeTheme(themeName);
    this.displayThemeDialog = false;
  }
}
