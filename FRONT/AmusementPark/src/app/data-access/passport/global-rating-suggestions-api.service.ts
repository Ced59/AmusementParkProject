import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  GlobalRatingSuggestionPreference,
  GlobalRatingSuggestions,
  RecordGlobalRatingSuggestionInteractionRequest
} from '@app/models/passport/global-rating-suggestion.models';
import { environment } from '../../../environments/environment';
import { GLOBAL_RATING_SUGGESTIONS_API_ENDPOINTS } from './global-rating-suggestions-api-endpoints';

@Injectable({ providedIn: 'root' })
export class GlobalRatingSuggestionsApiService {
  constructor(private readonly http: HttpClient) {
  }

  getSuggestions(): Observable<GlobalRatingSuggestions> {
    return this.http.get<GlobalRatingSuggestions>(
      `${environment.apiBaseUrl}${GLOBAL_RATING_SUGGESTIONS_API_ENDPOINTS.suggestions}`,
      { transferCache: false }
    );
  }

  setEnabled(isEnabled: boolean): Observable<GlobalRatingSuggestionPreference> {
    return this.http.put<GlobalRatingSuggestionPreference>(
      `${environment.apiBaseUrl}${GLOBAL_RATING_SUGGESTIONS_API_ENDPOINTS.preference}`,
      { isEnabled }
    );
  }

  recordInteraction(
    request: RecordGlobalRatingSuggestionInteractionRequest
  ): Observable<GlobalRatingSuggestionPreference> {
    return this.http.post<GlobalRatingSuggestionPreference>(
      `${environment.apiBaseUrl}${GLOBAL_RATING_SUGGESTIONS_API_ENDPOINTS.interactions}`,
      request
    );
  }
}
