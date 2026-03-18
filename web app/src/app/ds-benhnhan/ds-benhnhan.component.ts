import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BenhNhanService } from '../services/benh-nhan.service';
import { BorderDirective, TableDirective } from '@coreui/angular';
import { ToastModule } from '@coreui/angular';

interface BenhNhan {
  id?: number;
  ma_bn?: string;
  ho_ten?: string;
  gioi_tinh?: string;
  ngay_sinh?: string;
  ma_the_bhyt?: string;
  dia_chi?: string;
  ngay_vao?: string | Date | null;
  ngay_ra?: string | Date | null;
  chan_doan_rv?: string;
  ma_lk?: string;
  so_cccd?: string;
  nhom_mau?: string;
  ma_quoctich?: string;
  ma_dantoc?: string;
  ma_nghe_nghiep?: string;
  matinh_cu_tru?: string;
  mahuyen_cu_tru?: string;
  maxa_cu_tru?: string;
  dien_thoai?: string;
  ma_dkbd?: string;
  gt_the_tu?: string;
  gt_the_den?: string;
  ngay_mien_cct?: string;
  ly_do_vv?: string;
  ly_do_vnt?: string;
  ma_ly_do_vnt?: string;
  chan_doan_vao?: string;
  ma_benh_chinh?: string;
  ma_benh_kt?: string;
  ma_benh_yhct?: string;
  ma_pttt_qt?: string;
  ma_doituong_kcb?: string;
  ma_noi_di?: string;
  ma_noi_den?: string;
  ma_tai_nan?: string;
  ngay_vao_noi_tru?: string;
  giay_chuyen_tuyen?: string;
  so_ngay_dtri?: string;
  pp_dieu_tri?: string;
  ket_qua_dtri?: string;
  ma_loai_rv?: string;
  ghi_chu?: string;
  ngay_ttoan?: string;
  t_thuoc?: string;
  t_vtyt?: string;
  t_tongchi_bv?: string;
  t_tongchi_bh?: string;
  t_bntt?: string;
  t_bncct?: string;
  t_bhtt?: string;
  t_nguonkhac?: string;
  t_bhtt_gdv?: string;
  nam_qt?: string;
  thang_qt?: string;
  ma_loai_kcb?: string;
  ma_khoa?: string;
  ma_cskcb?: string;
  ma_khuvuc?: string;
  can_nang?: string;
  can_nang_con?: string;
  nam_nam_lien_tuc?: string;
  ngay_tai_kham?: string;
  ma_hsba?: string;
  ma_ttdv?: string;
  du_phong?: string;
  csytid?: string;
}

@Component({
  selector: 'app-ds-benhnhan',
  standalone: true,
  imports: [CommonModule, FormsModule, TableDirective, BorderDirective, ToastModule],
  templateUrl: './ds-benhnhan.component.html',
  styleUrls: ['./ds-benhnhan.component.css']
})
export class DsBenhnhanComponent implements OnInit {
  Math = Math;

  tuNgay: string = '';
  denNgay: string = '';
  searchTerm: string = '';

  pageNumber: number = 1;
  pageSize: number = 10;
  pageSizeOptions = [10, 25, 50, 100];
  totalRecords: number = 0;

  dsBenhNhan: BenhNhan[] = [];
  loading = false;

  selectedBN: BenhNhan | null = null;
  showModal = false;

  constructor(private benhNhanService: BenhNhanService) {}

  ngOnInit(): void {
    this.setDefaultMonthRange();
    this.loadData(true);
  }

  private setDefaultMonthRange() {
    const now = new Date();
    const first = new Date(now.getFullYear(), now.getMonth(), 1);
    const last = new Date(now.getFullYear(), now.getMonth() + 1, 0);

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
    if (resetPage) this.pageNumber = 1;

    if (!this.tuNgay || !this.denNgay) {
      this.addToast('Từ ngày / đến ngày không được để trống');
      return;
    }

    if (this.tuNgay > this.denNgay) {
      this.addToast('Từ ngày không được lớn hơn đến ngày');
      return;
    }

    this.loading = true;
    
    const tu = this.tuNgay ? new Date(this.tuNgay + 'T00:00:00') : undefined;
    const den = this.denNgay ? new Date(this.denNgay + 'T23:59:59') : undefined;

    this.benhNhanService.getDsBenhNhan(
      this.pageNumber,
      this.pageSize,
      this.searchTerm && this.searchTerm.trim().length ? this.searchTerm.trim() : '',
      tu,
      den
    ).subscribe({
      next: (res) => {
        this.totalRecords = res?.totalRecords ?? 0;
        this.pageNumber = res?.pageIndex ?? this.pageNumber;
        this.pageSize = res?.pageSize ?? this.pageSize;
        this.dsBenhNhan = res?.dsBenhNhan ?? [];
        console.log(res?.dsBenhNhan)
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

  onRowClick(bn: BenhNhan) {
    this.selectedBN = bn;
    this.showModal = true;
  }

  closeModal() {
    this.showModal = false;
    this.selectedBN = null;
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
    const d = new Date(value);
    if (isNaN(d.getTime())) return String(value);
    return d.toLocaleDateString('vi-VN');
  }

  rowIndex(i: number) {
    return (this.pageNumber - 1) * this.pageSize + i + 1;
  }

  fields = [
    { key: 'ma_bn', label: 'Mã BN' },
    { key: 'ho_ten', label: 'Họ tên' },
    { key: 'gioi_tinh', label: 'Giới tính' },
    { key: 'ngay_sinh', label: 'Ngày sinh' },
    { key: 'ma_the_bhyt', label: 'Mã BHYT' },
    { key: 'dia_chi', label: 'Địa chỉ' },
    { key: 'ngay_vao', label: 'Ngày vào viện' },
    { key: 'ngay_ra', label: 'Ngày ra viện' },
    { key: 'chan_doan_rv', label: 'Chẩn đoán ra viện' },
    { key: 'ma_lk', label: 'Mã LK' },
    { key: 'so_cccd', label: 'Số CCCD' },
    { key: 'nhom_mau', label: 'Nhóm máu' },
    { key: 'ma_quoctich', label: 'Mã quốc tịch' },
    { key: 'ma_dantoc', label: 'Mã dân tộc' },
    { key: 'ma_nghe_nghiep', label: 'Mã nghề nghiệp' },
    { key: 'matinh_cu_tru', label: 'Mã tỉnh cư trú' },
    { key: 'mahuyen_cu_tru', label: 'Mã huyện cư trú' },
    { key: 'maxa_cu_tru', label: 'Mã xã cư trú' },
    { key: 'dien_thoai', label: 'Điện thoại' },
    { key: 'ma_dkbd', label: 'Mã ĐKBD' },
    { key: 'gt_the_tu', label: 'GT thẻ từ' },
    { key: 'gt_the_den', label: 'GT thẻ đến' },
    { key: 'ngay_mien_cct', label: 'Ngày miễn CCT' },
    { key: 'ly_do_vv', label: 'Lý do vào viện' },
    { key: 'ly_do_vnt', label: 'Lý do VNT' },
    { key: 'ma_ly_do_vnt', label: 'Mã lý do VNT' },
    { key: 'chan_doan_vao', label: 'Chẩn đoán vào' },
    { key: 'ma_benh_chinh', label: 'Mã bệnh chính' },
    { key: 'ma_benh_kt', label: 'Mã bệnh kèm theo' },
    { key: 'ma_benh_yhct', label: 'Mã bệnh YHCT' },
    { key: 'ma_pttt_qt', label: 'Mã PTTT/QT' },
    { key: 'ma_doituong_kcb', label: 'Mã đối tượng KCB' },
    { key: 'ma_noi_di', label: 'Mã nơi đi' },
    { key: 'ma_noi_den', label: 'Mã nơi đến' },
    { key: 'ma_tai_nan', label: 'Mã tai nạn' },
    { key: 'ngay_vao_noi_tru', label: 'Ngày vào nội trú' },
    { key: 'giay_chuyen_tuyen', label: 'Giấy chuyển tuyến' },
    { key: 'so_ngay_dtri', label: 'Số ngày điều trị' },
    { key: 'pp_dieu_tri', label: 'PP điều trị' },
    { key: 'ket_qua_dtri', label: 'Kết quả điều trị' },
    { key: 'ma_loai_rv', label: 'Mã loại RV' },
    { key: 'ghi_chu', label: 'Ghi chú' },
    { key: 'ngay_ttoan', label: 'Ngày t.toán' },
    { key: 't_thuoc', label: 'T thuốc' },
    { key: 't_vtyt', label: 'T VTYT' },
    { key: 't_tongchi_bv', label: 'T tổng chi BV' },
    { key: 't_tongchi_bh', label: 'T tổng chi BH' },
    { key: 't_bntt', label: 'T BN TT' },
    { key: 't_bncct', label: 'T BN CCT' },
    { key: 't_bhtt', label: 'T BHTT' },
    { key: 't_nguonkhac', label: 'T nguồn khác' },
    { key: 't_bhtt_gdv', label: 'T BHTT GDV' },
    { key: 'nam_qt', label: 'Năm QT' },
    { key: 'thang_qt', label: 'Tháng QT' },
    { key: 'ma_loai_kcb', label: 'Mã loại KCB' },
    { key: 'ma_khoa', label: 'Mã khoa' },
    { key: 'ma_cskcb', label: 'Mã CSKCB' },
    { key: 'ma_khuvuc', label: 'Mã khu vực' },
    { key: 'can_nang', label: 'Cân nặng' },
    { key: 'can_nang_con', label: 'Cân nặng con' },
    { key: 'nam_nam_lien_tuc', label: 'Năm liên tục' },
    { key: 'ngay_tai_kham', label: 'Ngày tái khám' },
    { key: 'ma_hsba', label: 'Mã HSBA' },
    { key: 'ma_ttdv', label: 'Mã TTDV' },
    { key: 'du_phong', label: 'Dự phòng' }
  ];
  get colSize(): number {
    return Math.ceil(this.fields.length / 3);
  }
  getValue(key: string): string {
   const val = (this.selectedBN as any)?.[key];
   if (val === null || val === undefined || val === '') return '-';
   if (key.toLowerCase().includes('ngay') || key.toLowerCase().includes('gt_the') || key.toLowerCase().includes('gt_the')) {
     return this.formatDate(val);
   }
   return String(val);
  }
}