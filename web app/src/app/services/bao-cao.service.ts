import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';

@Injectable({
  providedIn: 'root'
})
export class BaoCaoService {
  private apiUrl = this.httpConfig.getApiUrl('BaoCao')
  constructor(private http: HttpClient, private httpConfig: HttpConfigService) { }

  getBcDoanhThuBscd(
    maBacSy: string = '',
    tuNgay?: Date,
    denNgay?: Date
  ): Observable<any> {

    const body: any = {
      tuNgay,
      denNgay,
      maBacSy
    };

    return this.http.post<any>(
      `${this.apiUrl}/bc_doanhthu_bscd`,
      body,
      { headers: this.httpConfig.getHeaders() }
    );
  }

  exportBcDoanhThuBscdExcel(
    maBacSy: string = '',
    tuNgay?: Date,
    denNgay?: Date
  ): Observable<Blob> {

    const body: any = {
      tuNgay,
      denNgay,
      maBacSy
    };

    return this.http.post(
      `${this.apiUrl}/bc_doanhthu_bscd_excel`,
      body,
      {
        headers: this.httpConfig.getHeaders(),
        responseType: 'blob'
      }
    );
  }

  xuatBaoCaoNghiPhep(request: { tuNgay: string; denNgay: string }): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/xuat-bao-cao-nghi-phep`, request, {
      headers: this.httpConfig.getHeaders(),
      responseType: 'blob',
    });
  }
  getBaoCaoNghiPhep(request: BaoCaoRequest): Observable<{ totalCount: number, items: any[] }> {
    return this.http.post<{ totalCount: number, items: any[] }>(
      `${this.apiUrl}/XuatBaoCaoNghiPhepJson`, 
      request, 
      { headers: this.httpConfig.getHeaders() }
    );
  }

  getBaoCaoNghiPhepBoPhan(request: BaoCaoRequest): Observable<{ totalCount: number, items: any[] }> {
    return this.http.post<{ totalCount: number, items: any[] }>(
      `${this.apiUrl}/XuatBaoCaoNghiPhepBoPhanJson`, 
      request, 
      { headers: this.httpConfig.getHeaders() }
    );
  }
  xuatBaoCaoNghiPhepBoPhan(request: { tuNgay: string; denNgay: string }): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/xuat-bao-cao-nghi-phep-bo-phan`, request, {
      headers: this.httpConfig.getHeaders(),
      responseType: 'blob',
    });
  }

  getYears(): Observable<number[]> {
    return this.http.get<number[]>(`${this.apiUrl}/years`, { headers: this.httpConfig.getHeaders() });
  }

  getQuarters(): Observable<{ year: number, quarter: number }[]> {
    return this.http.get<{ year: number, quarter: number }[]>(`${this.apiUrl}/quarters`, { headers: this.httpConfig.getHeaders() });
  }

  getMonths(): Observable<{ year: number, month: number }[]> {
    return this.http.get<{ year: number, month: number }[]>(`${this.apiUrl}/months`, { headers: this.httpConfig.getHeaders() });
  }
  getCount(timeFrame: string, inputDate: Date): Observable<any> {
    const params = new HttpParams()
      .set('TimeFrame', timeFrame)
      .set('InputDate', inputDate.toISOString());

    return this.http.get(`${this.apiUrl}/count`, { params, headers: this.httpConfig.getHeaders() });
  }
  getLeaveDaysCount(timeFrame: string, inputDate: Date): Observable<any> {
    const params = new HttpParams()
      .set('TimeFrame', timeFrame)
      .set('InputDate', inputDate.toISOString());

    return this.http.get(`${this.apiUrl}/leave-days-count`, { params, headers: this.httpConfig.getHeaders() });
  }
  getLeaveSummaryByDepartment(
    TimeFrame: string,
    InputDate: Date,
    searchTerm: string = 'Nội dung tìm kiếm',
    PageNumber: number = 1,
    PageSize: number = 10
  ): Observable<any> {
    const params = new HttpParams()
      .set('TimeFrame', TimeFrame)
      .set('InputDate', InputDate.toISOString())
      .set('searchTerm', searchTerm)
      .set('PageNumber', PageNumber.toString())
      .set('PageSize', PageSize.toString());

    return this.http.get(`${this.apiUrl}/bao-cao-bo-phan`, { params, headers: this.httpConfig.getHeaders() });
  }
  getLeaveSummaryByEmployee(
    TimeFrame: string,
    InputDate: Date,
    searchTerm: string = 'Nội dung tìm kiếm',
    PageNumber: number = 1,
    PageSize: number = 10
  ): Observable<any> {
    const params = new HttpParams()
      .set('TimeFrame', TimeFrame)
      .set('InputDate', InputDate.toISOString())
      .set('searchTerm', searchTerm)
      .set('PageNumber', PageNumber.toString())
      .set('PageSize', PageSize.toString());

    return this.http.get(`${this.apiUrl}/bao-cao-nhan-vien`, { params, headers: this.httpConfig.getHeaders() });
  }
}
export interface BaoCaoRequest {
  TuNgay: Date;    
  DenNgay: Date;  
  Page: number;
  PageSize: number;
  searchTerm: string;
}
