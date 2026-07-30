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
    service.getSummary('ParkItem', 'item/1', 'fr').subscribe();

    expect(httpClient.get).toHaveBeenNthCalledWith(
      1,
      `${environment.apiBaseUrl}comments/ParkItem/item%2F1`
    );
    expect(httpClient.get).toHaveBeenNthCalledWith(
      2,
      `${environment.apiBaseUrl}comments/ParkItem/item%2F1/summary`,
      {
        params: {
          language: 'fr'
        }
      }
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
      isOfficial: true,
      revision: 3
    };

    service.updateComment(request).subscribe();
    service.deleteComment(request.id, request.revision).subscribe();

    expect(httpClient.put).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}comments/comment%2F1`,
      {
        bodies: request.bodies,
        isOfficial: true,
        revision: 3
      },
      expect.objectContaining({
        headers: expect.anything()
      })
    );
    expect(httpClient.delete).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}comments/comment%2F1`,
      {
        params: {
          revision: '3'
        }
      }
    );
  });

  it('uploads managed comment images as multipart and deletes drafts through dedicated routes', () => {
    const httpClient = {
      post: vi.fn().mockReturnValue(of({
        id: '0123456789abcdef0123456789abcdef',
        url: '/images/0123456789abcdef0123456789abcdef'
      })),
      delete: vi.fn().mockReturnValue(of(undefined))
    };
    const service: CommentsApiService = new CommentsApiService(httpClient as unknown as HttpClient);
    const file: File = new File(['image'], 'coaster.png', { type: 'image/png' });

    service.uploadCommentImage(file).subscribe();
    service.deleteCommentImage('image/1').subscribe();

    const uploadCall: unknown[] = httpClient.post.mock.calls[0];
    expect(uploadCall[0]).toBe(`${environment.apiBaseUrl}comments/images`);
    expect(uploadCall[1]).toBeInstanceOf(FormData);
    const uploadedFile: File = (uploadCall[1] as FormData).get('file') as File;
    expect(uploadedFile.name).toBe(file.name);
    expect(uploadedFile.type).toBe(file.type);
    expect(httpClient.delete).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}comments/images/image%2F1`
    );
  });
});
