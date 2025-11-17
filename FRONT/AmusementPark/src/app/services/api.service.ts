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
import {CountryDto} from "../models/countries/country-dto";
import {ParkLogoDto} from "../models/parks/park-logo";
import {UploadedImage} from "../models/images/uploaded-image";

@Injectable({
  providedIn: 'root'
})
export class ApiService {

  private get imagesBaseUrl(): string {
    return (environment as any).imagesBaseUrl ?? '';
  }

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

  createPark(park: Park) {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.createPark}`;
    const httpOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      })
    };

    return this.http.post<Park>(url, JSON.stringify(park), httpOptions);
  }

  updatePark(id: string, park: Park) {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.updatePark(id)}`;
    const httpOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      })
    };

    return this.http.put<Park>(url, JSON.stringify(park), httpOptions);
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

  getCountries(lang: string) {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getCountries(lang)}`;
    return this.http.get<CountryDto[]>(url);
  }

  uploadImage(file: File): Observable<UploadedImage> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.uploadImage}`;
    const formData = new FormData();
    formData.append('file', file); // adapte au nom attendu côté API

    return this.http.post<UploadedImage>(url, formData);
  }

  createParkLogo(parkId: string, imageId: string, description?: string): Observable<ParkLogoDto> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.createParkLogo(parkId)}`;
    const body = { imageId, description };
    return this.http.post<ParkLogoDto>(url, body);
  }

  getParkLogos(parkId: string): Observable<ParkLogoDto[]> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getParkLogos(parkId)}`;
    return this.http.get<ParkLogoDto[]>(url);
  }

  setCurrentParkLogo(parkId: string, logoId: string): Observable<ParkLogoDto> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.setCurrentParkLogo(parkId, logoId)}`;
    return this.http.put<ParkLogoDto>(url, {});
  }

  deleteParkLogo(parkId: string, logoId: string): Observable<boolean> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.deleteParkLogo(parkId, logoId)}`;
    return this.http.delete<boolean>(url);
  }

  /**
   * Construit l'URL d'affichage d'un logo
   * (basée sur la convention imageId -> fichier)
   */
  buildLogoUrl(imageId: string): string {
    if (!this.imagesBaseUrl) {
      return '';
    }
    // adapte l'extension si besoin (.webp, .jpg, etc.)
    return `${this.imagesBaseUrl}/${imageId}.webp`;
  }


}
