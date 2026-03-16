import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Router } from '@angular/router';
import { HttpConfigService } from './http-config.service'; // Đường dẫn có thể thay đổi tùy cấu trúc project

@Injectable({
    providedIn: 'root'
})
export class NgayNghiCoDinhService {
    private apiUrl = this.httpConfig.getApiUrl('Holidays');

    constructor(
        private http: HttpClient,
        private httpConfig: HttpConfigService,
        private router: Router
    ) { }

    /**
     * Lấy danh sách ngày nghỉ theo năm với phân trang.
     * @param year Năm cần lấy dữ liệu.
     * @param page Số trang.
     * @param pageSize Số bản ghi mỗi trang.
     * @returns Observable chứa dữ liệu kết quả.
     */
    getHolidays(year: number, page: number = 1, pageSize: number = 10): Observable<any> {
        const params = new HttpParams()
            .set('year', year.toString())
            .set('page', page.toString())
            .set('pageSize', pageSize.toString());
        return this.http.get<any>(this.apiUrl, { params, headers: this.httpConfig.getHeaders() });
    }

    /**
     * Cập nhật trường mo_ta của một ngày nghỉ theo id.
     * @param id ID của ngày nghỉ cần cập nhật.
     * @param mo_ta Giá trị mới của trường mô tả.
     * @returns Observable chứa kết quả cập nhật.
     */
    updateHolidayDescription(id: number, mo_ta: string): Observable<any> {
        const url = `${this.apiUrl}/${id}/description`;
        return this.http.patch<any>(url, { mo_ta }, { headers: this.httpConfig.getHeaders() });
    }

    /**
     * Lấy danh sách các năm khác nhau có trong cơ sở dữ liệu.
     * API trả về danh sách year theo endpoint /years.
     */
     getDistinctYears(): Observable<any> {
        const url = `${this.apiUrl}/years`;
        return this.http.get<any>(url, { headers: this.httpConfig.getHeaders() });
    }

   /**
   * Xóa bản ghi ngày nghỉ theo id.
   * @param id Id của bản ghi cần xóa.
   * @returns Observable chứa kết quả xóa.
   */
    deleteHoliday(id: number): Observable<any> {
        const url = `${this.apiUrl}/${id}`;
        return this.http.delete<any>(url, {headers: this.httpConfig.getHeaders()});
    }


    /**
     * Lấy tất cả ngày nghỉ theo năm (không phân trang).
     * @param year Năm cần lấy dữ liệu.
     * @returns Observable chứa danh sách ngày nghỉ.
     */
    getAllHolidaysByYear(year: number): Observable<any> {
        const params = new HttpParams().set('year', year.toString());
        return this.http.get<any>(`${this.apiUrl}/all`, { params, headers: this.httpConfig.getHeaders() });
    }

    /**
     * Tạo mới một ngày nghỉ.
     * @param holiday Đối tượng Holiday cần thêm.
     * @returns Observable chứa đối tượng Holiday vừa được tạo.
     */
    createHoliday(holiday: any): Observable<any> {
        return this.http.post<any>(this.apiUrl, holiday,{headers: this.httpConfig.getHeaders()} );
    }
}
