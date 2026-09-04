import { ChangeDetectionStrategy, Component, DestroyRef, EventEmitter, OnInit, Output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateModule } from '@ngx-translate/core';

import { TranslationService } from '@app/services/translation.service';
import { UiButtonDirective, UiChipComponent, UiSurfaceDirective } from '@ui/primitives';
import { GlobalRatingSuggestionViewModel } from '../../models/global-rating-suggestion-view.models';
import { GlobalRatingSuggestionsStateFacade } from '../../state/global-rating-suggestions-state.facade';

@Component({
  selector: 'app-global-rating-suggestions',
  templateUrl: './global-rating-suggestions.component.html',
  styleUrl: './global-rating-suggestions.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [GlobalRatingSuggestionsStateFacade],
  imports: [TranslateModule, UiButtonDirective, UiChipComponent, UiSurfaceDirective]
})
export class GlobalRatingSuggestionsComponent implements OnInit {
  @Output() readonly reviewRequested = new EventEmitter<GlobalRatingSuggestionViewModel>();

  protected readonly facade: GlobalRatingSuggestionsStateFacade;

  constructor(
    facade: GlobalRatingSuggestionsStateFacade,
    private readonly translationService: TranslationService,
    private readonly destroyRef: DestroyRef
  ) {
    this.facade = facade;
  }

  ngOnInit(): void {
    this.facade.load(this.translationService.getCurrentLang() || 'en');
    this.translationService.languageChanged
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((language: string): void => this.facade.changeLanguage(language));
  }

  protected review(suggestion: GlobalRatingSuggestionViewModel): void {
    this.facade.accept(suggestion, (): void => this.reviewRequested.emit(suggestion));
  }
}
