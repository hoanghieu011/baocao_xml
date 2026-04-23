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
}