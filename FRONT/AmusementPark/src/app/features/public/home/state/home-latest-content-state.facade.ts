import { DestroyRef, Inject, Injectable, Signal, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { HomeFeaturedParkCardModel } from '@app/models/home/home-featured-park-card.model';
import { HomeFeaturedParkModel } from '@app/models/home/home-featured-park.model';
import { HomeLatestArticleCardModel } from '@app/models/home/home-latest-article-card.model';
import { HomeLatestArticleModel } from '@app/models/home/home-latest-article.model';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { mapHomeFeaturedParkToCardModel } from '../mappers/home-featured-park.mapper';
import { mapHomeLatestArticleToCardModel } from '../mappers/home-latest-article.mapper';
import { HOME_LATEST_CONTENT_DATA_PORT, HomeLatestContentDataPort } from './home-latest-content-data.ports';

interface HomeLatestParksViewModel {
  parks: HomeFeaturedParkModel[];
}

interface HomeLatestArticlesViewModel {
  articles: HomeLatestArticleModel[];
}

@Injectable()
export class HomeLatestContentStateFacade {
  private static readonly ContentLimit: number = 3;

  private readonly latestParksStateStore = new SignalScreenStateStore<HomeLatestParksViewModel>();
  private readonly latestArticlesStateStore = new SignalScreenStateStore<HomeLatestArticlesViewModel>();
  private readonly currentLanguageSignal = signal<string>('en');
  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  public readonly latestParksState = this.latestParksStateStore.state;
  public readonly latestArticlesState = this.latestArticlesStateStore.state;
  public readonly latestParks: Signal<HomeFeaturedParkCardModel[]> = computed(() =>
    (this.latestParksStateStore.data()?.parks ?? []).map((park: HomeFeaturedParkModel, index: number) =>
      mapHomeFeaturedParkToCardModel(
        park,
        this.currentLanguageSignal(),
        this.textTruncator,
        index,
        this.countryDisplayService)));
  public readonly latestArticles: Signal<HomeLatestArticleCardModel[]> = computed(() =>
    (this.latestArticlesStateStore.data()?.articles ?? []).map((article: HomeLatestArticleModel, index: number) =>
      mapHomeLatestArticleToCardModel(article, this.currentLanguageSignal(), this.textTruncator, index)));

  constructor(
    @Inject(HOME_LATEST_CONTENT_DATA_PORT) private readonly dataPort: HomeLatestContentDataPort,
    private readonly textTruncator: NaturalTextTruncatorService,
    private readonly countryDisplayService: CountryDisplayService
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  loadLatestContent(): void {
    this.loadLatestParks();
    this.loadLatestArticles();
  }

  private loadLatestParks(): void {
    const previousData: HomeLatestParksViewModel | undefined = this.latestParksStateStore.data();
    this.latestParksStateStore.setLoading(previousData);

    this.dataPort.getLatestParks(HomeLatestContentStateFacade.ContentLimit, anonymousHttpOptions())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (parks: HomeFeaturedParkModel[]) => {
          const viewModel: HomeLatestParksViewModel = { parks };
          parks.length > 0
            ? this.latestParksStateStore.setReady(viewModel)
            : this.latestParksStateStore.setEmpty(viewModel);
        },
        error: (error: unknown) => {
          console.error('Error loading latest parks', error);
          this.latestParksStateStore.setError('home.latestParks.errorMessage', previousData);
        }
      });
  }

  private loadLatestArticles(): void {
    const previousData: HomeLatestArticlesViewModel | undefined = this.latestArticlesStateStore.data();
    this.latestArticlesStateStore.setLoading(previousData);

    this.dataPort.getLatestArticles(HomeLatestContentStateFacade.ContentLimit, anonymousHttpOptions())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (articles: HomeLatestArticleModel[]) => {
          const viewModel: HomeLatestArticlesViewModel = { articles };
          articles.length > 0
            ? this.latestArticlesStateStore.setReady(viewModel)
            : this.latestArticlesStateStore.setEmpty(viewModel);
        },
        error: (error: unknown) => {
          console.error('Error loading latest articles', error);
          this.latestArticlesStateStore.setError('home.latestArticles.errorMessage', previousData);
        }
      });
  }
}
