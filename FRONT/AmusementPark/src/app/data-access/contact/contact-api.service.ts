import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  AdminContactGrievanceQuery,
  AdminContactGrievanceResponse,
  ContactGrievanceSubmission,
  SubmitContactGrievanceRequest
} from '@app/models/contact/contact-grievance.models';

@Injectable({
  providedIn: 'root'
})
export class ContactApiService {
  private readonly publicBaseUrl: string = `${environment.apiBaseUrl}contact/grievances`;
  private readonly adminBaseUrl: string = `${environment.apiBaseUrl}admin/contact/grievances`;

  constructor(private readonly http: HttpClient) {
  }

  submitGrievance(request: SubmitContactGrievanceRequest): Observable<ContactGrievanceSubmission> {
    return this.http.post<ContactGrievanceSubmission>(this.publicBaseUrl, request);
  }

  searchAdminGrievances(query: AdminContactGrievanceQuery): Observable<AdminContactGrievanceResponse> {
    const params: HttpParams = this.buildParams(query);
    return this.http.get<AdminContactGrievanceResponse>(this.adminBaseUrl, { params });
  }

  private buildParams(query: AdminContactGrievanceQuery): HttpParams {
    let params: HttpParams = new HttpParams()
      .set('page', query.page)
      .set('size', query.size);

    params = this.setOptionalParam(params, 'search', query.search);
    params = this.setOptionalParam(params, 'ipAddress', query.ipAddress);
    params = this.setOptionalParam(params, 'languageCode', query.languageCode);

    return params;
  }

  private setOptionalParam(params: HttpParams, key: string, value: string | null | undefined): HttpParams {
    if (!value || value.trim().length === 0) {
      return params;
    }

    return params.set(key, value.trim());
  }
}
