import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';

export interface LyDoNghi {
  id: number;
  ky_hieu: string;
  dien_giai: string;
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
export class LyDoNghiService {
  private apiUrl = this.httpConfig.getApiUrl('LyDoNghi');

  constructor(private http: HttpClient, private httpConfig: HttpConfigService) { }

  getDsLyDo(
    pageNumber: number = 1,
    pageSize: number = 10,
    searchTerm: string = ''
  ): Observable<PaginatedResponse<LyDoNghi>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString())
      .set('searchTerm', searchTerm);

    return this.http.get<PaginatedResponse<LyDoNghi>>(`${this.apiUrl}`, {
      headers: this.httpConfig.getHeaders(),
      params,
    });
  }

  getLyDoNghiById(id: number): Observable<LyDoNghi> {
    return this.http.get<LyDoNghi>(`${this.apiUrl}/${id}`, {
      headers: this.httpConfig.getHeaders(),
    });
  }

  createLyDoNghi(lyDo: Partial<LyDoNghi>): Observable<LyDoNghi> {
    return this.http.post<LyDoNghi>(`${this.apiUrl}`, lyDo, {
      headers: this.httpConfig.getHeaders(),
    });
  }

  updateLyDoNghi(id: number, lyDo: Partial<LyDoNghi>): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, lyDo, {
      headers: this.httpConfig.getHeaders(),
    });
  }

  getAllLyDo(): Observable<LyDoNghi[]> {
    return this.http.get<LyDoNghi[]>(`${this.apiUrl}/all`, {
      headers: this.httpConfig.getHeaders(),
    });
  }
}
