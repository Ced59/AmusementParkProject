import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { UserDto } from '@app/models/users/user_dto';
import { UserPut } from '@app/models/users/user_put';
import { UsersApiResponse } from '@app/models/users/users_api_response';
import { USERS_API_ENDPOINTS } from './users-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class UsersApiService {
  private readonly jsonHttpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  constructor(private readonly http: HttpClient) {
  }

  getUsers(page: number = 1, size: number = 10): Observable<UsersApiResponse> {
    const url: string = `${environment.apiBaseUrl}${USERS_API_ENDPOINTS.getUsers(page, size)}`;
    return this.http.get<UsersApiResponse>(url);
  }

  getUserById(id: string): Observable<UserDto> {
    const url: string = `${environment.apiBaseUrl}${USERS_API_ENDPOINTS.getUserById(id)}`;
    return this.http.get<UserDto>(url);
  }

  putUserById(id: string | null, user: UserPut | null): Observable<UserDto> {
    const url: string = `${environment.apiBaseUrl}${USERS_API_ENDPOINTS.putUserById(id)}`;
    return this.http.put<UserDto>(url, JSON.stringify(user), this.jsonHttpOptions);
  }
}
