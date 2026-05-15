import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { USER_ADMIN_API_ENDPOINTS } from '@data-access/users/user-admin-api-endpoints';
import { AuthMessageResponse } from '@app/models/auth/auth-message-response';
import { UserLockRequest } from '@app/models/users/user-lock-request';
import { UserPasswordChange } from '@app/models/users/user-password-change';
import { UserRoleRequest } from '@app/models/users/user-role-request';
import { UserLockStateResponse, UserRolesUpdateResponse } from '@app/models/users/user-admin-responses';

@Injectable({
  providedIn: 'root'
})
export class UserAdminApiService {
  constructor(private readonly http: HttpClient) {
  }

  assignRoleToUser(id: string, payload: UserRoleRequest): Observable<UserRolesUpdateResponse> {
    const url: string = `${environment.apiBaseUrl}${USER_ADMIN_API_ENDPOINTS.assignRoleToUser(id)}`;
    return this.http.post<UserRolesUpdateResponse>(url, payload);
  }

  removeRoleFromUser(id: string, payload: UserRoleRequest): Observable<UserRolesUpdateResponse> {
    const url: string = `${environment.apiBaseUrl}${USER_ADMIN_API_ENDPOINTS.removeRoleFromUser(id)}`;
    return this.http.delete<UserRolesUpdateResponse>(url, { body: payload });
  }

  lockUser(payload: UserLockRequest): Observable<UserLockStateResponse> {
    const url: string = `${environment.apiBaseUrl}${USER_ADMIN_API_ENDPOINTS.lockUser}`;
    return this.http.post<UserLockStateResponse>(url, payload);
  }

  unlockUser(payload: UserLockRequest): Observable<UserLockStateResponse> {
    const url: string = `${environment.apiBaseUrl}${USER_ADMIN_API_ENDPOINTS.unlockUser}`;
    return this.http.post<UserLockStateResponse>(url, payload);
  }

  changeUserPassword(id: string, payload: UserPasswordChange): Observable<AuthMessageResponse> {
    const url: string = `${environment.apiBaseUrl}${USER_ADMIN_API_ENDPOINTS.changeUserPassword(id)}`;
    return this.http.post<AuthMessageResponse>(url, payload);
  }
}
