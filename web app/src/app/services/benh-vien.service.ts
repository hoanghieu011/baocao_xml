import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';

@Injectable({
  providedIn: 'root'
})
export class BenhVienService {

  private apiUrl = this.httpConfig.getApiUrl('BenhVien');

  constructor(
    private http: HttpClient,
    private httpConfig: HttpConfigService
  ) {}

  getTtBenhVien(
  ): Observable<any> {
    return this.http.get<any>(
      `${this.apiUrl}/tt_benhvien`,
      { headers: this.httpConfig.getHeaders() }
    );
  }
}