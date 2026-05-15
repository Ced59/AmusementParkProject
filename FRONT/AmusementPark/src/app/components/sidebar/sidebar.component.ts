import { Component, DestroyRef, ElementRef, HostListener, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslationService } from '@app/services/translation.service';
import { AuthService } from '@app/services/auth/auth.service';
import { SharedService } from '@app/services/shared/shared.service';
import { SidebarViewComponent } from './sidebar-view.component';

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss'],
  imports: [SidebarViewComponent]
})
export class SidebarComponent implements OnInit {
  isCollapsed: boolean = true;
  currentLang: string = 'en';
  isLoggedIn: boolean = false;
  isAdmin: boolean = false;

  constructor(
    private readonly translationService: TranslationService,
    private readonly authService: AuthService,
    private readonly sharedService: SharedService,
    private readonly elRef: ElementRef<HTMLElement>,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    this.currentLang = this.translationService.getCurrentLang() || 'en';
    this.translationService.languageChanged
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((lang: string): void => {
        this.currentLang = lang;
      });

    this.checkAuthStatus();

    this.sharedService.getLoginStatusListener()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((): void => {
        this.checkAuthStatus();
      });
  }

  handleNavClick(event: Event): void {
    void event;

    if (window.innerWidth < 768) {
      this.isCollapsed = true;
    }
  }

  checkAuthStatus(): void {
    this.isLoggedIn = this.authService.isLoggedIn();
    this.isAdmin = this.isLoggedIn && this.authService.hasRole('ADMIN');
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (window.innerWidth < 768 && !this.isCollapsed && !this.elRef.nativeElement.contains(event.target as Node)) {
      this.isCollapsed = true;
    }
  }
}
