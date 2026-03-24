import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BorderDirective, TableDirective } from '@coreui/angular';
import { ToastModule } from '@coreui/angular';
import { Select2Module, Select2Data, Select2UpdateEvent, Select2SearchEvent } from 'ng-select2-component';
import { OfficerService } from '../services/officer.service';
import { BaoCaoService } from '../services/bao-cao.service';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';

type ReportRowType = 'group' | 'item' | 'total' | 'grandTotal';

interface ReportRow {
  type: ReportRowType;
  stt?: string;
  tennhom?: string;
  ten_dich_vu?: string;
  soluong?: number;
  don_gia_bh?: number;
  thanh_tien?: number;
  chiphi_vattu?: number;
  sotien_conlai?: number;
  heso?: number;
  diem_thuchien?: number;
}

@Component({
  selector: 'app-ds-benhnhan',
  standalone: true,
  imports: [CommonModule, FormsModule, TableDirective, BorderDirective, ToastModule, Select2Module],
  templateUrl: './bc-doanhthu-bsth.component.html',
  styleUrl: './bc-doanhthu-bsth.component.css'
})


export class BcDoanhthuBsthComponent implements OnInit {
  displayRows: ReportRow[] = [];

  Math = Math;

  tuNgay: string = '';
  denNgay: string = '';

  data: any[] = [];
  loading = false;

  loadingExcel = false;

  cur_officer: string = '';
  ds_officer: any[] = [];

  constructor(private officerService: OfficerService, private baoCaoService: BaoCaoService) { }

  ngOnInit(): void {
    this.setDefaultMonthRange();
    this.loadDsOfficer();
  }

  loadDsOfficer() {
    this.officerService.getDsOfficer().subscribe({
      next: (res) => {
        const data = res?.ds_officer || [];

        this.ds_officer = [
          ...data.map((x: any) => ({
            value: x.ma_bac_si,
            label: x.ma_bac_si + ' - ' + x.officer_name
          }))
        ];
        this.cur_officer = ''
      },
      error: (err) => {
        console.error(err);
        this.ds_officer = [{ value: '', label: '' }];
        this.cur_officer = '';
      }
    });
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

    if (this.cur_officer == '') {
      this.addToast('Vui lòng chọn bác sĩ!');
      return;
    }

    this.loading = true;

    const tu = this.tuNgay ? new Date(this.tuNgay + 'T00:00:00') : undefined;
    const den = this.denNgay ? new Date(this.denNgay + 'T23:59:59') : undefined;

    this.baoCaoService.getBcDoanhThuBsth(
      this.cur_officer,
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

    if (this.cur_officer == '') {
      this.addToast('Vui lòng chọn bác sĩ!');
      return;
    }

    this.loadingExcel = true;

    const tu = this.tuNgay ? new Date(this.tuNgay + 'T00:00:00') : undefined;
    const den = this.denNgay ? new Date(this.denNgay + 'T23:59:59') : undefined;

    this.baoCaoService.exportBcDoanhThuBsthExcel(
      this.cur_officer,
      tu,
      den
    ).subscribe({
      next: (blob) => {
        const fileName = `bao_cao_doanhthu_bsth_${this.tuNgay}_${this.denNgay}.xlsx`;

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
    this.cur_officer = event.value;
    this.loadData(true);
  }

  buildDisplayRows() {
    const sorted = [...(this.data ?? [])].sort((a, b) => {
      const n1 = (a.nhom_mabhyt_id ?? 0) - (b.nhom_mabhyt_id ?? 0);
      if (n1 !== 0) return n1;
      return String(a.ma_dich_vu ?? '').localeCompare(String(b.ma_dich_vu ?? ''));
    });

    const groups = new Map<string, any[]>();

    for (const item of sorted) {
      const key = item.tennhom ?? '';
      if (!groups.has(key)) groups.set(key, []);
      groups.get(key)!.push(item);
    }

    this.displayRows = [];

    let groupIndex = 0;
    let grandThanhTien = 0;
    let grandChiPhiVattu = 0;
    let grandConLai = 0;
    let grandDiem = 0;

    groups.forEach((items, groupName) => {
      groupIndex++;

      this.displayRows.push({
        type: 'group',
        stt: String(groupIndex),
        tennhom: groupName
      });

      let itemIndex = 0;
      let tongThanhTien = 0;
      let tongChiPhiVattu = 0;
      let tongConLai = 0;
      let tongDiem = 0;

      for (const item of items) {
        itemIndex++;

        const thanhTien = Number(item.thanh_tien ?? 0);
        const chiPhi = Number(item.chiphi_vattu ?? 0);
        const conLai = Number(item.sotien_conlai ?? 0);
        const diem = Number(item.diem_thuchien ?? 0);

        tongThanhTien += thanhTien;
        tongChiPhiVattu += chiPhi;
        tongConLai += conLai;
        tongDiem += diem;

        grandThanhTien += thanhTien;
        grandChiPhiVattu += chiPhi;
        grandConLai += conLai;
        grandDiem += diem;

        this.displayRows.push({
          type: 'item',
          stt: `${groupIndex}.${itemIndex}`,
          ten_dich_vu: item.ten_dich_vu,
          soluong: item.soluong,
          don_gia_bh: item.don_gia_bh,
          thanh_tien: thanhTien,
          chiphi_vattu: chiPhi,
          sotien_conlai: conLai,
          heso: item.heso,
          diem_thuchien: diem
        });
      }

      this.displayRows.push({
        type: 'total',
        ten_dich_vu: 'Tổng',
        thanh_tien: tongThanhTien,
        chiphi_vattu: tongChiPhiVattu,
        sotien_conlai: tongConLai,
        diem_thuchien: tongDiem
      });
    });
    this.displayRows.push({
      type: 'grandTotal',
      ten_dich_vu: 'Tổng cộng',
      thanh_tien: grandThanhTien,
      chiphi_vattu: grandChiPhiVattu,
      sotien_conlai: grandConLai,
      diem_thuchien: grandDiem
    });
  }
}