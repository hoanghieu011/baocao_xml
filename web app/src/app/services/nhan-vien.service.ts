import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';

export interface NhanVienDto {
  ten_bo_phan: string;
  ma_nv: string;
  full_name: string;
  vi_tri: string;
  role: string;
}

export interface PaginatedResponse<T> {
  totalCount: number;
  items: T[];
}

@Injectable({
  providedIn: 'root',
})
export class NhanVienService {
  private apiUrl = this.httpConfig.getApiUrl('NhanVien')

  constructor(private http: HttpClient, private httpConfig: HttpConfigService) { }

  getPagedNhanVien(searchTerm: string, pageNumber: number = 1, pageSize: number = 10): Observable<PaginatedResponse<NhanVienDto>> {
    let params = new HttpParams()
      .set('searchTerm', searchTerm)
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PaginatedResponse<NhanVienDto>>(`${this.apiUrl}/list-role`, { params, headers: this.httpConfig.getHeaders() });
  }
  importNhanVien(file: File): Observable<Blob> {
    const formData: FormData = new FormData();
    formData.append('file', file);

    return this.http.post(`${this.apiUrl}/ImportNhanVien`, formData, {
      headers: this.httpConfig.getHeadersForFileUpload(),
      responseType: 'blob'
    });
  }
  xuatDsNhanVien(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/xuat-ds-nhan-vien`, {
      responseType: 'blob',
      headers: this.httpConfig.getHeaders(),
    });
  }
  updateUserRole(ma_nv: string, role: string): Observable<void> {
    const updateRoleDto = { ma_nv: ma_nv, role: role };
    return this.http.put<void>(`${this.apiUrl}/update-role`, updateRoleDto, { headers: this.httpConfig.getHeaders() });
  }
  searchNhanVien(searchTerm: string, pageNumber: number = 1, pageSize: number = 10): Observable<any> {
    let params = new HttpParams()
      .set('searchTerm', searchTerm)
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<any>(this.apiUrl, { params, headers: this.httpConfig.getHeaders() });
  }
  updateNhanVien(id: number, nhanVienDto: UpdateNhanVienDto): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, nhanVienDto, { headers: this.httpConfig.getHeaders() });
  }
  createNhanVien(newNhanVien: any): Observable<CreatNhanVienResponse> {
    return this.http.post<CreatNhanVienResponse>(this.apiUrl, newNhanVien, { headers: this.httpConfig.getHeaders() });
  }
  deleteNhanVien(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`, { headers: this.httpConfig.getHeaders() });
  }
  getNhanVienDetail(ma_nv: string): Observable<any> {
    return this.http.get<NhanVienDto>(`${this.apiUrl}/nhan-vien_detail/${ma_nv}`, { headers: this.httpConfig.getHeaders() });
  }
  getNhanVienCungBoPhan(searchTerm: string = 'All', pageNumber: number = 1, pageSize: number = 10): Observable<any> {
    if (searchTerm === null || searchTerm === '') {
      searchTerm = 'All'
    }
    let params = new HttpParams()
      .set('searchTerm', searchTerm)
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<any>(`${this.apiUrl}/nhan_vien_cung_bo_phan`, {
      params,
      headers: this.httpConfig.getHeaders(),
    });
  }
  updateWorkingPositionFromExcel(file: File): Observable<Blob> {
    const formData: FormData = new FormData();
    formData.append('file', file);

    return this.http.post(`${this.apiUrl}/UpdateWorkingPositionFromExcel`, formData, {
      headers: this.httpConfig.getHeadersForFileUpload(),
      responseType: 'blob'
    });
  }
}
export interface UpdateNhanVienDto {
  full_name?: string;
  gioi_tinh?: string;
  bo_phan_id?: number;
  vi_tri?: string;
  cong_viec?: string;
}
export interface CreatNhanVienResponse {
  ma_nv: string;
  id: number;
  password: string;
}