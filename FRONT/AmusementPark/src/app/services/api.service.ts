import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { API_ENDPOINTS } from '../api/api-endpoints';

import { UserCredentials } from '../models/users/user_credentials';
import { UserToken } from '../models/users/user_token';
import { UserDto } from '../models/users/user_dto';
import { UserPut } from '../models/users/user_put';
import { UsersApiResponse } from '../models/users/users_api_response';

import { ParksApiResponse } from '../models/parks/parks_api_response';
import { Park } from '../models/parks/park';

import { SearchApiResponse } from '../models/search/search-api-response';
import { CountryDto } from '../models/countries/country-dto';

import { UploadedImage } from '../models/images/uploaded-image';
import { ImageCategory } from '../models/images/image-category';
import { ImageDto } from '../models/images/image-dto';
import { ImageOwnerType } from '../models/images/image-owner-type';
import { LinkImageToOwner } from '../models/images/link-image-to-owner';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  constructor(private readonly http: HttpClient) {
  }

  login(credentials: UserCredentials): Observable<UserToken> {
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

  getUsers(page: number = 1, size: number = 10): Observable<UsersApiResponse> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getUsers}?page=${page}&size=${size}`;
    return this.http.get<UsersApiResponse>(url);
  }

  getUserById(id: string): Observable<UserDto> {
    return this.http.get<UserDto>(`${environment.apiBaseUrl}${API_ENDPOINTS.getUserById(id)}`);
  }

  putUserById(id: string | null, user: UserPut | null): Observable<UserDto> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.putUserById(id)}`;
    const httpOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      })
    };

    return this.http.put<UserDto>(url, JSON.stringify(user), httpOptions);
  }

  getParksPaginated(page: number, size: number): Observable<ParksApiResponse> {
    return this.http.get<ParksApiResponse>(
      `${environment.apiBaseUrl}${API_ENDPOINTS.getParksPaginated(page, size)}`
    );
  }

  getParkById(id: string): Observable<Park> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getParkById(id)}`;
    return this.http.get<Park>(url);
  }

  searchParks(name: string, page: number, size: number): Observable<ParksApiResponse> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.searchParks(name, page, size)}`;
    return this.http.get<ParksApiResponse>(url);
  }

  getParksByLocation(latitude: number, longitude: number, radius: number): Observable<Park[]> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.searchParksByLocation(latitude, longitude, radius)}`;
    return this.http.get<Park[]>(url);
  }

  updateParkVisibility(parkId: string, isVisible: boolean): Observable<Park> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.updateParkVisibility(parkId)}`;
    const body = { isVisible };
    return this.http.patch<Park>(url, body);
  }

  createPark(park: Park): Observable<Park> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.createPark}`;
    const httpOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      })
    };

    return this.http.post<Park>(url, JSON.stringify(park), httpOptions);
  }

  updatePark(id: string, park: Park): Observable<Park> {
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

  getCountries(lang: string): Observable<CountryDto[]> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getCountries(lang)}`;
    return this.http.get<CountryDto[]>(url);
  }

  uploadImage(
    file: File,
    category: ImageCategory,
    withWatermark: boolean = true,
    description?: string
  ): Observable<UploadedImage> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.uploadImage}`;
    const formData = new FormData();

    formData.append('File', file);
    formData.append('Category', category);
    formData.append('WithWatermark', String(withWatermark));

    if (description) {
      formData.append('Description', description);
    }

    return this.http.post<UploadedImage>(url, formData);
  }

  linkImage(request: LinkImageToOwner): Observable<ImageDto> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.linkImage}`;
    return this.http.post<ImageDto>(url, request);
  }

  getImages(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory): Observable<ImageDto[]> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getImages(ownerType, ownerId, category)}`;
    return this.http.get<ImageDto[]>(url);
  }

  getCurrentImage(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory): Observable<ImageDto> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getCurrentImage(ownerType, ownerId, category)}`;
    return this.http.get<ImageDto>(url);
  }

  setCurrentImage(imageId: string): Observable<ImageDto> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.setCurrentImage(imageId)}`;
    return this.http.put<ImageDto>(url, {});
  }

  deleteImage(imageId: string): Observable<boolean> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.deleteImage(imageId)}`;
    return this.http.delete<boolean>(url);
  }

  buildImageUrl(imageId: string): string {
    return `${environment.imagesBaseUrl}/${imageId}`;
  }
}
