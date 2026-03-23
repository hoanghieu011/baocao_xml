import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';

@Injectable({
  providedIn: 'root'
})
export class LoaiDichVuService {

  private apiUrl = this.httpConfig.getApiUrl('LoaiDichVu');

  constructor(
    private http: HttpClient,
    private httpConfig: HttpConfigService
  ) {}

  getDsLoaiDichVu(
  ): Observable<any> {
    return this.http.get<any>(
      `${this.apiUrl}/ds_loaidichvu`,
      { headers: this.httpConfig.getHeaders() }
    );
  }
}