import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import {
  GlobalRatingSuggestion,
  GlobalRatingSuggestionInteractionType,
  GlobalRatingSuggestionPresentationTarget,
  RecordGlobalRatingSuggestionInteractionRequest
} from '@app/models/passport/global-rating-suggestion.models';
import { mapGlobalRatingSuggestionView } from '../mappers/global-rating-suggestion-view.mapper';
import { GlobalRatingSuggestionViewModel } from '../models/global-rating-suggestion-view.models';
import {
  GLOBAL_RATING_SUGGESTIONS_API_PORT,
  GlobalRatingSuggestionsApiPort
} from './global-rating-suggestions-state-data.ports';

@Injectable()
export class GlobalRatingSuggestionsStateFacade {
  private readonly suggestionsSignal = signal<GlobalRatingSuggestionViewModel[]>([]);
  private readonly availableSignal = signal<boolean>(false);
  private readonly enabledSignal = signal<boolean>(true);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly savingSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);
  private sourceSuggestions: GlobalRatingSuggestion[] = [];
  private language: string = 'en';

  readonly suggestions: Signal<GlobalRatingSuggestionViewModel[]> = this.suggestionsSignal.asReadonly();
  readonly available: Signal<boolean> = this.availableSignal.asReadonly();
  readonly enabled: Signal<boolean> = this.enabledSignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly saving: Signal<boolean> = this.savingSignal.asReadonly();
  readonly error: Signal<boolean> = this.errorSignal.asReadonly();

  constructor(
    @Inject(GLOBAL_RATING_SUGGESTIONS_API_PORT)
    private readonly api: GlobalRatingSuggestionsApiPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(language: string): void {
    this.language = language;
    this.loadingSignal.set(true);
    this.errorSignal.set(false);
    this.sourceSuggestions = [];
    this.remap();
    this.api.getSuggestions().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response): void => {
        this.availableSignal.set(response.isAvailable);
        this.enabledSignal.set(response.isEnabled);
        if (!response.isAvailable || !response.isEnabled || response.suggestions.length === 0) {
          this.sourceSuggestions = [];
          this.remap();
          this.loadingSignal.set(false);
          return;
        }

        const targets: GlobalRatingSuggestionPresentationTarget[] = response.suggestions.map(
          (suggestion: GlobalRatingSuggestion): GlobalRatingSuggestionPresentationTarget => {
            return this.toTarget(suggestion);
          }
        );
        this.api.presentSuggestions({ targets })
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: (presentation): void => {
              const presentedTargets: GlobalRatingSuggestionPresentationTarget[] =
                presentation.isAvailable && presentation.isEnabled
                  ? presentation.presentedTargets
                  : [];
              const acknowledgedTargets = new Set<string>(presentedTargets.map(
                (target: GlobalRatingSuggestionPresentationTarget): string => {
                  return `${target.targetType}:${target.targetId}`;
                }
              ));
              this.availableSignal.set(presentation.isAvailable);
              this.enabledSignal.set(presentation.isEnabled);
              this.sourceSuggestions = response.suggestions.filter(
                (suggestion: GlobalRatingSuggestion): boolean => {
                  const target: GlobalRatingSuggestionPresentationTarget = this.toTarget(suggestion);
                  return acknowledgedTargets.has(`${target.targetType}:${target.targetId}`);
                }
              );
              this.remap();
              this.loadingSignal.set(false);
            },
            error: (): void => {
              this.sourceSuggestions = [];
              this.remap();
              this.loadingSignal.set(false);
              this.errorSignal.set(true);
            }
          });
      },
      error: (): void => {
        this.loadingSignal.set(false);
        this.errorSignal.set(true);
      }
    });
  }

  changeLanguage(language: string): void {
    this.language = language;
    this.remap();
  }

  dismiss(view: GlobalRatingSuggestionViewModel): void {
    this.recordAndRemove(view, 'Dismissed');
  }

  accept(view: GlobalRatingSuggestionViewModel, accepted: () => void): void {
    this.savingSignal.set(true);
    this.errorSignal.set(false);
    this.api.recordInteraction(this.toRequest(view, 'Accepted'))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (): void => {
          this.remove(view.id);
          this.savingSignal.set(false);
          accepted();
        },
        error: (): void => {
          this.savingSignal.set(false);
          this.errorSignal.set(true);
        }
      });
  }

  setEnabled(isEnabled: boolean): void {
    this.savingSignal.set(true);
    this.errorSignal.set(false);
    this.api.setEnabled(isEnabled).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (preference): void => {
        this.availableSignal.set(preference.isAvailable);
        this.enabledSignal.set(preference.isEnabled);
        if (!preference.isEnabled) {
          this.sourceSuggestions = [];
          this.remap();
        }
        this.savingSignal.set(false);
        if (preference.isEnabled) {
          this.load(this.language);
        }
      },
      error: (): void => {
        this.savingSignal.set(false);
        this.errorSignal.set(true);
      }
    });
  }

  private recordAndRemove(
    view: GlobalRatingSuggestionViewModel,
    interactionType: GlobalRatingSuggestionInteractionType
  ): void {
    this.savingSignal.set(true);
    this.errorSignal.set(false);
    this.api.recordInteraction(this.toRequest(view, interactionType))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (): void => {
          this.remove(view.id);
          this.savingSignal.set(false);
        },
        error: (): void => {
          this.savingSignal.set(false);
          this.errorSignal.set(true);
        }
      });
  }

  private toRequest(
    view: GlobalRatingSuggestionViewModel,
    interactionType: GlobalRatingSuggestionInteractionType
  ): RecordGlobalRatingSuggestionInteractionRequest {
    return { targetType: view.targetType, targetId: view.targetId, interactionType };
  }

  private remove(id: string): void {
    this.sourceSuggestions = this.sourceSuggestions.filter((suggestion: GlobalRatingSuggestion): boolean => {
      const targetType: 'Park' | 'ParkItem' = suggestion.targetType === 'Park' || suggestion.targetType === 1
        ? 'Park'
        : 'ParkItem';
      return `${targetType}:${suggestion.targetId}` !== id;
    });
    this.remap();
  }

  private toTarget(suggestion: GlobalRatingSuggestion): GlobalRatingSuggestionPresentationTarget {
    const targetType: 'Park' | 'ParkItem' = suggestion.targetType === 'Park' || suggestion.targetType === 1
      ? 'Park'
      : 'ParkItem';
    return { targetType, targetId: suggestion.targetId };
  }

  private remap(): void {
    this.suggestionsSignal.set(this.sourceSuggestions.map((suggestion: GlobalRatingSuggestion) => {
      return mapGlobalRatingSuggestionView(suggestion, this.language);
    }));
  }
}
