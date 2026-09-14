import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';

export type ExcelTypeData = 'BNND' | 'BN15T' | 'BN_NHAPVIEN';

export interface ImportExcelTableInfo {
  code: ExcelTypeData;
  name: string;
}

export interface ImportExcelResponse {
  isError: boolean;
  message: string;
  affectedRows?: number;
  table?: string;
}

export interface ImportXMLResponse {
  isError: boolean;
  message: string;
  countXML1?: number;
  countXML2?: number;
  countXML3?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ImportDataService {
  private apiUrl = this.httpConfig.getApiUrl('Import');

  constructor(
    private http: HttpClient,
    private httpConfig: HttpConfigService
  ) {}

  getImportExcelTablesInfo(): Observable<ImportExcelTableInfo[]> {
    return this.http.post<ImportExcelTableInfo[]>(
      `${this.apiUrl}/GetImportExcelTablesInfo`,
      {},
      { headers: this.httpConfig.getHeaders() }
    );
  }

  getImportExcelTemplate(excelTable: ExcelTypeData): Observable<Blob> {
    const params = new HttpParams().set('excelTable', excelTable);
    return this.http.post(
      `${this.apiUrl}/GetImportExcelTemplate`,
      null,
      {
        headers: this.httpConfig.getHeaders(),
        params,
        responseType: 'blob'
      }
    );
  }

  importXMLData(file: File): Observable<ImportXMLResponse> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<ImportXMLResponse>(
      `${this.apiUrl}/ImportXMLHospitalData`,
      formData,
      { headers: this.httpConfig.getHeadersForFileUpload() }
    );
  }

  importExcelData(file: File, type: ExcelTypeData): Observable<ImportExcelResponse> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('excelTable', type);

    return this.http.post<ImportExcelResponse>(
      `${this.apiUrl}/ImportExcelHospitalData`,
      formData,
      { headers: this.httpConfig.getHeadersForFileUpload() }
    );
  }

  deleteHospitalDataByMonth(thang: number, nam: number): Observable<string> {
    const params = new HttpParams()
      .set('thang', thang)
      .set('nam', nam);

    return this.http.delete(
      `${this.apiUrl}/DeleteHospitalDataByMonth`,
      {
        headers: this.httpConfig.getHeaders(),
        params,
        responseType: 'text'
      }
    );
  }
}
