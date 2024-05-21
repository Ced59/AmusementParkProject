import { Injectable } from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {environment} from "../../environments/environment";
import {API_ENDPOINTS} from "../api/api-endpoints";
import {UserCredentials} from "../models/users/user_credentials";

@Injectable({
  providedIn: 'root'
})
export class ApiService {

  constructor(private http: HttpClient) { }

  login(credentials: UserCredentials) {
    const url = `${API_ENDPOINTS.postLogin}`;
    return this.http.post(url, credentials);
  }



  getUsers() {
    return this.http.get(`${environment.baseUrl}${API_ENDPOINTS.getUsers}`);
  }

  getUserById(id: string) {
    return this.http.get(`${environment.baseUrl}${API_ENDPOINTS.getUserById(id)}`);
  }
}
