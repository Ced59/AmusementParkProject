import { HttpClient } from '@angular/common/http';
import { of } from 'rxjs';

import { environment } from '../../../environments/environment';
import { GlobalRatingSuggestionsApiService } from './global-rating-suggestions-api.service';

describe('GlobalRatingSuggestionsApiService', () => {
  it('uses private non-cached reads and explicit mutation endpoints', () => {
    const httpClient: Pick<HttpClient, 'get' | 'put' | 'post'> = {
      get: vi.fn().mockReturnValue(of({})),
      put: vi.fn().mockReturnValue(of({})),
      post: vi.fn().mockReturnValue(of({}))
    };
    const service = new GlobalRatingSuggestionsApiService(httpClient as HttpClient);

    service.getSuggestions().subscribe();
    service.setEnabled(false).subscribe();
    service.recordInteraction({
      targetType: 'ParkItem',
      targetId: 'item/1',
      interactionType: 'Dismissed',
      presentedAtUtc: '2026-09-04T10:00:00Z'
    }).subscribe();

    const root: string = `${environment.apiBaseUrl}me/passport/rating-update-suggestions`;
    expect(httpClient.get).toHaveBeenCalledWith(root, { transferCache: false });
    expect(httpClient.put).toHaveBeenCalledWith(`${root}/preference`, { isEnabled: false });
    expect(httpClient.post).toHaveBeenCalledWith(`${root}/interactions`, {
      targetType: 'ParkItem',
      targetId: 'item/1',
      interactionType: 'Dismissed',
      presentedAtUtc: '2026-09-04T10:00:00Z'
    });
  });
});
