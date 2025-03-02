import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from "@angular/common/http";
import {environment} from "../../environments/environment";
import {API_ENDPOINTS} from "../api/api-endpoints";
import {UserCredentials} from "../models/users/user_credentials";
import {Observable} from "rxjs";
import {UserToken} from "../models/users/user_token";
import {UserDto} from "../models/users/user_dto";
import {UserPut} from "../models/users/user_put";
import {ParksApiResponse} from "../models/parks/parks_api_response";
import {Park} from "../models/parks/park";

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




  getUsers() {
    return this.http.get(`${environment.apiBaseUrl}${API_ENDPOINTS.getUsers}`);
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
    return this.http.get<ParksApiResponse>(`${environment.apiBaseUrl}${API_ENDPOINTS.getParksPaginated(page, size)}`)
  }

  getParkById(id: string): Observable<Park> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getParkById(id)}`;
    return this.http.get<Park>(url);
  }
}
