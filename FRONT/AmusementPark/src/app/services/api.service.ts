import { Injectable } from '@angular/core';
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {environment} from "../../environments/environment";
import {API_ENDPOINTS} from "../api/api-endpoints";
import {UserCredentials} from "../models/users/user_credentials";
import {Observable} from "rxjs";
import {UserToken} from "../models/users/user_token";

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




  getUsers() {
    return this.http.get(`${environment.apiBaseUrl}${API_ENDPOINTS.getUsers}`);
  }

  getUserById(id: string) {
    return this.http.get(`${environment.apiBaseUrl}${API_ENDPOINTS.getUserById(id)}`);
  }


  getParksPaginated(page: number, size: number) {
    return this.http.get(`${environment.apiBaseUrl}${API_ENDPOINTS.getParksPaginated(page, size)}`)
  }
}
