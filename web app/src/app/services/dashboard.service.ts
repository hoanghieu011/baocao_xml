import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';

export interface DashboardSummaryResponse {
  year: number;
  kpis: {
    totalVisits: number;
    insuredPatients: number;
    totalRevenue: number;
  };
  revenueStructure: {
    bhytPaid: number;
    copay: number;
    hospitalFee: number;
  };
  monthlyVisits: number[];
  monthlyRevenue: {
    bhytPaid: number[];
    copay: number[];
    insuredPatientRevenue: number[];
    hospitalFee: number[];
  };
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private apiUrl = this.httpConfig.getApiUrl('Dashboard');

  constructor(private http: HttpClient, private httpConfig: HttpConfigService) {}

  getSummary(year: number): Observable<DashboardSummaryResponse> {
    const params = new HttpParams().set('year', year.toString());
    return this.http.get<DashboardSummaryResponse>(`${this.apiUrl}/summary`, {
      params,
      headers: this.httpConfig.getHeaders()
    });
  }
}
