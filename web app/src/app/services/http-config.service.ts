import { Injectable } from '@angular/core';
import { HttpHeaders } from '@angular/common/http';
export type CONTENT_TYPE = null | 'FILE_UPLOAD'
@Injectable({
  providedIn: 'root',
})
export class HttpConfigService {
  private readonly baseUrl = '/api';
  // private readonly baseUrl = 'https://localhost:7037/api';

  getHeaders(contentType: CONTENT_TYPE = null): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'Content-Type': contentType==null ? 'application/json' : 'multipart/form-data',
    });
  }

  getHeadersForFileUpload(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });
  }  

  getApiUrl(endpoint: string): string {
    return `${this.baseUrl}/${endpoint}`;
  }
}
