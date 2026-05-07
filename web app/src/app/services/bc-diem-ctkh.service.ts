import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';

@Injectable({
  providedIn: 'root'
})
export class BaoCaoDiemCtkhService {

  private apiUrl = this.httpConfig.getApiUrl('BaoCao');

  constructor(
    private http: HttpClient,
    private httpConfig: HttpConfigService
  ) {}

  getDsDiemCtkh(
    tuThang: number,
    tuNam: number,
    denThang: number,
    denNam: number,

  ): Observable<any> {
    let body: any = {
      tuThang,
      tuNam,
      denThang,
      denNam
    };
    return this.http.post<any>(
      `${this.apiUrl}/bc_diem_ctkh`,
      body,
      { headers: this.httpConfig.getHeaders() }
    );
  }

  exportExcel(
    tuThang: number,
    tuNam: number,
    denThang: number,
    denNam: number,
    loaiBaoCao: 0|1
  ): Observable<Blob> {
    let body: any = {
      tuThang,
      tuNam,
      denThang,
      denNam,
      loaiBaoCao
    };
    return this.http.post(
      `${this.apiUrl}/bc_diem_ctkh_excel`,
      body,
      { headers: this.httpConfig.getHeaders(), responseType: 'blob' }
    );
  }
}