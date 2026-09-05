import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { TranslationService } from '@app/services/translation.service';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { UiArticleCardComponent, UiFeaturedParkCardComponent } from '@ui/cards';
import { UiButtonDirective, UiSectionHeaderComponent, UiSurfaceDirective } from '@ui/primitives';
import { HomeLatestContentStateFacade } from '../state/home-latest-content-state.facade';

@Component({
  selector: 'app-home-latest-content',
  templateUrl: './home-latest-content.component.html',
  styleUrls: ['./home-latest-content.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [HomeLatestContentStateFacade],
  imports: [
    RouterLink,
    TranslateModule,
    PageStateComponent,
    UiArticleCardComponent,
    UiFeaturedParkCardComponent,
    UiButtonDirective,
    UiSectionHeaderComponent,
    UiSurfaceDirective
  ]
})
export class HomeLatestContentComponent implements OnInit {
  protected readonly currentLang = signal<string>('en');
  protected readonly latestParksState = this.stateFacade.latestParksState;
  protected readonly latestParks = this.stateFacade.latestParks;
  protected readonly latestArticlesState = this.stateFacade.latestArticlesState;
  protected readonly latestArticles = this.stateFacade.latestArticles;

  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  constructor(
    private readonly stateFacade: HomeLatestContentStateFacade,
    private readonly translationService: TranslationService
  ) {
  }

  ngOnInit(): void {
    const currentLanguage: string = this.translationService.getCurrentLang() || 'en';
    this.currentLang.set(currentLanguage);
    this.stateFacade.setCurrentLanguage(currentLanguage);
    this.stateFacade.loadLatestContent();

    this.translationService.languageChanged
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((language: string) => {
        this.currentLang.set(language);
        this.stateFacade.setCurrentLanguage(language);
      });
  }
}
