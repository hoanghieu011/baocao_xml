import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';

@Injectable({
  providedIn: 'root'
})
export class LoaiPhepService {
  private apiUrl = this.httpConfig.getApiUrl('LoaiPhep')

  constructor(private http: HttpClient, private httpConfig: HttpConfigService) { }

  getPagedLoaiPhep(currentPage: number, pageSize: number, searchTerm: string = ''): Observable<any> {
    let params = new HttpParams()
      .set('currentPage', currentPage)
      .set('pageSize', pageSize);
    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }

    return this.http.get(`${this.apiUrl}/GetPagedLoaiPhep`, { params, headers: this.httpConfig.getHeaders() });
  }

  createLoaiPhep(loaiPhep: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/CreateLoaiPhep`, loaiPhep, { headers: this.httpConfig.getHeaders() });
  }

  updateLoaiPhep(id: number, loaiPhep: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/UpdateLoaiPhep/${id}`, loaiPhep, { headers: this.httpConfig.getHeaders() });
  }
  getAllLoaiPhep(): Observable<any> {
    return this.http.get(`${this.apiUrl}/GetAllLoaiPhep`, { headers: this.httpConfig.getHeaders() });
  }
}
