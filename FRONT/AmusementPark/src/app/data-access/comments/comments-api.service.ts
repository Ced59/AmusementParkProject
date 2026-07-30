import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CommentSummary,
  CommentTargetType,
  CommentThread,
  CreateCommentRequest,
  PublicComment,
  UpdateCommentRequest
} from '@app/models/comments/comment.models';
import { CommentImageUpload } from '@app/models/comments/comment-image.models';
import { environment } from '../../../environments/environment';
import { COMMENTS_API_ENDPOINTS } from './comments-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class CommentsApiService {
  private readonly jsonHttpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  constructor(private readonly http: HttpClient) {
  }

  getSummary(targetType: CommentTargetType, targetId: string): Observable<CommentSummary> {
    const url: string = `${environment.apiBaseUrl}${COMMENTS_API_ENDPOINTS.getSummary(targetType, targetId)}`;
    return this.http.get<CommentSummary>(url);
  }

  getThread(targetType: CommentTargetType, targetId: string): Observable<CommentThread> {
    const url: string = `${environment.apiBaseUrl}${COMMENTS_API_ENDPOINTS.getThread(targetType, targetId)}`;
    return this.http.get<CommentThread>(url);
  }

  createComment(request: CreateCommentRequest): Observable<PublicComment> {
    const url: string = `${environment.apiBaseUrl}${COMMENTS_API_ENDPOINTS.create}`;
    return this.http.post<PublicComment>(url, request, this.jsonHttpOptions);
  }

  uploadCommentImage(file: File): Observable<CommentImageUpload> {
    const url: string = `${environment.apiBaseUrl}${COMMENTS_API_ENDPOINTS.uploadImage}`;
    const formData: FormData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<CommentImageUpload>(url, formData);
  }

  deleteCommentImage(imageId: string): Observable<void> {
    const url: string = `${environment.apiBaseUrl}${COMMENTS_API_ENDPOINTS.deleteImage(imageId)}`;
    return this.http.delete<void>(url);
  }

  updateComment(request: UpdateCommentRequest): Observable<PublicComment> {
    const url: string = `${environment.apiBaseUrl}${COMMENTS_API_ENDPOINTS.update(request.id)}`;
    return this.http.put<PublicComment>(
      url,
      {
        bodies: request.bodies,
        isOfficial: request.isOfficial,
        revision: request.revision
      },
      this.jsonHttpOptions
    );
  }

  deleteComment(commentId: string, revision: number): Observable<void> {
    const url: string = `${environment.apiBaseUrl}${COMMENTS_API_ENDPOINTS.delete(commentId)}`;
    return this.http.delete<void>(url, {
      params: {
        revision: revision.toString()
      }
    });
  }
}
