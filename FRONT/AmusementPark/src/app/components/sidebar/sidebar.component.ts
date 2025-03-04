import { Component, ElementRef, HostListener, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { TranslationService } from '../../services/translation.service';

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss']
})
export class SidebarComponent implements OnInit, OnDestroy {
  isCollapsed: boolean = true;
  currentLang: string = 'en';
  private langSub!: Subscription;

  constructor(
    private translationService: TranslationService,
    private elRef: ElementRef
  ) {}

  ngOnInit(): void {
    this.currentLang = this.translationService.getCurrentLang() || 'en';
    // Utiliser languageChanged (EventEmitter<string>) et typer le paramètre
    this.langSub = this.translationService.languageChanged.subscribe((lang: string) => {
      this.currentLang = lang;
    });
  }

  ngOnDestroy(): void {
    if (this.langSub) {
      this.langSub.unsubscribe();
    }
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

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (window.innerWidth < 768 && !this.isCollapsed) {
      if (!this.elRef.nativeElement.contains(event.target)) {
        this.isCollapsed = true;
      }
    }
  }
}
