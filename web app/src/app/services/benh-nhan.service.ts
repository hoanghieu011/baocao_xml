import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { HttpConfigService } from './http-config.service';

export interface Dvkt {
  stt: number | null;
  tenDichVu: string | null;
  soLuong: number | null;
  giaTien: number | null;
  giaBh: number | null;
  bhytTra: number | null;
  bncct: number | null;
  bnTra: number | null;
  tyLeThanhToan: number | null;
  phanTramMienGiam: number | null;
  mienGiam: number | null;
  vat: number | null;
}

export interface DsDvktResponse {
  totalRecords: number;
  pageIndex: number;
  pageSize: number;
  dsDvkt: Dvkt[];
}

export interface Thuoc {
  stt: number | null;
  tenThuoc: string | null;
  donViTinh: string | null;
  hamLuong: string | null;
  duongDung: string | null;
  dangBaoChe: string | null;
  lieuDung: string | null;
  cachDung: string | null;
  soLuong: number | null;
  donGia: number | null;
  thanhTienBv: number | null;
  thanhTienBh: number | null;
  bhytTra: number | null;
  bncct: number | null;
  bnTra: number | null;
  tyLeThanhToan: number | null;
}
 
export interface DsThuocResponse {
  totalRecords: number;
  pageIndex: number;
  pageSize: number;
  dsThuoc: Thuoc[];
}

@Injectable({
  providedIn: 'root'
})
export class BenhNhanService {

  private xml1ApiUrl = this.httpConfig.getApiUrl('Xml1');
  private xml2ApiUrl = this.httpConfig.getApiUrl('Xml2');
  private xml3ApiUrl = this.httpConfig.getApiUrl('Xml3');

  constructor(
    private http: HttpClient,
    private httpConfig: HttpConfigService
  ) {}

  getDsBenhNhan(
    pageNumber: number = 1,
    pageSize: number = 50,
    searchTerm: string = '',
    tuNgay?: Date,
    denNgay?: Date
  ): Observable<any> {

    const body: any = {
      pageNumber,
      pageSize,
      searchTerm
    };

    if (tuNgay) {
      body.tuNgay = tuNgay;
    }

    if (denNgay) {
      body.denNgay = denNgay;
    }

    return this.http.post<any>(
      `${this.xml1ApiUrl}/ds_benh_nhan`,
      body,
      { headers: this.httpConfig.getHeaders() }
    );
  }

  getDsDvktByMaLk(
    maLk: string,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Observable<DsDvktResponse> {
    return this.http.post<Record<string, unknown>>(
      `${this.xml3ApiUrl}/get_ds_dvkt_by_malk`,
      {
        pageNumber,
        pageSize,
        maLk,
        searchTerm: ''
      },
      { headers: this.httpConfig.getHeaders() }
    ).pipe(
      map((response) => {
        const rows = this.readApiValue(response, 'DsDvkt');
        const dsDvkt = Array.isArray(rows)
          ? rows
              .filter((row): row is Record<string, unknown> => row !== null && typeof row === 'object')
              .map((row) => this.mapDvkt(row))
          : [];

        return {
          totalRecords: this.toNullableNumber(this.readApiValue(response, 'TotalRecords')) ?? 0,
          pageIndex: this.toNullableNumber(this.readApiValue(response, 'PageIndex')) ?? pageNumber,
          pageSize: this.toNullableNumber(this.readApiValue(response, 'PageSize')) ?? pageSize,
          dsDvkt
        };
      })
    );
  }

  getDsThuocByMaLk(
    maLk: string,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Observable<DsThuocResponse> {
    return this.http.post<Record<string, unknown>>(
      `${this.xml2ApiUrl}/get_ds_thuoc_by_malk`,
      {
        pageNumber,
        pageSize,
        maLk,
        searchTerm: ''
      },
      { headers: this.httpConfig.getHeaders() }
    ).pipe(
      map((response) => {
        const rows = this.readApiValue(response, 'DsThuoc');
        const dsThuoc = Array.isArray(rows)
          ? rows
              .filter((row): row is Record<string, unknown> => row !== null && typeof row === 'object')
              .map((row) => this.mapThuoc(row))
          : [];
 
        return {
          totalRecords: this.toNullableNumber(this.readApiValue(response, 'TotalRecords')) ?? 0,
          pageIndex: this.toNullableNumber(this.readApiValue(response, 'PageIndex')) ?? pageNumber,
          pageSize: this.toNullableNumber(this.readApiValue(response, 'PageSize')) ?? pageSize,
          dsThuoc
        };
      })
    );
  }

  private mapDvkt(row: Record<string, unknown>): Dvkt {
    return {
      stt: this.toNullableNumber(this.readApiValue(row, 'STT')),
      tenDichVu:
        this.toNullableString(this.readApiValue(row, 'TEN_DICH_VU'))
        ?? this.toNullableString(this.readApiValue(row, 'TEN_VAT_TU')),
      soLuong: this.toNullableNumber(this.readApiValue(row, 'SO_LUONG')),
      giaTien: this.toNullableNumber(this.readApiValue(row, 'DON_GIA_BV')),
      giaBh: this.toNullableNumber(this.readApiValue(row, 'DON_GIA_BH')),
      bhytTra: this.toNullableNumber(this.readApiValue(row, 'T_BHTT')),
      bncct: this.toNullableNumber(this.readApiValue(row, 'T_BNCCT')),
      bnTra: this.toNullableNumber(this.readApiValue(row, 'T_BNTT')),
      tyLeThanhToan: this.toNullableNumber(this.readApiValue(row, 'TYLE_TT_DV')),
      phanTramMienGiam: this.toNullableNumber(
        this.readApiValue(row, 'TYLE_MIEN_GIAM', 'TY_LE_MIEN_GIAM', 'PHAN_TRAM_MIEN_GIAM')
      ),
      mienGiam: this.toNullableNumber(this.readApiValue(row, 'MIEN_GIAM', 'T_MIEN_GIAM')),
      vat: this.toNullableNumber(this.readApiValue(row, 'VAT', 'T_VAT'))
    };
  }

  private mapThuoc(row: Record<string, unknown>): Thuoc {
    return {
      stt: this.toNullableNumber(this.readApiValue(row, 'STT')),
      tenThuoc: this.toNullableString(this.readApiValue(row, 'TEN_THUOC')),
      donViTinh: this.toNullableString(this.readApiValue(row, 'DON_VI_TINH')),
      hamLuong: this.toNullableString(this.readApiValue(row, 'HAM_LUONG')),
      duongDung: this.toNullableString(this.readApiValue(row, 'DUONG_DUNG')),
      dangBaoChe: this.toNullableString(this.readApiValue(row, 'DANG_BAO_CHE')),
      lieuDung: this.toNullableString(this.readApiValue(row, 'LIEU_DUNG')),
      cachDung: this.toNullableString(this.readApiValue(row, 'CACH_DUNG')),
      soLuong: this.toNullableNumber(this.readApiValue(row, 'SO_LUONG')),
      donGia: this.toNullableNumber(this.readApiValue(row, 'DON_GIA')),
      thanhTienBv: this.toNullableNumber(this.readApiValue(row, 'THANH_TIEN_BV')),
      thanhTienBh: this.toNullableNumber(this.readApiValue(row, 'THANH_TIEN_BH')),
      bhytTra: this.toNullableNumber(this.readApiValue(row, 'T_BHTT')),
      bncct: this.toNullableNumber(this.readApiValue(row, 'T_BNCCT')),
      bnTra: this.toNullableNumber(this.readApiValue(row, 'T_BNTT')),
      tyLeThanhToan: this.toNullableNumber(this.readApiValue(row, 'TYLE_TT_BH'))
    };
  }

  private readApiValue(source: Record<string, unknown>, ...propertyNames: string[]): unknown {
    const normalizedNames = propertyNames.map((name) => name.toLowerCase());
    const key = Object.keys(source).find((item) => normalizedNames.includes(item.toLowerCase()));
    return key ? source[key] : undefined;
  }

  private toNullableNumber(value: unknown): number | null {
    if (value === null || value === undefined || value === '') {
      return null;
    }

    const numberValue = Number(value);
    return Number.isFinite(numberValue) ? numberValue : null;
  }

  private toNullableString(value: unknown): string | null {
    if (value === null || value === undefined) {
      return null;
    }

    const stringValue = String(value).trim();
    return stringValue || null;
  }

}