import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';
type ExcelTypeData = 'BNND' | 'BN15T' | 'BN_NHAPVIEN';
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

    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<any>(
      `${this.apiUrl}/ImportXMLHospitalData`,
      formData,
      { headers: this.httpConfig.getHeaders() }
    );
  }

  importExcelData(
    file: File,
    type: ExcelTypeData
  ): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('type', type);
    return this.http.post<any>(
      `${this.apiUrl}/ImportExcelHospitalData`,
      formData,
      { headers: this.httpConfig.getHeaders() }
    );
  }

}