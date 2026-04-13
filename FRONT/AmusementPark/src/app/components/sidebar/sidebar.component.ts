import { Component, ElementRef, HostListener, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
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
export class SidebarComponent implements OnInit, OnDestroy {
  isCollapsed: boolean = true;
  currentLang: string = 'en';
  isLoggedIn: boolean = false;
  isAdmin: boolean = false;
  private langSub!: Subscription;

  private subscriptions = new Subscription();

  constructor(
    private readonly translationService: TranslationService,
    private readonly authService: AuthService,
    private readonly sharedService: SharedService,
    private readonly elRef: ElementRef
  ) {
  }

  ngOnInit(): void {
    this.currentLang = this.translationService.getCurrentLang() || 'en';
    this.langSub = this.translationService.languageChanged.subscribe((lang: string) => {
      this.currentLang = lang;
    });

    this.checkAuthStatus();

    this.subscriptions.add(
      this.sharedService.getLoginStatusListener().subscribe(() => {
        this.checkAuthStatus();
      })
    );
  }

  ngOnDestroy(): void {
    if (this.langSub) {
      this.langSub.unsubscribe();
    }

    this.subscriptions.unsubscribe();
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
    if (window.innerWidth < 768 && !this.isCollapsed && !this.elRef.nativeElement.contains(event.target)) {
      this.isCollapsed = true;
    }
  }
}
