import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BorderDirective, TableDirective } from '@coreui/angular';
import { ToastModule } from '@coreui/angular';
import { Select2Module, Select2Data, Select2UpdateEvent, Select2SearchEvent } from 'ng-select2-component';
import { BaoCaoService } from '../services/bao-cao.service';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { forEach } from 'lodash-es';

type ReportRowType = 'item' | 'grandTotal';

interface ReportRow {
  type: ReportRowType;
  stt?: string;
  thanh_tien?: number;
  chiphi_vattu?: number;
  sotien_conlai?: number;
  khoa?: string;
  ma_khoa?: string;
}

@Component({
  selector: 'app-ds-benhnhan',
  standalone: true,
  imports: [CommonModule, FormsModule, TableDirective, BorderDirective, ToastModule, Select2Module],
  templateUrl: './bc-doanhthu-toanvien.component.html',
  styleUrl: './bc-doanhthu-toanvien.component.css'
})


export class BcDoanhthuToanvienComponent implements OnInit {
  displayRows: ReportRow[] = [];

  Math = Math;

  tuNgay: string = '';
  denNgay: string = '';

  data: any[] = [];
  loading = false;

  loadingExcel = false;

  constructor(private baoCaoService: BaoCaoService) { }

  ngOnInit(): void {
    this.setDefaultMonthRange();
  }

  search$ = new Subject<string>();

  onSearch(e: any) {
    const term = e?.term || '';
    this.search$.next(term);
  }

  private setDefaultMonthRange() {
    const now = new Date();
    const first = new Date(now.getFullYear(), now.getMonth() - 1, 1);
    const last = new Date(now.getFullYear(), now.getMonth(), 0);

    this.tuNgay = this.toInputDate(first);
    this.denNgay = this.toInputDate(last);
  }
  toasts: any[] = [];

  addToast(message: string, color: string = 'danger') {
    this.toasts.push({
      message,
      color,
      visible: true
    });
    setTimeout(() => {
      this.toasts.shift();
    }, 3000);
  }
  private toInputDate(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${dd}`;
  }

  loadData(resetPage: boolean = false) {
    if (!this.tuNgay || !this.denNgay) {
      this.addToast('Từ ngày / đến ngày không được để trống');
      return;
    }

    if (this.tuNgay > this.denNgay) {
      this.addToast('Từ ngày / đến ngày không được lớn hơn đến ngày');
      return;
    }


    this.loading = true;

    const tu = this.tuNgay ? new Date(this.tuNgay + 'T00:00:00') : undefined;
    const den = this.denNgay ? new Date(this.denNgay + 'T23:59:59') : undefined;

    this.baoCaoService.getBcDoanhThuToanvien(
      tu,
      den
    ).subscribe({
      next: (res) => {
        this.data = res?.data ?? [];
        this.buildDisplayRows();
        this.loading = false;
      },
      error: (e) => {
        console.error(e);
        this.addToast('Có lỗi xảy ra, vui lòng thử lại sau!');
        this.loading = false;
      }
    });
  }

  exportExcel() {
    if (!this.tuNgay || !this.denNgay) {
      this.addToast('Từ ngày / đến ngày không được để trống');
      return;
    }

    if (this.tuNgay > this.denNgay) {
      this.addToast('Từ ngày không được lớn hơn đến ngày');
      return;
    }

    // if (this.cur_org == '') {
    //   this.addToast('Vui lòng chọn khoa!');
    //   return;
    // }

    this.loadingExcel = true;

    const tu = this.tuNgay ? new Date(this.tuNgay + 'T00:00:00') : undefined;
    const den = this.denNgay ? new Date(this.denNgay + 'T23:59:59') : undefined;

    this.baoCaoService.exportBcDoanhThuToanvienExcel(
      tu,
      den
    ).subscribe({
      next: (blob) => {
        const fileName = `bao_cao_doanhthu_toanvien_${this.tuNgay}_${this.denNgay}.xlsx`;

        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        a.click();

        window.URL.revokeObjectURL(url);

        this.loadingExcel = false;
      },
      error: () => {
        this.addToast('Xuất file thất bại!');
        this.loadingExcel = false;
      }
    });
  }

  update(event: Select2UpdateEvent<any>) {
    this.loadData(true);
  }

  buildDisplayRows() {
    this.displayRows = [];
    let grandThanhTien = 0;
    let grandChiPhiVattu = 0;
    let grandConLai = 0;
    let grandDiem = 0;

    this.data.forEach((item: any, idx) => {
      grandThanhTien += item.thanh_tien ?? 0;
      grandChiPhiVattu += item.chiphi_vattu ?? 0;
      grandConLai += item.sotien_conlai ?? 0;
      grandDiem += item.diem_thuchien ?? 0;
      this.displayRows.push({
        type: 'item',
        stt: (idx+1).toString(),
        ma_khoa: item.ma_khoa,
        khoa: item.khoa,
        thanh_tien: item.thanh_tien,
        chiphi_vattu: item.chiphi_vattu,
        sotien_conlai: item.sotien_conlai,
      });

    });
    this.displayRows.push({
      type: 'grandTotal',
      thanh_tien: grandThanhTien,
      chiphi_vattu: grandChiPhiVattu,
      sotien_conlai: grandConLai,
    });
  }
}