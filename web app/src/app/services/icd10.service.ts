import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';

@Injectable({
  providedIn: 'root'
})
export class Icd10Service {

  private apiUrl = this.httpConfig.getApiUrl('Icd10');

  constructor(
    private http: HttpClient,
    private httpConfig: HttpConfigService
  ) {}

  getDsIcd10(
    pageNumber: number = 1,
    pageSize: number = 50,
    searchTerm: string = '',
    chiTiet: boolean = true
  ): Observable<any> {

    const body: any = {
      pageNumber,
      pageSize,
      searchTerm,
      chiTiet
    };

    return this.http.post<any>(
      `${this.apiUrl}/ds_icd10`,
      body,
      { headers: this.httpConfig.getHeaders() }
    );
  }

  // Lấy các node con của một node trong cây ICD-10.
  // maCha rỗng => lấy các chương gốc (22 chương).
  getCayCon(maCha: string = ''): Observable<any> {
    return this.http.post<any>(
      `${this.apiUrl}/cay_con`,
      { maCha },
      { headers: this.httpConfig.getHeaders() }
    );
  }

  // Tìm kiếm theo mã/tên, trả về các node khớp KÈM tổ tiên để dựng cây.
  timCay(searchTerm: string): Observable<any> {
    return this.http.post<any>(
      `${this.apiUrl}/tim_cay`,
      { searchTerm },
      { headers: this.httpConfig.getHeaders() }
    );
  }

  // Đồng bộ danh mục ICD-10 từ nguồn (icd.kcb.vn). Chỉ ADMIN.
  dongBoTuNguon(): Observable<any> {
    return this.http.post<any>(
      `${this.apiUrl}/DongBoTuNguon`,
      {},
      { headers: this.httpConfig.getHeaders() }
    );
  }
}
