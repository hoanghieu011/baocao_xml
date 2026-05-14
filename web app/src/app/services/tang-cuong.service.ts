import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';

@Injectable({
  providedIn: 'root'
})
export class TangCuongService {

  private apiUrl = this.httpConfig.getApiUrl('TangCuong');

  constructor(
    private http: HttpClient,
    private httpConfig: HttpConfigService
  ) {}

  getAllTangCuongByDiemKeHoachId(
    diemKeHoachId: number
  ): Observable<any> {

    const body: any = {
      diemKeHoachId
    };
    return this.http.post<any>(
      `${this.apiUrl}/ds_tangcuong`,
      body,
      { headers: this.httpConfig.getHeaders() }
    );
  }

  batchInsertOrUpdateTangCuong(
    diemKeHoachId: number,
    tangCuongs: any[]
  ): Observable<any> {
    const body: any = {
      dsTangCuong: tangCuongs,
      diemKeHoachId
    };
    return this.http.patch<any>(
      `${this.apiUrl}/capnhat_tangcuong`,
      body,
      { headers: this.httpConfig.getHeaders() }
    );
  }

  deleteTangCuong(diemKeHoachId: number): Observable<any> {
    return this.http.delete<any>(
      `${this.apiUrl}/xoa_tangcuong/${diemKeHoachId}`,
      { headers: this.httpConfig.getHeaders() }
    );
  }

}