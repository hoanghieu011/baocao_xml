import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';

export interface ViTri {
  id: number;
  ma_vi_tri: string;
  ten_vi_tri: string;
  ten_vi_tri_en: string;
}

export interface PaginatedResponse<T> {
  totalItems: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  items: T[];
}

@Injectable({
  providedIn: 'root',
})
export class ViTriService {
  private apiUrl = this.httpConfig.getApiUrl('ViTri');

  constructor(private http: HttpClient, private httpConfig: HttpConfigService) {}

  getDsViTri(
    pageNumber: number = 1,
    pageSize: number = 10,
    searchTerm: string = ''
  ): Observable<PaginatedResponse<ViTri>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString())
      .set('searchTerm', searchTerm);

    return this.http.get<PaginatedResponse<ViTri>>(`${this.apiUrl}`, {
      headers: this.httpConfig.getHeaders(),
      params,
    });
  }

  getViTriById(id: number): Observable<ViTri> {
    return this.http.get<ViTri>(`${this.apiUrl}/${id}`, {
      headers: this.httpConfig.getHeaders(),
    });
  }

  createViTri(viTri: ViTri): Observable<ViTri> {
    return this.http.post<ViTri>(`${this.apiUrl}`, viTri, {
      headers: this.httpConfig.getHeaders(),
    });
  }

  updateViTri(id: number, viTri: Partial<ViTri>): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, viTri, {
      headers: this.httpConfig.getHeaders(),
    });
  }

  getAllViTri(): Observable<ViTri[]> {
    return this.http.get<ViTri[]>(`${this.apiUrl}/all`, {
      headers: this.httpConfig.getHeaders(),
    });
  }
}
