import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';

@Injectable({
  providedIn: 'root'
})
export class DichVuService {

  private apiUrl = this.httpConfig.getApiUrl('DichVu');

  constructor(
    private http: HttpClient,
    private httpConfig: HttpConfigService
  ) {}

  getDsDichVu(
    pageNumber: number = 1,
    pageSize: number = 50,
    searchTerm: string = '',
    idLoaiDV?: number
  ): Observable<any> {

    const body: any = {
      pageNumber,
      pageSize,
      searchTerm
    };

    if (idLoaiDV && idLoaiDV !== 0) {
      body.idLoaiDV = idLoaiDV;
    }

    return this.http.post<any>(
      `${this.apiUrl}/ds_dichvu`,
      body,
      { headers: this.httpConfig.getHeaders() }
    );
  }

  updateDichVu(
    dichVuId: number,
    chiPhi: number,
    heSo: number,
  ): Observable<any> {

    const body: any = {
      dichVuId,
      chiPhi,
      heSo
    };

    return this.http.put<any>(
      `${this.apiUrl}/cap-nhat-dichvu`,
      body,
      { headers: this.httpConfig.getHeaders() }
    );
  }
}