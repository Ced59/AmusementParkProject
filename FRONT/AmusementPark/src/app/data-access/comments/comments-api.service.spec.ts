import { HttpClient } from '@angular/common/http';
import { of } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CommentsApiService } from './comments-api.service';

describe('CommentsApiService', () => {
  it('encodes target identifiers when loading a comment summary', () => {
    const httpClient = {
      get: vi.fn().mockReturnValue(of({
        targetType: 'ParkItem',
        targetId: 'item/1',
        commentCount: 0,
        officialComment: null
      }))
    };
    const service: CommentsApiService = new CommentsApiService(httpClient as unknown as HttpClient);

    service.getSummary('ParkItem', 'item/1').subscribe();

    expect(httpClient.get).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}/comments/ParkItem/item%2F1/summary`
    );
  });
});
