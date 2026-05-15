import { Component, DestroyRef, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { EMPTY, switchMap } from 'rxjs';
import { catchError, filter, tap } from 'rxjs/operators';

import { TranslationService } from '@app/services/translation.service';
import { TopbarComponent } from '@app/components/topbar/topbar.component';
import { SidebarComponent } from '@app/components/sidebar/sidebar.component';
import { Bind } from 'primeng/bind';
import { Toast } from 'primeng/toast';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
  imports: [TopbarComponent, SidebarComponent, RouterOutlet, Bind, Toast]
})
export class AppComponent implements OnInit {
  title: string = 'Amusement Parks';
  isLoading: boolean = true;
  showTopbar: boolean = true;

  constructor(
    private readonly translationService: TranslationService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    this.router.events.pipe(
      filter((event: unknown): event is NavigationEnd => event instanceof NavigationEnd),
      tap((): void => {
        this.isLoading = true;
      }),
      switchMap(() => {
        const lang: string | null | undefined = this.route.root.firstChild?.snapshot.paramMap.get('lang');
        if (!lang) {
          this.isLoading = false;
          return EMPTY;
        }

        return this.translationService.useLang(lang).pipe(
          tap((): void => {
            this.isLoading = false;
          }),
          catchError((error: unknown) => {
            this.isLoading = false;
            console.error('Error loading language:', error);
            return EMPTY;
          })
        );
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe();
  }
}
