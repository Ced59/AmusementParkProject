import { isPlatformBrowser, NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, OnInit, PLATFORM_ID } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { ThemeService } from '@app/services/themes/themes.service';

@Component({
  selector: 'app-theme-switcher',
  templateUrl: './theme-switcher.component.html',
  styleUrls: ['./theme-switcher.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass, TranslateModule]
})
export class ThemeSwitcherComponent implements OnInit {
  currentTheme: 'light' | 'dark' = 'dark';

  constructor(
    private readonly themeService: ThemeService,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {
  }

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.currentTheme = this.themeService.getCurrentTheme();
    }
  }

  toggleTheme(): void {
    const nextTheme: 'light' | 'dark' = this.currentTheme === 'light' ? 'dark' : 'light';
    this.currentTheme = nextTheme;
    this.themeService.changeTheme(nextTheme);
  }
}
