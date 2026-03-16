import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormGroup, FormBuilder, ReactiveFormsModule, FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { QuillModule } from 'ngx-quill';
import { NghiPhepService, NghiPhepSearchDto, BatchUpdateStatusDto } from '../services/nghi-phep.service'
import { DatePipe } from '@angular/common';
import { Select2Module, Select2Data, Select2UpdateEvent } from 'ng-select2-component';
import { AuthService } from '../services/auth.service';
import { NhanVienService } from '../services/nhan-vien.service';
import { Subscriber } from 'rxjs';
import { TranslateService, TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-quan-ly-phieu-nghi',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, QuillModule, DatePipe, Select2Module, TranslateModule],
  providers: [DatePipe],
  templateUrl: './quan-ly-phieu-nghi.component.html',
  styleUrl: './quan-ly-phieu-nghi.component.css'
})
export class QuanLyPhieuNghiComponent implements OnInit {
  isSubmitting = false;
  searchForm: FormGroup;
  inforForm: FormGroup;
  currentStatus: string = 'Tất cả'
  currentPage: number = 1;
  pageSize: number = 15;
  phieu_nghi_selected: any = null
  totalCount: number = 0;
  ds_phieu_nghi: any[] = []
  ds_phieu_nghi_selected: any[] = []
  ly_do_selected = new FormControl('');
  userInfo: any
  ly_do_err: boolean = false
  ds_trang_thai: any[] = [];
  updateStatusList() {
    const selectedValue = this.trang_thai;
    this.translate.get(['TAT_CA', 'CHUA_XU_LY', 'DA_DUYET', 'TUCHOI', 'DA_HUY']).subscribe(translations => {
      this.ds_trang_thai = [
        { value: 'Tất cả', label: translations['TAT_CA'] },
        { value: 'Chưa xử lý', label: translations['CHUA_XU_LY'] },
        { value: 'Đã duyệt', label: translations['DA_DUYET'] },
        { value: 'Từ chối', label: translations['TUCHOI'] },
        { value: 'Đã hủy', label: translations['DA_HUY']}
      ];
      this.searchForm.patchValue({ status: null });
      setTimeout(() => {
        this.searchForm.patchValue({ status: selectedValue });
      }, 10);
    });
  }
  trang_thai = 'Tất cả'
  update(event: Select2UpdateEvent<any>) {
    this.trang_thai = event.value;
    this.searchPhieuNghiForm();
  }


  ngOnInit(): void {
    this.userInfo = this.authService.getUserInfo();
    this.ds_phieu_nghi = [];
    this.ds_phieu_nghi_selected = [];
    this.searchPhieuNghi();
  }
  constructor(private datePipe: DatePipe, private nghiPhepService: NghiPhepService,
    private fb: FormBuilder, private router: Router, private translate: TranslateService,
    private authService: AuthService, private nhanVienService: NhanVienService) {
    this.searchForm = this.fb.group({
      searchTerm: [''],
      status: [this.currentStatus]
    });
    this.inforForm = this.fb.group({
      full_name: [''],
      ma_nv: [''],
      bo_phan: [''],
      loai_phep_name: [''],
      so_ngay_nghi: [''],
      ly_do: [''],
      ban_giao: [''],
      ngay_tao: [''],
      trang_thai: [''],
      ngay_xu_ly_1: [''],
      ngay_xu_ly_2: [''],
      ngay_xu_ly_3: [''],
      ngay_nghi: [''],
      nv_xu_ly_1: [''],
      nv_xu_ly_2: [''],
      nv_xu_ly_3: [''],
      ngay_xu_ly_4: [''],
      nghi_tu: [''],
      vi_tri: [''],
      cong_viec: [''],
      nghi_den: [''],
      nv_xu_ly_name_1: [''],
      nv_xl_bp_1: [''],
      nv_xl_vt_1: [''],
      nv_xu_ly_name_2: [''],
      nv_xl_bp_2: [''],
      nv_xl_vt_2: [''],
      nv_xu_ly_name_3: [''],
      nv_xl_bp_3: [''],
      nv_xl_vt_3: [''],
      ly_do_tc: [''],
      nv_xu_ly_name_4: [''],
      nv_xl_bp_4: [''],
      nv_xl_vt_4: [''],
      ly_do_huy: ['']
    })
    this.translate.setDefaultLang('vi');
    const savedLang = localStorage.getItem('language') || 'vi';
    this.translate.use(savedLang);
    this.updateStatusList();
    this.searchForm.patchValue({ status: 'Chưa xử lý' });
    this.translate.onLangChange.subscribe(() => {
      this.updateStatusList();
    })
  }
  ly_do_huy: string = ''
  ly_do_err_huy: boolean = true;
  showHuyModal: boolean = false
  openLyDoHuy() {
    this.showHuyModal = true;
    this.ly_do_huy = ''
    this.ly_do_err_huy = false
  }
  huyTuChoi() {
    this.showHuyModal = false;
    this.ly_do_huy = ''
    this.ly_do_err_huy = false
  }
  xacNhanHuy() {
    if (!this.ly_do_huy.trim()) {
      this.ly_do_err_huy = true;
      return;
    }

    this.nghiPhepService.huyPhieu(this.phieu_nghi_selected.id, this.ly_do_huy).subscribe({
      next: (res) => {
        alert(this.translate.instant('HUY_PHIEU_TC'));
        this.showHuyModal = false;
        this.ly_do_huy = '';
        this.searchPhieuNghiForm();
      },
      error: (err) => {
        alert(this.translate.instant('CO_LOI_XR'));
        console.error('Lỗi khi hủy piếu:', err);
      }
    });
  }
  showQuyTrinh: boolean = false
  quyTrinhData: any = { tier1: [], tier2: [], tier3: [] };
  openQuyTrinh() {
    this.nghiPhepService.getQuyTrinh(this.phieu_nghi_selected.maNv).subscribe(
      (data) => {
        this.quyTrinhData = data;
        this.showQuyTrinh = true;
      },
      (error) => {
        console.error('Lỗi khi lấy quy trình:', error);
      }
    );
  }
  closeQuyTrinh(){
    this.showQuyTrinh = false;
    this.quyTrinhData = { tier1: [], tier2: [], tier3: [] };
  }
  formatNgayNghi(dates: string): string {
    const parsedDates = JSON.parse(dates);
    if (Array.isArray(parsedDates)) {
      return parsedDates
        .map(date => this.datePipe.transform(date, 'dd/MM/yyyy'))
        .join(', ');
    }
    return '';
  }
  closeEditModal() {
    this.inforForm.reset()
    this.phieu_nghi_selected = null
  }

  capNhatTrangThai(data: BatchUpdateStatusDto) {
    this.nghiPhepService.batchUpdateStatus(data).subscribe({
      next: (response: any) => {
        const updatedCount = response.updatedCount || 0;
        if (data.Ids.length === 1) {
          if (updatedCount === 0) {
            alert(this.translate.instant('CAP_NHAT_KO_THANH_CONG'));
          } else {
            alert(this.translate.instant('CAP_NHAT_TRANG_THAI_PHIEU_TC'));
          }
        } else {
          alert(this.translate.instant('CAP_NHAT_TRANG_THAI_PHIEU_TC') + ` (${updatedCount}/${data.Ids.length})`);
        }

        this.searchPhieuNghiForm();
      },
      error: (error: HttpErrorResponse) => {
        if (error.status === 401) {
          this.router.navigate(['/login']);
        }
        else if (error.error.code === 2) {
          alert(this.translate.instant('ER_HET_PHEP_NAM'))
          this.searchPhieuNghiForm();
        }
        else {
          // console.error('Error updating leave request:', error);
          alert(this.translate.instant('CAP_NHAT_KO_THANH_CONG'));
          this.searchPhieuNghiForm();
        }
      }
    });
  }

  searchPhieuNghi() {
    this.ds_phieu_nghi_selected = [];
    this.phieu_nghi_selected = null
    if (!this.searchForm.get('status')?.value) {
      return
    }
    const searchDto: NghiPhepSearchDto = {
      trang_thai: this.searchForm.get('status')?.value || 'Tất cả',
      searchTerm: this.searchForm.get('searchTerm')?.value || 'All',
      Page: this.currentPage,
      PageSize: this.pageSize
    };

    this.nghiPhepService.dsPhieuAll(searchDto).subscribe({
      next: response => {
        this.ds_phieu_nghi = response.items;
        this.totalCount = response.totalCount;
      },
      error: (error: HttpErrorResponse) => {
        if (error.status === 401) {
          this.router.navigate(['/login']);
        } else {
          // console.error('Error fetching data:', error);
        }
      }
    });
  }
  searchPhieuNghiForm() {
    this.isSubmitting = false;
    this.currentPage = 1;
    this.searchPhieuNghi()
  }
  addHours(dateString: string | null, hours: number): Date | null {
    if (!dateString) return null;
    const date = new Date(dateString);
    date.setHours(date.getHours() + hours);
    return date;
  }
  viewPhieuNghiDetails(phieu_nghi_id: number) {
    this.phieu_nghi_selected = this.ds_phieu_nghi.find(phieu => phieu.id === phieu_nghi_id);
    this.ly_do_selected.setValue(this.phieu_nghi_selected.ly_do);
    var loai_phep_name = this.phieu_nghi_selected.kyHieuLyDo + ' - ' + this.translate.instant(this.phieu_nghi_selected.lyDoDienGiai)
    this.inforForm.patchValue({
      full_name: this.phieu_nghi_selected.fullName,
      ma_nv: this.phieu_nghi_selected.maNv,
      bo_phan: this.phieu_nghi_selected.tenBoPhan,
      loai_phep_name: loai_phep_name.replace(/^# - /, ''),
      so_ngay_nghi: this.phieu_nghi_selected.soNgayNghi,
      ly_do: this.phieu_nghi_selected.lyDoNghiStr,
      ban_giao: this.phieu_nghi_selected.banGiao,
      cong_viec: this.phieu_nghi_selected.congViec,
      ngay_tao: this.datePipe.transform(this.phieu_nghi_selected.ngayTao, 'dd/MM/yyyy'),
      ngay_nghi: this.phieu_nghi_selected.ngayNghi,
      trang_thai: this.phieu_nghi_selected.trangThai === "-1"
        ? this.translate.instant("DA_HUY")
        : (this.phieu_nghi_selected.duyet === 0
          ? this.translate.instant("TUCHOI")
          : (["0", "1", "2"].includes(this.phieu_nghi_selected.trangThai)
            ? this.translate.instant("CHUA_XU_LY")
            : (this.phieu_nghi_selected.trangThai === "3"
              ? this.translate.instant("DA_DUYET")
              : ""))),

      ngay_xu_ly_1: this.datePipe.transform(this.addHours(this.phieu_nghi_selected.ngayXuLy1, 7), 'dd/MM/yyyy, HH:mm', 'vi-VN'),
      ngay_xu_ly_2: this.datePipe.transform(this.addHours(this.phieu_nghi_selected.ngayXuLy2, 7), 'dd/MM/yyyy, HH:mm', 'vi-VN'),
      ngay_xu_ly_3: this.datePipe.transform(this.addHours(this.phieu_nghi_selected.ngayXuLy3, 7), 'dd/MM/yyyy, HH:mm', 'vi-VN'),
      ngay_xu_ly_4: this.datePipe.transform(this.addHours(this.phieu_nghi_selected.ngayHuy, 7), 'dd/MM/yyyy, HH:mm', 'vi-VN'),
      nv_xu_ly_1: this.phieu_nghi_selected.nvXuLy1,
      nv_xu_ly_2: this.phieu_nghi_selected.nvXuLy2,
      nv_xu_ly_3: this.phieu_nghi_selected.nvXuLy3,
      nv_xu_ly_name_1: '',
      nv_xl_bp_1: '',
      nv_xl_vt_1: '',
      nv_xu_ly_name_2: '',
      nv_xl_bp_2: '',
      nv_xl_vt_2: '',
      nv_xu_ly_name_3: '',
      nv_xl_bp_3: '',
      nv_xl_vt_3: '',
      nghi_tu: this.datePipe.transform(this.phieu_nghi_selected.nghiTu, 'dd/MM/yyyy'),
      nghi_den: this.datePipe.transform(this.phieu_nghi_selected.nghiDen, 'dd/MM/yyyy'),
      vi_tri: this.translate.instant(this.phieu_nghi_selected.tenViTri),
      ly_do_tc: this.phieu_nghi_selected.lyDoTuChoi,
      ly_do_huy: this.phieu_nghi_selected.lyDoHuy
    });
    this.getNhanVienDetails(this.phieu_nghi_selected.nvXuLy1, '1');
    this.getNhanVienDetails(this.phieu_nghi_selected.nvXuLy2, '2');
    this.getNhanVienDetails(this.phieu_nghi_selected.nvXuLy3, '3');
    this.getNhanVienDetails(this.phieu_nghi_selected.maNvHuy, '4')
  }
  getNhanVienDetails(nvId: string, index: string) {
    if (!nvId) return;

    this.nhanVienService.getNhanVienDetail(nvId).subscribe(response => {
      if (response) {
        const updateData: { [key: string]: string | null } = {};
        updateData[`nv_xu_ly_name_${index}`] = response.ma_nv + ' - ' + response.full_name;
        updateData[`nv_xl_bp_${index}`] = response.ten_bo_phan;
        updateData[`nv_xl_vt_${index}`] = this.translate.instant(response.ten_vi_tri);
        this.inforForm.patchValue(updateData);
      }
    });
  }


  goToPage(page: number): void {
    this.currentPage = page;
    this.searchPhieuNghi();
  }

  nextPage(): void {
    if (this.currentPage * this.pageSize < this.totalCount) {
      this.currentPage++;
      this.searchPhieuNghi();
    }
  }

  prePage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.searchPhieuNghi();
    }
  }
  getLimitedPageNumbers(): number[] {
    const totalPages = Math.ceil(this.totalCount / this.pageSize);
    const pageNumbers = Array.from({ length: totalPages }, (_, i) => i + 1);

    const maxVisiblePages = 5;
    const halfVisible = Math.floor(maxVisiblePages / 2);

    if (totalPages <= maxVisiblePages) {
      return pageNumbers;
    }

    let start = Math.max(1, this.currentPage - halfVisible);
    let end = Math.min(totalPages, this.currentPage + halfVisible);

    if (start === 1) {
      end = Math.min(maxVisiblePages, totalPages);
    } else if (end === totalPages) {
      start = Math.max(1, totalPages - maxVisiblePages + 1);
    }

    return pageNumbers.slice(start - 1, end);
  }


}
