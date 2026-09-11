import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';
export type ExcelTypeData = 'BNND' | 'BN15T' | 'BN_NHAPVIEN';
@Injectable({
  providedIn: 'root'
})
export class ImportDataService {

  private apiUrl = this.httpConfig.getApiUrl('Import');

  constructor(
    private http: HttpClient,
    private httpConfig: HttpConfigService
  ) {}

  importXMLData(
    file: File
  ): Observable<any> {
    const token = localStorage.getItem('token');
    const formData = new FormData();
    formData.append('file', file);
    let headers = this.httpConfig.getHeaders('FILE_UPLOAD');
    
    return this.http.post<any>(
      `${this.apiUrl}/ImportXMLHospitalData`,
      formData,
     { headers: new HttpHeaders({'Authorization': `Bearer ${token}`}) }
    );
  }

  importExcelData(
    file: File,
    type: ExcelTypeData
  ): Observable<any> {
    const token = localStorage.getItem('token');
    const formData = new FormData();
    formData.append('file', file);
    formData.append('excelTable', type);
    let headers = this.httpConfig.getHeaders('FILE_UPLOAD');
    return this.http.post<any>(
      `${this.apiUrl}/ImportExcelHospitalData`,
      formData,
      { headers: new HttpHeaders({'Authorization': `Bearer ${token}`}) }
    );
  }

}