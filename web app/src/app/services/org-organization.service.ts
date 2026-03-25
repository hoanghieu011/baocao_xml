import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';

@Injectable({
  providedIn: 'root'
})
export class OrganizationService {

  private apiUrl = this.httpConfig.getApiUrl('Organization');

  constructor(
    private http: HttpClient,
    private httpConfig: HttpConfigService
  ) {}

  getDsOrganization(
    term: string = ''
  ): Observable<any> {

    let params = new HttpParams()
      .set('searchTerm', term)
  
    return this.http.get<any>(
      `${this.apiUrl}/ds_organization`,
      {
      params,
      headers: this.httpConfig.getHeaders(),
      });
  }
}