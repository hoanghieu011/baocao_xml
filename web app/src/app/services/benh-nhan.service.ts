import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';

@Injectable({
  providedIn: 'root'
})
export class BenhNhanService {

  private apiUrl = this.httpConfig.getApiUrl('Xml1');

  constructor(
    private http: HttpClient,
    private httpConfig: HttpConfigService
  ) {}

  getDsBenhNhan(
    pageNumber: number = 1,
    pageSize: number = 50,
    searchTerm: string = '',
    tuNgay?: Date,
    denNgay?: Date
  ): Observable<any> {

    const body: any = {
      pageNumber,
      pageSize,
      searchTerm
    };

    if (tuNgay) {
      body.tuNgay = tuNgay;
    }

    if (denNgay) {
      body.denNgay = denNgay;
    }

    return this.http.post<any>(
      `${this.apiUrl}/ds_benh_nhan`,
      body,
      { headers: this.httpConfig.getHeaders() }
    );
  }

}