import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';

@Injectable({
  providedIn: 'root'
})
export class OfficerService {

  private apiUrl = this.httpConfig.getApiUrl('Officer');

  constructor(
    private http: HttpClient,
    private httpConfig: HttpConfigService
  ) {}

  getDsOfficer(
    term: string = ''
  ): Observable<any> {

    let params = new HttpParams()
      .set('searchTerm', term)
  
    return this.http.get<any>(
      `${this.apiUrl}/ds_officer`,
      {
      params,
      headers: this.httpConfig.getHeaders(),
      });
  }

  getTtOfficer(
  ): Observable<any> {
  
    return this.http.get<any>(
      `${this.apiUrl}/tt_officer`,
      {
      headers: this.httpConfig.getHeaders(),
      });
  }
}