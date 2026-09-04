import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import {
  GlobalRatingSuggestionPreference,
  GlobalRatingSuggestionPresentation,
  GlobalRatingSuggestions,
  PresentGlobalRatingSuggestionsRequest,
  RecordGlobalRatingSuggestionInteractionRequest
} from '@app/models/passport/global-rating-suggestion.models';
import { GlobalRatingSuggestionsApiService } from '@data-access/passport/global-rating-suggestions-api.service';

export interface GlobalRatingSuggestionsApiPort {
  getSuggestions(): Observable<GlobalRatingSuggestions>;
  presentSuggestions(
    request: PresentGlobalRatingSuggestionsRequest
  ): Observable<GlobalRatingSuggestionPresentation>;
  setEnabled(isEnabled: boolean): Observable<GlobalRatingSuggestionPreference>;
  recordInteraction(
    request: RecordGlobalRatingSuggestionInteractionRequest
  ): Observable<GlobalRatingSuggestionPreference>;
}

export const GLOBAL_RATING_SUGGESTIONS_API_PORT =
  new InjectionToken<GlobalRatingSuggestionsApiPort>('GLOBAL_RATING_SUGGESTIONS_API_PORT', {
    providedIn: 'root',
    factory: () => inject(GlobalRatingSuggestionsApiService)
  });
