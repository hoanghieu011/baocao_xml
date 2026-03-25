import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BorderDirective, TableDirective } from '@coreui/angular';
import { ToastModule } from '@coreui/angular';
import { Select2Module, Select2Data, Select2UpdateEvent, Select2SearchEvent } from 'ng-select2-component';
import { BaoCaoService } from '../services/bao-cao.service';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { OrganizationService } from '../services/org-organization.service';
import { forEach } from 'lodash-es';

type ReportRowType = 'organization' | 'group' | 'item' | 'totalKhoa' | 'grandTotal' | 'totalGroup';

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
  ten_khoa?: string;
  ma_khoa?: string;
  heso?: number;
  diem_thuchien?: number;
}

@Component({
  selector: 'app-ds-benhnhan',
  standalone: true,
  imports: [CommonModule, FormsModule, TableDirective, BorderDirective, ToastModule, Select2Module],
  templateUrl: './bc-doanhthu-khoa.component.html',
  styleUrl: './bc-doanhthu-khoa.component.css'
})


export class BcDoanhthuKhoaComponent implements OnInit {
  displayRows: ReportRow[] = [];

  Math = Math;

  tuNgay: string = '';
  denNgay: string = '';

  data: any[] = [];
  loading = false;

  loadingExcel = false;

  cur_org: string = '';
  ds_organization: any[] = [];

  constructor(private organizationService: OrganizationService, private baoCaoService: BaoCaoService) { }

  ngOnInit(): void {
    this.setDefaultMonthRange();
    this.loadDsOrganization();
  }

  loadDsOrganization() {
    this.organizationService.getDsOrganization().subscribe({
      next: (res) => {
        const data = res?.ds_organization || [];

        this.ds_organization = [
          ...data.map((x: any) => ({
            value: x.ma_khoa,
            label: x.ma_khoa + ' - ' + x.org_name
          }))
        ];
        this.cur_org = ''
      },
      error: (err) => {
        console.error(err);
        this.ds_organization = [{ value: '', label: '' }];
        this.cur_org = '';
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

    // if (this.cur_org == '') {
    //   this.addToast('Vui lòng chọn khoa!');
    //   return;
    // }

    this.loading = true;

    const tu = this.tuNgay ? new Date(this.tuNgay + 'T00:00:00') : undefined;
    const den = this.denNgay ? new Date(this.denNgay + 'T23:59:59') : undefined;

    this.baoCaoService.getBcDoanhThuKhoa(
      this.cur_org,
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

    this.baoCaoService.exportBcDoanhThuKhoaExcel(
      this.cur_org,
      tu,
      den
    ).subscribe({
      next: (blob) => {
        const fileName = `bao_cao_doanhthu_khoa_${this.tuNgay}_${this.denNgay}.xlsx`;

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
    this.cur_org = event.value;
    this.loadData(true);
  }

  buildDisplayRows() {
    // const sorted = [...(this.data ?? [])].sort((a, b) => {
    //   const n1 = (a.nhom_mabhyt_id ?? 0) - (b.nhom_mabhyt_id ?? 0);
    //   if (n1 !== 0) return n1;
    //   return String(a.ma_dich_vu ?? '').localeCompare(String(b.ma_dich_vu ?? ''));
    // });
    const orgs = new Map<string, any[]>();

    for (const item of this.data ?? []) {
      const key = item.ma_khoa ?? '';
      if (!orgs.has(key)) orgs.set(key, []);
      orgs.get(key)!.push(item);
    }

    this.displayRows = [];

    let groupIndex = 0;
    let grandThanhTien = 0;
    let grandChiPhiVattu = 0;
    let grandConLai = 0;
    let grandDiem = 0;

    orgs.forEach((items, groupCode) => {
      // mỗi khoa là 1 nhóm
      groupIndex++;

      this.displayRows.push({
        type: 'organization',
        stt: String(groupIndex),
        ten_khoa: items[0].khoa // lấy tên khoa từ item đầu tiên trong nhóm
      });

      let itemKhoaIndex = 0;
      let tongThanhTienKhoa = 0;
      let tongChiPhiVattuKhoa = 0;
      let tongConLaiKhoa = 0;
      let tongDiemKhoa = 0;

      const groups = new Map<string, any[]>();
      items.forEach( (item)=> {
        const key = item.tennhom ?? '';
        if (!groups.has(key)) groups.set(key, []);
          groups.get(key)!.push(item);
        });

      groups.forEach((groupItems) => {
        itemKhoaIndex++;
        // mỗi nhóm là 1 nhóm con trong khoa
        this.displayRows.push({
          type: 'group',
          stt: `${groupIndex}.${itemKhoaIndex}`,
          tennhom: groupItems[0].tennhom
        });

        let itemServiceIndex = 0;
        let tongThanhTienGroup = 0;
        let tongChiPhiVattuGroup = 0;
        let tongConLaiGroup = 0;
        let tongDiemGroup = 0;

        for (const groupItem of groupItems) {

          itemServiceIndex++;

          const thanhTien = Number(groupItem.thanh_tien ?? 0);
          const chiPhi = Number(groupItem.chiphi_vattu ?? 0);
          const conLai = Number(groupItem.sotien_conlai ?? 0);
          const diem = Number(groupItem.diem_thuchien ?? 0);

          tongThanhTienGroup += thanhTien;
          tongChiPhiVattuGroup += chiPhi;
          tongConLaiGroup += conLai;
          tongDiemGroup += diem;

          this.displayRows.push({
            type: 'item',
            stt: `${groupIndex}.${itemKhoaIndex}.${itemServiceIndex}`,
            ten_dich_vu: groupItem.ten_dich_vu,
            soluong: groupItem.soluong,
            ten_khoa: groupItem.khoa,
            don_gia_bh: groupItem.don_gia_bh,
            thanh_tien: thanhTien,
            chiphi_vattu: chiPhi,
            sotien_conlai: conLai,
            heso: groupItem.heso,
            diem_thuchien: diem
          });
        }
        this.displayRows.push({
          type: 'totalGroup',
          ten_dich_vu: 'Tổng theo nhóm',
          thanh_tien: tongThanhTienGroup,
          chiphi_vattu: tongChiPhiVattuGroup,
          sotien_conlai: tongConLaiGroup,
          diem_thuchien: tongDiemGroup
        });
        tongThanhTienKhoa += tongThanhTienGroup;
        tongChiPhiVattuKhoa += tongChiPhiVattuGroup;
        tongConLaiKhoa += tongConLaiGroup;
        tongDiemKhoa += tongDiemGroup;
      });
      this.displayRows.push({
        type: 'totalKhoa',
        ten_dich_vu: 'Tổng theo khoa',
        thanh_tien: tongThanhTienKhoa,
        chiphi_vattu: tongChiPhiVattuKhoa,
        sotien_conlai: tongConLaiKhoa,
        diem_thuchien: tongDiemKhoa
      });
      grandThanhTien += tongThanhTienKhoa;
      grandChiPhiVattu += tongChiPhiVattuKhoa;
      grandConLai += tongConLaiKhoa;
      grandDiem += tongDiemKhoa;
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