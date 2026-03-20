import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DichVuService } from '../services/dich-vu.service';
import { LoaiDichVuService } from '../services/loai-dich-vu.service';
import { BorderDirective, TableDirective } from '@coreui/angular';
import { ToastModule } from '@coreui/angular';
import { Select2Module, Select2Data, Select2UpdateEvent } from 'ng-select2-component';

@Component({
  selector: 'app-dich-vu',
  standalone: true,
  imports: [CommonModule, FormsModule, TableDirective, BorderDirective, ToastModule, Select2Module],
  templateUrl: './dich-vu.component.html',
  styleUrl: './dich-vu.component.css'
})
export class DichVuComponent {
  Math = Math;

  cur_loai_dichvu: number = 0;
  ds_loai_dichvu: any[] = [];

  searchTerm: string = '';
  pageNumber: number = 1;
  pageSize: number = 50;
  pageSizeOptions = [10, 25, 50, 100];
  totalRecords: number = 0;

  dsDichVu: any[] = [];
  loading = false;

  selectedDV: any | null = null;
  editChiPhi: number | null = null;
  editHeSo: number | null = null;
  saving = false;
  showModal = false;

  constructor(private dichVuService: DichVuService, private loaiDichVuService: LoaiDichVuService) {
    this.loadDsLoaiDichVu();
  }

  loadDsLoaiDichVu() {
    this.loaiDichVuService.getDsLoaiDichVu().subscribe({
      next: (res) => {
        const data = res?.dsLoaiDichVu || [];

        this.ds_loai_dichvu = [
          { value: 0, label: 'Tất cả' },
          ...data.map((x: any) => ({
            value: x.nhom_mabhyt_id,
            label: x.tennhom
          }))
        ];

        this.cur_loai_dichvu = 0;
      },
      error: (err) => {
        console.error(err);
        this.ds_loai_dichvu = [{ value: 0, label: 'Tất cả' }];
        this.cur_loai_dichvu = 0;
      }
    });
  }

  ngOnInit(): void {
    this.loadDsLoaiDichVu();
    this.loadData(true);
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

  loadData(resetPage: boolean = false) {
    if (resetPage) this.pageNumber = 1;

    this.loading = true;

    this.dichVuService.getDsDichVu(
      this.pageNumber,
      this.pageSize,
      this.searchTerm && this.searchTerm.trim().length ? this.searchTerm.trim() : '',
      this.cur_loai_dichvu
    ).subscribe({
      next: (res) => {
        this.totalRecords = res?.totalRecords ?? 0;
        this.pageNumber = res?.pageIndex ?? this.pageNumber;
        this.pageSize = res?.pageSize ?? this.pageSize;
        this.dsDichVu = res?.dsDichVu ?? [];
        this.loading = false;
      },
      error: (err) => {
        this.addToast('Có lỗi xảy ra, vui lòng thử lại sau!');
        this.loading = false;
      }
    });
  }

  onPrev() {
    if (this.pageNumber > 1) {
      this.pageNumber--;
      this.loadData();
    }
  }

  onNext() {
    const maxPage = Math.max(1, Math.ceil(this.totalRecords / this.pageSize));
    if (this.pageNumber < maxPage) {
      this.pageNumber++;
      this.loadData();
    }
  }

  onPageSizeChange(newSize: number) {
    this.pageSize = Number(newSize);
    this.pageNumber = 1;
    this.loadData(true);
  }

  onRowClick(dv: any) {
    this.selectedDV = dv;
    this.editChiPhi = dv.chiphi != null ? Number(dv.chiphi) : null;
    this.editHeSo = dv.heso != null ? Number(dv.heso) : null;
    this.showModal = true;
  }

  closeModal() {
    if (this.saving) return;
    this.showModal = false;
    this.selectedDV = null;
    this.editChiPhi = null;
    this.editHeSo = null;
  }

  // helper hiển thị ngày theo format dd/MM/yyyy
formatDate(value: any): string {
  if (!value) return '-';
  if (typeof value === 'string' && /^\d{12}$/.test(value)) {
    const year = value.substring(0, 4);
    const month = value.substring(4, 6);
    const day = value.substring(6, 8);
    return `${day}/${month}/${year}`;
  }
  if (typeof value === 'string' && /^\d{8}$/.test(value)) {
    const year = value.substring(0, 4);
    const month = value.substring(4, 6);
    const day = value.substring(6, 8);
    return `${day}/${month}/${year}`;
  }

  const d = new Date(value);
  if (isNaN(d.getTime())) return String(value);

  return d.toLocaleDateString('vi-VN');
}

  rowIndex(i: number) {
    return (this.pageNumber - 1) * this.pageSize + i + 1;
  }
  getValue(key: string): string {
   const val = (this.selectedDV as any)?.[key];
   if (val === null || val === undefined || val === '') return '-';
   if (key.toLowerCase().includes('ngay') || key.toLowerCase().includes('gt_the') || key.toLowerCase().includes('gt_the') || key.toLowerCase().includes('nam_nam_lien_tuc')) {
     return this.formatDate(val);
   }
  if (key.toLowerCase().includes('gia_bhyt') || key.toLowerCase().includes('chiphi')) {
    const num = Number(val);
    if (isNaN(num)) return '-';

    return new Intl.NumberFormat('vi-VN', {
      minimumFractionDigits: 0,
      maximumFractionDigits: 2
    }).format(num);
  }
   return String(val);
  }

  update(event: Select2UpdateEvent<any>) {
    this.cur_loai_dichvu = event.value;
    this.loadData(true);
  }

  saveDichVu() {
    if (!this.selectedDV?.dichvuid) {
      this.addToast('Không tìm thấy dịch vụ để cập nhật.', 'danger');
      return;
    }

    if (this.editChiPhi == null || this.editHeSo == null) {
      this.addToast('Vui lòng nhập đủ chi phí và hệ số.', 'warning');
      return;
    }

    this.saving = true;

    this.dichVuService.updateDichVu(
      this.selectedDV.dichvuid,
      Number(this.editChiPhi),
      Number(this.editHeSo)
    ).subscribe({
      next: () => {
        this.saving = false;
        this.addToast('Cập nhật dịch vụ thành công.', 'success');
        this.closeModal();
        this.loadData(true);
      },
      error: (err) => {
        this.saving = false;
        this.addToast('Cập nhật không thành công.', 'danger');
      }
    });
  } 

  formatCurrency(value: any): string {
  if (value == null || value === '') return '';
  return Number(value).toLocaleString('vi-VN');
}

  onChiPhiChange(value: string) {
    if (!value) {
      this.editChiPhi = null;
      return;
    }

    const raw = value.replace(/[^0-9]/g, '');
    this.editChiPhi = raw ? Number(raw) : null;
  }
}