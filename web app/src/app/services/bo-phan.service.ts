import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service'

@Injectable({
  providedIn: 'root'
})
export class BoPhanService {
  private apiUrl = this.httpConfig.getApiUrl('BoPhan')
  constructor(private http: HttpClient, private httpConfig: HttpConfigService) { }

  getBoPhans(pageNumber: number = 1, pageSize: number = 10, searchTerm: string = 'Nội dung tìm kiếm'): Observable<any> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize)
      .set('searchTerm', searchTerm);

    return this.http.get<any>(this.apiUrl, { params, headers: this.httpConfig.getHeaders() });
  }

  getBoPhanById(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`, { headers: this.httpConfig.getHeaders() });
  }

  updateBoPhan(id: number, ten_bo_phan: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, { ten_bo_phan }, { headers: this.httpConfig.getHeaders() });
  }

  getAllBoPhan(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/all`, { headers: this.httpConfig.getHeaders()})
  }
  createBoPhan(ten_bo_phan: string): Observable<any> {
    return this.http.post<any>(this.apiUrl, { ten_bo_phan }, { headers: this.httpConfig.getHeaders() });
  }
}
