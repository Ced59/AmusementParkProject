import { Component, ElementRef, HostListener, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { TranslationService } from '../../services/translation.service';
import {AuthService} from "../../services/auth/auth.service";
import {SharedService} from "../../services/shared/shared.service";

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss']
})
export class SidebarComponent implements OnInit, OnDestroy {
  isCollapsed: boolean = true;
  currentLang: string = 'en';
  isLoggedIn = false;
  isAdmin = false;
  private langSub!: Subscription;

  private subscriptions = new Subscription();

  constructor(
    private translationService: TranslationService,
    private authService: AuthService,
    private sharedService: SharedService,
    private elRef: ElementRef
  ) {}

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
  }

  handleNavClick(event: Event): void {
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
