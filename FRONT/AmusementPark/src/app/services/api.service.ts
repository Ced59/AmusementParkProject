import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { map, Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { API_ENDPOINTS } from '../api/api-endpoints';

import { UserCredentials } from '../models/users/user_credentials';
import { UserToken } from '../models/users/user_token';
import { UserDto } from '../models/users/user_dto';
import { UserRegister } from '../models/users/user-register';
import { UserPut } from '../models/users/user_put';
import { UsersApiResponse } from '../models/users/users_api_response';

import { ParksApiResponse } from '../models/parks/parks_api_response';
import { Park } from '../models/parks/park';
import { ParkFounder } from '../models/parks/park-founder';
import { ParkOperator } from '../models/parks/park-operator';
import { AttractionManufacturer } from '../models/parks/attraction-manufacturer';
import { ParkZone } from '../models/parks/park-zone';
import { ParkItem } from '../models/parks/park-item';
import { ParkItemAdminRow } from '../models/parks/park-item-admin-row';
import { ParkExplorer } from '../models/parks/park-explorer';

import { SearchApiResponse } from '../models/search/search-api-response';
import { ApiResponse } from '../models/shared/api_reponse';
import { PaginationContract } from '@shared/models/contracts';
import { coalesceArray, mapArray } from '@shared/utils/mapping';
import { CountryDto } from '../models/countries/country-dto';

import { UploadedImage } from '../models/images/uploaded-image';
import { ImageCategory } from '../models/images/image-category';
import { ImageDto } from '../models/images/image-dto';
import { ImageOwnerType } from '../models/images/image-owner-type';
import { LinkImageToOwner } from '../models/images/link-image-to-owner';
import { ImageTagDto } from '../models/images/image-tag-dto';
import { LocalizedItemDto } from '../models/shared/localized-item-dto';
import { ImageGeoLocation } from '../models/images/image-geo-location';
import { AuthMessageResponse } from '../models/auth/auth-message-response';

interface PagedCollectionResponse<T> {
  data?: T[];
  pagination?: PaginationContract | null;
}


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


  register(request: UserRegister): Observable<UserDto> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.postRegister}`;
    return this.http.post<UserDto>(url, request);
  }

  confirmEmail(token: string): Observable<AuthMessageResponse> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.confirmEmail}`;
    return this.http.post<AuthMessageResponse>(url, { token });
  }

  resendConfirmation(email: string): Observable<AuthMessageResponse> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.resendConfirmation}`;
    return this.http.post<AuthMessageResponse>(url, { email });
  }

  forgotPassword(email: string): Observable<AuthMessageResponse> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.forgotPassword}`;
    return this.http.post<AuthMessageResponse>(url, { email });
  }

  resetPassword(token: string, newPassword: string, newPasswordConfirm: string): Observable<AuthMessageResponse> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.resetPassword}`;
    return this.http.post<AuthMessageResponse>(url, { token, newPassword, newPasswordConfirm });
  }
  externalLogin(provider: string, token: string, nonce?: string): Observable<UserToken> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.externalLogin(provider)}`;
    return this.http.post<UserToken>(url, { token, nonce });
  }

  googleLogin(token: string): Observable<UserToken> {
    return this.externalLogin('google', token);
  }

  getUsers(page: number = 1, size: number = 10): Observable<UsersApiResponse> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getUsers(page, size)}`;
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
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getParksByLocation(latitude, longitude, radius)}`;
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

  getParkFounders(): Observable<ParkFounder[]> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getParkFounders}`;
    return this.http.get<ParkFounder[] | PagedCollectionResponse<ParkFounder>>(url).pipe(
      map((response: ParkFounder[] | PagedCollectionResponse<ParkFounder>) => this.unwrapCollection<ParkFounder>(response))
    );
  }

  getParkFounderById(id: string): Observable<ParkFounder> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getParkFounderById(id)}`;
    return this.http.get<ParkFounder>(url);
  }

  createParkFounder(founder: ParkFounder): Observable<ParkFounder> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.createParkFounder}`;
    const httpOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      })
    };

    return this.http.post<ParkFounder>(url, JSON.stringify(founder), httpOptions);
  }

  updateParkFounder(id: string, founder: ParkFounder): Observable<ParkFounder> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.updateParkFounder(id)}`;
    const httpOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      })
    };

    return this.http.put<ParkFounder>(url, JSON.stringify(founder), httpOptions);
  }

  getParkOperators(): Observable<ParkOperator[]> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getParkOperators}`;
    return this.http.get<ParkOperator[] | PagedCollectionResponse<ParkOperator>>(url).pipe(
      map((response: ParkOperator[] | PagedCollectionResponse<ParkOperator>) => this.unwrapCollection<ParkOperator>(response))
    );
  }

  getParkOperatorById(id: string): Observable<ParkOperator> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getParkOperatorById(id)}`;
    return this.http.get<ParkOperator>(url);
  }

  createParkOperator(parkOperator: ParkOperator): Observable<ParkOperator> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.createParkOperator}`;
    const httpOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      })
    };

    return this.http.post<ParkOperator>(url, JSON.stringify(parkOperator), httpOptions);
  }

  updateParkOperator(id: string, parkOperator: ParkOperator): Observable<ParkOperator> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.updateParkOperator(id)}`;
    const httpOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      })
    };

    return this.http.put<ParkOperator>(url, JSON.stringify(parkOperator), httpOptions);
  }

  getAttractionManufacturers(): Observable<AttractionManufacturer[]> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getAttractionManufacturers}`;
    return this.http.get<AttractionManufacturer[] | PagedCollectionResponse<AttractionManufacturer>>(url).pipe(
      map((response: AttractionManufacturer[] | PagedCollectionResponse<AttractionManufacturer>) => this.unwrapCollection<AttractionManufacturer>(response))
    );
  }

  getAttractionManufacturerById(id: string): Observable<AttractionManufacturer> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getAttractionManufacturerById(id)}`;
    return this.http.get<AttractionManufacturer>(url);
  }

  createAttractionManufacturer(manufacturer: AttractionManufacturer): Observable<AttractionManufacturer> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.createAttractionManufacturer}`;
    const httpOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      })
    };

    return this.http.post<AttractionManufacturer>(url, JSON.stringify(manufacturer), httpOptions);
  }

  updateAttractionManufacturer(id: string, manufacturer: AttractionManufacturer): Observable<AttractionManufacturer> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.updateAttractionManufacturer(id)}`;
    const httpOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      })
    };

    return this.http.put<AttractionManufacturer>(url, JSON.stringify(manufacturer), httpOptions);
  }

  getParkZonesByParkId(parkId: string): Observable<ParkZone[]> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getParkZonesByParkId(parkId)}`;
    return this.http.get<ParkZone[] | PagedCollectionResponse<ParkZone>>(url).pipe(
      map((response: ParkZone[] | PagedCollectionResponse<ParkZone>) => this.unwrapCollection<ParkZone>(response))
    );
  }

  getParkZoneById(id: string): Observable<ParkZone> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getParkZoneById(id)}`;
    return this.http.get<ParkZone>(url);
  }

  createParkZone(zone: ParkZone): Observable<ParkZone> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.createParkZone}`;
    return this.http.post<ParkZone>(url, zone);
  }

  updateParkZone(id: string, zone: ParkZone): Observable<ParkZone> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.updateParkZone(id)}`;
    return this.http.put<ParkZone>(url, zone);
  }

  deleteParkZone(id: string): Observable<boolean> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.deleteParkZone(id)}`;
    return this.http.delete<boolean>(url);
  }

  getParkExplorer(parkId: string): Observable<ParkExplorer> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getParkExplorer(parkId)}`;
    return this.http.get<ParkExplorer>(url);
  }

  getParkItemsByParkId(parkId: string): Observable<ParkItem[]> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getParkItemsByParkId(parkId)}`;
    return this.http.get<ParkItem[] | PagedCollectionResponse<ParkItem>>(url).pipe(
      map((response: ParkItem[] | PagedCollectionResponse<ParkItem>) => this.unwrapCollection<ParkItem>(response).map((item: ParkItem) => this.normalizeParkItem(item)))
    );
  }

  getParkItemsPaginated(
    page: number,
    size: number,
    parkId?: string | null,
    search?: string | null
  ): Observable<ApiResponse<ParkItemAdminRow>> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getParkItemsPaginated(page, size, parkId, search)}`;
    return this.http.get<ApiResponse<ParkItemAdminRow>>(url).pipe(
      map((response: ApiResponse<ParkItemAdminRow>) => ({
        ...response,
        data: mapArray(response.data, (row: ParkItemAdminRow) => this.normalizeParkItemAdminRow(row))
      }))
    );
  }

  getParkItemById(id: string): Observable<ParkItem> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getParkItemById(id)}`;
    return this.http.get<ParkItem>(url).pipe(
      map((item: ParkItem) => this.normalizeParkItem(item))
    );
  }

  createParkItem(item: ParkItem): Observable<ParkItem> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.createParkItem}`;
    return this.http.post<ParkItem>(url, item);
  }

  updateParkItem(id: string, item: ParkItem): Observable<ParkItem> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.updateParkItem(id)}`;
    return this.http.put<ParkItem>(url, item);
  }

  deleteParkItem(id: string): Observable<boolean> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.deleteParkItem(id)}`;
    return this.http.delete<boolean>(url);
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
    return this.http.get<CountryDto[] | PagedCollectionResponse<CountryDto>>(url).pipe(
      map((response: CountryDto[] | PagedCollectionResponse<CountryDto>) => this.unwrapCollection<CountryDto>(response))
    );
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
    formData.append('Category', String(this.toImageCategoryApiValue(category)));
    formData.append('WithWatermark', String(withWatermark));

    if (description) {
      formData.append('Description', description);
    }

    return this.http.post<UploadedImage>(url, formData);
  }

  linkImage(request: LinkImageToOwner): Observable<ImageDto> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.linkImage}`;
    return this.http.post<ImageDto>(url, {
      ...request,
      ownerType: this.toImageOwnerTypeApiValue(request.ownerType)
    });
  }

  getImages(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory): Observable<ImageDto[]> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getImages(ownerType, ownerId, category)}`;
    return this.http.get<ImageDto[] | PagedCollectionResponse<ImageDto>>(url).pipe(
      map((response: ImageDto[] | PagedCollectionResponse<ImageDto>) => this.unwrapCollection<ImageDto>(response))
    );
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

  resolveImageUrl(imagePathOrUrl?: string | null): string | null {
    if (!imagePathOrUrl) {
      return null;
    }

    if (/^https?:\/\//i.test(imagePathOrUrl)) {
      return imagePathOrUrl;
    }

    if (imagePathOrUrl.startsWith('/images/')) {
      const imageId: string = imagePathOrUrl.replace(/^\/images\//, '');
      return this.buildImageUrl(imageId);
    }

    const normalizedPath: string = imagePathOrUrl.replace(/^\/+/, '');
    return `${environment.apiBaseUrl}${normalizedPath}`;
  }

  getAdminImages(): Observable<ImageDto[]> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getAdminImages}`;
    return this.http.get<ImageDto[] | PagedCollectionResponse<ImageDto>>(url).pipe(
      map((response: ImageDto[] | PagedCollectionResponse<ImageDto>) => this.unwrapCollection<ImageDto>(response))
    );
  }

  updateAdminImage(id: string, request: {
    description?: string;
    geoLocation?: ImageGeoLocation | null;
    altTexts: LocalizedItemDto<string>[];
    captions: LocalizedItemDto<string>[];
    credits: LocalizedItemDto<string>[];
    tagIds: string[];
    isPublished: boolean;
  }): Observable<ImageDto> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.updateAdminImage(id)}`;
    return this.http.put<ImageDto>(url, request);
  }

  getAdminImageTags(): Observable<ImageTagDto[]> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.getAdminImageTags}`;
    return this.http.get<ImageTagDto[] | PagedCollectionResponse<ImageTagDto>>(url).pipe(
      map((response: ImageTagDto[] | PagedCollectionResponse<ImageTagDto>) => this.unwrapCollection<ImageTagDto>(response))
    );
  }

  createAdminImageTag(request: { slug: string; labels: LocalizedItemDto<string>[]; descriptions: LocalizedItemDto<string>[]; }): Observable<ImageTagDto> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.createAdminImageTag}`;
    return this.http.post<ImageTagDto>(url, request);
  }

  updateAdminImageTag(id: string, request: { slug: string; labels: LocalizedItemDto<string>[]; descriptions: LocalizedItemDto<string>[]; isActive: boolean; }): Observable<ImageTagDto> {
    const url = `${environment.apiBaseUrl}${API_ENDPOINTS.updateAdminImageTag(id)}`;
    return this.http.put<ImageTagDto>(url, request);
  }

  private unwrapCollection<T>(response: T[] | PagedCollectionResponse<T> | null | undefined): T[] {
    if (Array.isArray(response)) {
      return response;
    }

    if (response && Array.isArray(response.data)) {
      return coalesceArray(response.data);
    }

    return [];
  }

  private normalizeParkItem(item: ParkItem): ParkItem {
    return {
      ...item,
      category: this.toParkItemCategory(item.category),
      type: this.toParkItemType(item.type)
    };
  }

  private normalizeParkItemAdminRow(row: ParkItemAdminRow): ParkItemAdminRow {
    return {
      ...row,
      category: this.toParkItemCategory(row.category),
      type: this.toParkItemType(row.type)
    };
  }

  private toParkItemCategory(value: ParkItem['category'] | ParkItemAdminRow['category'] | number | null | undefined): ParkItem['category'] {
    if (typeof value === 'string') {
      return value as ParkItem['category'];
    }

    switch (value) {
      case 0:
        return 'Attraction';
      case 1:
        return 'Restaurant';
      case 2:
        return 'Hotel';
      case 3:
        return 'Animal';
      case 4:
        return 'Show';
      case 5:
        return 'Shop';
      case 6:
        return 'Service';
      case 7:
        return 'Transport';
      default:
        return 'Other';
    }
  }

  private toParkItemType(value: ParkItem['type'] | ParkItemAdminRow['type'] | number | null | undefined): ParkItem['type'] {
    if (typeof value === 'string') {
      return value as ParkItem['type'];
    }

    switch (value) {
      case 0:
        return 'Attraction';
      case 1:
        return 'RollerCoaster';
      case 2:
        return 'WaterRide';
      case 3:
        return 'FlatRide';
      case 4:
        return 'DarkRide';
      case 5:
        return 'FamilyRide';
      case 6:
        return 'ThrillRide';
      case 7:
        return 'TransportRide';
      case 8:
        return 'WalkThrough';
      case 9:
        return 'Playground';
      case 10:
        return 'InteractiveExperience';
      case 11:
        return 'ObservationRide';
      case 12:
        return 'AnimalExhibit';
      case 13:
        return 'Restaurant';
      case 14:
        return 'Snack';
      case 15:
        return 'Hotel';
      case 16:
        return 'Show';
      case 17:
        return 'Shop';
      case 18:
        return 'Service';
      case 19:
        return 'Transport';
      default:
        return 'Other';
    }
  }

  private toImageOwnerTypeApiValue(value: ImageOwnerType): number {
    switch (value) {
      case ImageOwnerType.PARK:
        return 1;
      case ImageOwnerType.USER:
        return 2;
      case ImageOwnerType.ATTRACTION:
        return 3;
      default:
        return 0;
    }
  }

  private toImageCategoryApiValue(value: ImageCategory): number {
    switch (value) {
      case ImageCategory.AVATAR:
        return 0;
      case ImageCategory.PARK_LOGO:
        return 1;
      case ImageCategory.PARK:
        return 2;
      case ImageCategory.ATTRACTION:
        return 3;
      default:
        return 2;
    }
  }
}
