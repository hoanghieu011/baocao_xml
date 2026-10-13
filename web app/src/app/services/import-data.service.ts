import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, Observable } from 'rxjs';
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

export interface HisFetchRequest {
  year: number;
  month: number;
  token: string;
  cookie: string;
  cskcb: string;
  locTheo: string;
  qd3176: string;
  page: number;
  rows: number;
  maLks: string[];
}

export interface HisRecord {
  maLk: string;
  qd3176: string;
}

export interface HisPage {
  total: number;
  records: HisRecord[];
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

  fetchHisRecords(request: HisFetchRequest): Observable<HisPage> {
    return this.http.post<HisPage>(`${this.apiUrl}/FetchHisRecords`, request,
      { headers: this.httpConfig.getHeaders() });
  }

  checkHisSession(request: { token: string; cookie: string }): Observable<{ isValid: boolean; message: string }> {
    return this.http.post<{ isValid: boolean; message: string }>(`${this.apiUrl}/CheckHisSession`,
      { token: request.token, cookie: request.cookie }, { headers: this.httpConfig.getHeaders() });
  }

  fetchHisXmlBatch(request: HisFetchRequest): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/FetchHisXmlBatch`, request,
      { headers: this.httpConfig.getHeaders(), responseType: 'blob' }).pipe(
        catchError(async (error: HttpErrorResponse) => {
          if (error.error instanceof Blob) {
            let body: unknown;
            try { body = JSON.parse(await error.error.text()); }
            catch { body = { message: 'HIS không trả được XML. Kiểm tra lại phiên và tham số.' }; }
            throw new HttpErrorResponse({ error: body, headers: error.headers, status: error.status,
              statusText: error.statusText, url: error.url || undefined });
          }
          throw error;
        })
      );
  }

  getHisBrowserScope(): Observable<{ scope: string }> {
    return this.http.get<{ scope: string }>(`${this.apiUrl}/HisBrowserScope`,
      { headers: this.httpConfig.getHeaders() });
  }

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
