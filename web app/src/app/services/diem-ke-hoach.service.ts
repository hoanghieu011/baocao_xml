import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';
export type UpdateDiemKeHoach = {
  diemKeHoachId: number,
  diemKeHoach: number,
  soBuoiTruc: number,
  diemTruc?: number,
  diemLayMau?: number,
  soBenhNhan?: number,
  officerType?: number,
  diemTrucCC?: number,
};

export type ThemDiemKeHoach = {
  diemKeHoach: number,
  soBuoiTruc: number,
  diemTruc?: number,
  diemLayMau?: number,
  soBenhNhan?: number,
  diemTrucCC?: number,
  thangNam: string,
  khoaId: number,
  bacSiId?: number,
  bacSi: string
};
@Injectable({
  providedIn: 'root'
})
export class DiemKeHoachService {

  private apiUrl = this.httpConfig.getApiUrl('DiemKeHoach');

  constructor(
    private http: HttpClient,
    private httpConfig: HttpConfigService
  ) {}

  getDsDiemKeHoach(
    pageNumber: number = 1,
    pageSize: number = 50,
    thangNam: string,
    khoaId: string,
    searchTerm: string = ''
  ): Observable<any> {

    const body: any = {
      pageNumber,
      pageSize,
      searchTerm,
      thangNam,
      khoaId
    };
    return this.http.post<any>(
      `${this.apiUrl}/ds_diemkehoach`,
      body,
      { headers: this.httpConfig.getHeaders() }
    );
  }

  updateDiemKeHoach(diemKeHoach: UpdateDiemKeHoach): Observable<any> {
    const body: any = {
      ...diemKeHoach,
    };
    return this.http.put<any>(
      `${this.apiUrl}/cap-nhat-diemkehoach`,
      body,
      { headers: this.httpConfig.getHeaders() }
    );
  }

  themDiemKeHoach(diemKeHoach: ThemDiemKeHoach): Observable<any> {
    const body: any = {
      ...diemKeHoach,
    };
    return this.http.post<any>(
      `${this.apiUrl}/them-moi-diemkehoach`,
      body,
      { headers: this.httpConfig.getHeaders() }
    );
  }
    
}