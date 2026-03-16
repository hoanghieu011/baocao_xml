import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';

@Injectable({
  providedIn: 'root'
})
export class PhepTonService {
  private apiUrl = this.httpConfig.getApiUrl('phep-ton')

  constructor(private http: HttpClient, private httpConfig: HttpConfigService) { }
  getDistinctYears(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/years`, { headers: this.httpConfig.getHeaders() });
  }
  importPhepTon(year: string, file: File): Observable<Blob> {
    const formData: FormData = new FormData();
    formData.append('file', file, file.name);

    return this.http.post(`${this.apiUrl}/import-phep-ton?year=${year}`, formData, {
      headers: this.httpConfig.getHeadersForFileUpload(),
      responseType: 'blob'
    });
  }
  getDsPhepTon(year: string, searchTerm: string = '', page: number = 1, pageSize: number = 10): Observable<any> {
    let params = new HttpParams()
      .set('year', year)
      .set('searchTerm', searchTerm)
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<any>(`${this.apiUrl}/get-phep-ton`, { headers: this.httpConfig.getHeaders(), params });
  }
  getPhepTonByMaNV(ma_nv: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/GetNghiPhepByMaNV/${ma_nv}`, { headers: this.httpConfig.getHeaders() });
  }
  updatePhepTon(ma_nv: string, year: string, phep_ton: number): Observable<any> {
    const body = { MaNv: ma_nv, Year: year, PhepTon: phep_ton };
    return this.http.put<any>(`${this.apiUrl}`, body, { headers: this.httpConfig.getHeaders() });
  }
  getPhepTon(ma_nv: string, year: string): Observable<any> {
    const params = new HttpParams()
      .set('ma_nv', ma_nv)
      .set('year', year);

    return this.http.get<any>(`${this.apiUrl}`, {
      headers: this.httpConfig.getHeaders(),
      params: params
    });
  }

}
