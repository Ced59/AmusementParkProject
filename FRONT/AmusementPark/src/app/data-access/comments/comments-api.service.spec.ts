import { HttpClient } from '@angular/common/http';
import { of } from 'rxjs';

import { CreateCommentRequest, UpdateCommentRequest } from '@app/models/comments/comment.models';
import { environment } from '../../../environments/environment';
import { CommentsApiService } from './comments-api.service';

describe('CommentsApiService', () => {
  it('uses the API base URL without introducing a double slash for comment reads', () => {
    const httpClient = {
      get: vi.fn()
        .mockReturnValueOnce(of({
          targetType: 'ParkItem',
          targetId: 'item/1',
          targetName: 'Item',
          parkId: 'park-1',
          parkName: 'Park',
          comments: []
        }))
        .mockReturnValueOnce(of({
          targetType: 'ParkItem',
          targetId: 'item/1',
          commentCount: 0,
          officialComment: null
        }))
    };
    const service: CommentsApiService = new CommentsApiService(httpClient as unknown as HttpClient);

    service.getThread('ParkItem', 'item/1').subscribe();
    service.getSummary('ParkItem', 'item/1').subscribe();

    expect(httpClient.get).toHaveBeenNthCalledWith(
      1,
      `${environment.apiBaseUrl}comments/ParkItem/item%2F1`
    );
    expect(httpClient.get).toHaveBeenNthCalledWith(
      2,
      `${environment.apiBaseUrl}comments/ParkItem/item%2F1/summary`
    );
  });

  it('uses the API base URL without introducing a double slash when creating a comment', () => {
    const httpClient = {
      post: vi.fn().mockReturnValue(of({}))
    };
    const service: CommentsApiService = new CommentsApiService(httpClient as unknown as HttpClient);
    const request: CreateCommentRequest = {
      targetType: 'Park',
      targetId: 'park-1',
      bodies: [{ languageCode: 'fr', value: 'Avis' }],
      isOfficial: false
    };

    service.createComment(request).subscribe();

    expect(httpClient.post).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}comments`,
      request,
      expect.objectContaining({
        headers: expect.anything()
      })
    );
  });

  it('uses encoded comment identifiers for update and delete operations', () => {
    const httpClient = {
      put: vi.fn().mockReturnValue(of({})),
      delete: vi.fn().mockReturnValue(of(undefined))
    };
    const service: CommentsApiService = new CommentsApiService(httpClient as unknown as HttpClient);
    const request: UpdateCommentRequest = {
      id: 'comment/1',
      bodies: [{ languageCode: 'fr', value: '<p>Avis corrigé</p>' }],
      isOfficial: true
    };

    service.updateComment(request).subscribe();
    service.deleteComment(request.id).subscribe();

    expect(httpClient.put).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}comments/comment%2F1`,
      {
        bodies: request.bodies,
        isOfficial: true
      },
      expect.objectContaining({
        headers: expect.anything()
      })
    );
    expect(httpClient.delete).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}comments/comment%2F1`
    );
  });
});
