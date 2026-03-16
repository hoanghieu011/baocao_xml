import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';
import { BehaviorSubject } from 'rxjs';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class NghiPhepService {
  constructor(private http: HttpClient, private httpConfig: HttpConfigService, private router: Router) { }
  private apiUrl = this.httpConfig.getApiUrl('NghiPhep')

  // Thông báo số lượng phiếu cần duyệt
  private notificationCountSubject = new BehaviorSubject<number>(0);
  notificationCount1$ = this.notificationCountSubject.asObservable();

  // Thông báo số lượng phiếu cập nhật mới
  private notificationCountSubject_cn = new BehaviorSubject<number>(0);
  notificationCount_cn$ = this.notificationCountSubject_cn.asObservable();

  getNotificationCount(): void {
    this.http.get<{ totalCount: number }>(`${this.apiUrl}/thong-bao-cn`, { headers: this.httpConfig.getHeaders() })
      .subscribe(response => {
        this.notificationCountSubject_cn.next(response.totalCount);
      }, error => {
        console.error('Lỗi khi lấy số lượng thông báo:', error);
      });
  }
  updateThongBao(id: number): void {
    this.http.post(`${this.apiUrl}/cap-nhat-thong-bao`, id, { headers: this.httpConfig.getHeaders() })
      .subscribe(() => {
      }, error => {
        console.error('Lỗi khi cập nhật thông báo:', error);
      });
  }


  huyPhieu(id: number, lyDoHuy: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/huy-phieu`, { id, ly_do_huy: lyDoHuy }, { headers: this.httpConfig.getHeaders() });
  }
  getQuyTrinh(ma_nv: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/quy-trinh/${ma_nv}`, {
      headers: this.httpConfig.getHeaders()
    });
  }
  dsPhieuAll(dto: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/quan-ly-phieu`, dto, { headers: this.httpConfig.getHeaders() });
  }

  updateNotificationBadge1() {
    const searchDto: NghiPhepSearchDto = {
      trang_thai: 'Chưa xử lý',
      searchTerm: 'All',
      Page: 1,
      PageSize: 1
    };

    this.searchNghiPhep(searchDto).subscribe({
      next: response => {
        const count = response.totalCount;
        this.notificationCountSubject.next(count);
      },
      error: (error: HttpErrorResponse) => {
        if (error.status === 401) {
          this.router.navigate(['/login']);
        }
      }
    });
  }
  createNghiPhep(dto: NghiPhepDto, file?: File): Observable<any> {
    const formData = new FormData();

    formData.append('nghi_tu', dto.nghi_tu.toString());
    formData.append('nghi_den', dto.nghi_den.toString());
    formData.append('so_ngay_nghi', dto.so_ngay_nghi.toString());
    formData.append('ky_hieu_ly_do', dto.ky_hieu_ly_do);
    formData.append('ly_do_nghi_str', dto.ly_do_nghi_str);
    formData.append('loai_phep_id', dto.loai_phep_id.toString());
    formData.append('nghi_t7', dto.nghi_t7.toString());
    formData.append('ban_giao', dto.ban_giao);

    if (file) {
      formData.append('file', file, file.name);
    }

    return this.http.post<any>(this.apiUrl, formData, { headers: this.httpConfig.getHeadersForFileUpload() });
  }

  // createNghiPhep(dto: NghiPhepDto): Observable<any> {
  //   return this.http.post<any>(this.apiUrl, dto, { headers: this.httpConfig.getHeaders() });
  // }
  searchNghiPhep(searchDto: NghiPhepSearchDto): Observable<any> {
    const params = new URLSearchParams();

    if (searchDto.trang_thai) {
      params.append('trang_thai', searchDto.trang_thai);
    }
    if (searchDto.searchTerm) {
      params.append('searchTerm', searchDto.searchTerm);
    }
    if (searchDto.Page) {
      params.append('Page', searchDto.Page.toString());
    }
    if (searchDto.PageSize) {
      params.append('PageSize', searchDto.PageSize.toString());
    }
    return this.http.get<any>(`${this.apiUrl}/search?${params.toString()}`, { headers: this.httpConfig.getHeaders() });
  }
  searchNghiPhepCn(searchDto: NghiPhepSearchDto): Observable<any> {
    const params = new URLSearchParams();

    if (searchDto.trang_thai) {
      params.append('trang_thai', searchDto.trang_thai);
    }
    if (searchDto.searchTerm) {
      params.append('searchTerm', searchDto.searchTerm);
    }
    if (searchDto.Page) {
      params.append('Page', searchDto.Page.toString());
    }
    if (searchDto.PageSize) {
      params.append('PageSize', searchDto.PageSize.toString());
    }
    return this.http.get<any>(`${this.apiUrl}/searchCN?${params.toString()}`, { headers: this.httpConfig.getHeaders() });
  }
  batchUpdateStatus(dto: BatchUpdateStatusDto): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/batch-update-status`, dto, { headers: this.httpConfig.getHeaders() });
  }

}

export interface NghiPhepSearchDto {
  trang_thai?: string;
  searchTerm?: string;
  Page: number;
  PageSize: number;
}
export interface NghiPhepDto {
  loai_phep_id: number;
  so_ngay_nghi: number;
  ky_hieu_ly_do: string;
  ly_do_nghi_str: string;
  ban_giao: string;
  nghi_tu: Date;
  nghi_den: Date;
  nghi_t7: number
}
export interface BatchUpdateStatusDto {
  Ids: number[];
  TrangThai: string;
  PreTrangThai: string[];
  LyDoTuChoi: string;
}

