import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { environment } from "../../environments/environment";
import { API_ENDPOINTS } from "../api/api-endpoints";
import { UserCredentials } from "../models/users/user_credentials";
import { Observable } from "rxjs";
import { UserToken } from "../models/users/user_token";
import { UserDto } from "../models/users/user_dto";
import { UserPut } from "../models/users/user_put";
import { ParksApiResponse } from "../models/parks/parks_api_response";
import { Park } from "../models/parks/park";
import { SearchApiResponse } from "../models/search/search-api-response";
import {UsersApiResponse} from "../models/users/users_api_response";

@Injectable({
  providedIn: 'root'
})
export class ApiService {

  constructor(private http: HttpClient) { }

  login(credentials: UserCredentials) : Observable<UserToken> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.postLogin}`;
    const httpOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      })
    };
    return this.http.post<UserToken>(url, JSON.stringify(credentials), httpOptions);
  }

  googleLogin(code: string): Observable<any> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.googleLogin}`;
    return this.http.post(url, { code });
  }

  getUsers(page: number = 1, size: number = 10) {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getUsers}?page=${page}&size=${size}`;
    return this.http.get<UsersApiResponse>(url);
  }

  getUserById(id: string) {
    return this.http.get<UserDto>(`${environment.apiBaseUrl}${API_ENDPOINTS.getUserById(id)}`);
  }

  putUserById(id: string | null, user: UserPut | null){
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.putUserById(id)}`;
    const httpOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      })
    };

    return this.http.put<UserDto>(url, JSON.stringify(user), httpOptions)
  }

  getParksPaginated(page: number, size: number) {
    return this.http.get<ParksApiResponse>(
      `${environment.apiBaseUrl}${API_ENDPOINTS.getParksPaginated(page, size)}`
    );
  }

  getParkById(id: string): Observable<Park> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getParkById(id)}`;
    return this.http.get<Park>(url);
  }

  searchParks(name: string, page: number, size: number) {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.searchParks(name, page, size)}`;
    return this.http.get<ParksApiResponse>(url);
  }

  updateParkVisibility(parkId: string, isVisible: boolean) {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.updateParkVisibility(parkId)}`;
    const body = { isVisible };
    return this.http.patch<Park>(url, body);
  }

  getSearch(
    query: string,
    categories: string[],
    page: number,
    size: number
  ): Observable<SearchApiResponse> {
    const url =
      `${environment.apiBaseUrl}${API_ENDPOINTS.getSearch(query, categories, page, size)}`;
    return this.http.get<SearchApiResponse>(url);
  }
}
