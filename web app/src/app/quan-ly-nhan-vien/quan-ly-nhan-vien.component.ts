import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { BoPhanService } from '../services/bo-phan.service';
import { NhanVienService } from '../services/nhan-vien.service';
import { Select2Module, Select2Data, Select2UpdateEvent } from 'ng-select2-component';
import { AuthService } from '../services/auth.service';
import { ViTriService } from '../services/vi-tri.service'
import { TranslateService, TranslateModule } from '@ngx-translate/core';
import { saveAs } from 'file-saver';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-quan-ly-nhan-vien',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, Select2Module, TranslateModule],
  templateUrl: './quan-ly-nhan-vien.component.html',
  styleUrl: './quan-ly-nhan-vien.component.css'
})
export class QuanLyNhanVienComponent {
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;
  @ViewChild('updateFileInput') updateFileInput!: ElementRef<HTMLInputElement>;
  ds_nhan_vien: any[] = [];
  totalCount: number = 0;
  loading: boolean = false;
  currentPage: number = 1;
  pageSize: number = 15;
  searchForm: FormGroup;
  titleModal: string = this.translate.instant('CAP_NHAT_TT_NV');
  nhan_vien_selected: any = null;
  inforForm: FormGroup;
  error: string = '';
  isError: boolean = false;

  isAdd: boolean = false;

  ds_gioi_tinh: any[] = []
  load_ds_gioi_tinh() {
    this.ds_gioi_tinh = [
      {
        value: 'Nam', label: this.translate.instant('GT_NAM')
      },
      {
        value: 'Nữ', label: this.translate.instant('GT_NU')
      }
    ]
  }
  removeVietnameseTones(str: string): string {
    return str
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .replace(/đ/g, "d")
      .replace(/Đ/g, "D");
  }
  xuatExcel() {
    this.loading = true
    this.nhanVienService.xuatDsNhanVien().subscribe(response => {
      const blob = new Blob([response], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `Danh_sach_nhan_vien_${new Date().toLocaleDateString('vi-VN')}.xlsx`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      window.URL.revokeObjectURL(url);
      this.loading = false
    });
  }
  triggerUpdateWorkingPositionFile(): void {
    this.updateFileInput.nativeElement.click();
  }
  onUpdateWorkingPositionFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file: File = input.files[0];
      this.loading = true;
      this.nhanVienService.updateWorkingPositionFromExcel(file).subscribe({
        next: (blob: Blob) => {
          alert(this.translate.instant('CN_HOAN_TAT'))
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `ImportResult-${new Date().toISOString()}.xlsx`;
          document.body.appendChild(a);
          a.click();
          document.body.removeChild(a);
          window.URL.revokeObjectURL(url);
          this.loading = false;
          this.searchNhanVienForm();
        },
        error: (error: HttpErrorResponse) => {
          // console.error('Lỗi khi cập nhật vị trí làm việc từ Excel:', error);
          this.loading = false;
        }
      });
    }
  }
  importExcel() {
    this.fileInput.nativeElement.click();
  }
  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file: File = input.files[0];
      this.loading = true;
      this.nhanVienService.importNhanVien(file).subscribe({
        next: (blob: Blob) => {
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = 'KetQuaImportNhanVien.xlsx';
          document.body.appendChild(a);
          a.click();
          document.body.removeChild(a);
          window.URL.revokeObjectURL(url);
          this.loading = false;
          this.searchNhanVienForm();
        },
        error: (error: HttpErrorResponse) => {
          console.error('Lỗi khi import file Excel:', error);
          this.loading = false;
        }
      });
    }
  }
  ds_bo_phan: any = []
  ds_vi_tri: any = [];
  getAllBoPhan(): void {
    this.boPhanService.getAllBoPhan().subscribe(
      (data) => {
        this.ds_bo_phan = data.map((boPhan: any) => ({
          value: boPhan.id,
          label: boPhan.ten_bo_phan
        }));
      },
      (error) => {
        if (error.status === 401) {
          localStorage.removeItem('token')
          this.router.navigate(['/login']);
        } else {
          // console.error('Lỗi khi lấy danh sách bộ phận:', error);
        }
      }
    );
  }
  getAllViTri(): void {
    this.viTriService.getAllViTri().subscribe(
      (data) => {
        this.ds_vi_tri = data.map((viTri: any) => ({
          value: viTri.ma_vi_tri,
          label: this.translate.instant(viTri.ten_vi_tri)
        }));
      },
      (error) => {
        // console.error('Lỗi khi lấy danh sách vị trí:', error);
      }
    );
  }
  constructor(
    private authService: AuthService,
    private nhanVienService: NhanVienService,
    private boPhanService: BoPhanService,
    private fb: FormBuilder,
    private router: Router,
    private viTriService: ViTriService,
    private translate: TranslateService
  ) {
    this.searchForm = this.fb.group({
      searchTerm: ['']
    });
    this.inforForm = this.fb.group({
      ten_nhan_vien: ['', Validators.required],
      gioi_tinh: ['', Validators.required],
      bo_phan: [''],
      vi_tri: ['', Validators.required],
      email: ['', [Validators.email]],
      cong_viec: [''],
      ma_nhan_vien: ['', Validators.required]
    });
    this.load_ds_gioi_tinh()
    this.translate.setDefaultLang('vi');
    const savedLang = localStorage.getItem('language') || 'vi';
    this.translate.use(savedLang);
    this.translate.onLangChange.subscribe(() => {
      this.getAllViTri()
      this.load_ds_gioi_tinh()
    })
  }

  ngOnInit(): void {
    this.searchNhanVien();
    this.getAllBoPhan()
    this.getAllViTri()
  }

  closeEditModal(): void {
    this.isAdd = false;
    this.isError = false;
    this.nhan_vien_selected = null;
  }

  updateNhanVien(id: number): void {
    const nhanVien = this.ds_nhan_vien.find(nv => nv.id === id);
    if (nhanVien) {
      this.isAddSuccess = false;
      this.isResetSuccess = false;
      this.nhan_vien_selected = nhanVien;
      this.inforForm.patchValue({
        ten_nhan_vien: nhanVien.full_name,
        gioi_tinh: nhanVien.gioi_tinh,
        bo_phan: nhanVien.bo_phan_id,
        vi_tri: nhanVien.ma_vi_tri,
        email: nhanVien.email,
        cong_viec: nhanVien.cong_viec,
        ma_nhan_vien: nhanVien.ma_nv
      });
      this.titleModal = 'CAP_NHAT_TT_NV';
    }
  }

  addNhanVien(): void {
    this.isAddSuccess = false;
    this.isResetSuccess = false;
    this.isAdd = true;
    this.nhan_vien_selected = null;
    this.inforForm.reset();
    this.titleModal = this.translate.instant('THEM_NV_MOI');
    this.inforForm.patchValue({ ma_nhan_vien: 'SMTV-' })
  }
  isAddSuccess: boolean = false;
  addSuccessStr: string = ''
  saveNhanVien(): void {
    if (this.inforForm.valid) {
      var bo_phan = 0;
      var cong_viec = 'defaut'
      var email = 'defaut'
      if (this.inforForm.value.bo_phan != null || this.inforForm.value.bo_phan != '') {
        bo_phan = this.inforForm.value.bo_phan
      }
      if (this.inforForm.value.cong_viec != null || this.inforForm.value.cong_viec != '') {
        cong_viec = this.inforForm.value.cong_viec
      }
      if (this.inforForm.value.email != null || this.inforForm.value.email != '') {
        email = this.inforForm.value.email
      }
      const nhanVienData = {
        ma_nhan_vien: this.inforForm.value.ma_nhan_vien,
        full_name: this.inforForm.value.ten_nhan_vien,
        gioi_tinh: this.inforForm.value.gioi_tinh,
        bo_phan_id: bo_phan,
        ma_vi_tri: this.inforForm.value.vi_tri,
        email: email ?? null,
        cong_viec: cong_viec
      };

      if (this.nhan_vien_selected) {
        this.nhanVienService.updateNhanVien(this.nhan_vien_selected.id, nhanVienData)
          .subscribe(
            () => {
              this.isError = false;
              this.error = '';
              alert(this.translate.instant('CAP_NHAT_TT_NV_TC'));
              this.closeEditModal();
              this.searchNhanVien();
            },
            (error) => {
              if (error.status === 401) {
                localStorage.removeItem('token')
                this.router.navigate(['/login']);
              } else {
                console.error('Lỗi khi cập nhật nhân viên:', error);
                this.isError = true;
                // this.error = 'Có lỗi xảy ra, cập nhật thông tin không thành công.';
              }
            }
          );
      } else {
        const newNhanVien = {
          id: 0,
          ma_nv: nhanVienData.ma_nhan_vien,
          full_name: nhanVienData.full_name,
          gioi_tinh: nhanVienData.gioi_tinh,
          ma_vi_tri: nhanVienData.ma_vi_tri,
          email: nhanVienData.email,
          bo_phan_id: nhanVienData.bo_phan_id,
          cong_viec: nhanVienData.cong_viec
        }
        this.nhanVienService.createNhanVien(newNhanVien).subscribe(
          (response) => {
            this.isError = false;
            this.error = '';
            this.isAddSuccess = true;
            this.resetSuccessStr = this.translate.instant('PASSWORD') + ': ' + response.password
            this.searchNhanVien();
          },
          (error) => {
            // console.log(error)
            if (error.status === 401) {
              localStorage.removeItem('token')
              this.router.navigate(['/login']);
            }
            else if (error.error.code === 2) {
              this.isError = true;
              this.error = this.translate.instant('TRUNG_MA_NV')
            }
            else {
              this.isError = true;
              this.error = this.translate.instant('THEM_NV_KO_TC');
            }
          }
        );
      }
    } else {
      this.error = this.translate.instant('THONG_TIN_NV_KO_HOP_LE');
      this.isError = true;
    }
  }

  isResetSuccess: boolean = false;
  resetSuccessStr: string = ''

  resetPassword() {
    const confirmation = confirm(this.translate.instant('XAC_NHAN_DLMK') + ' ' + `${this.nhan_vien_selected.ma_nv} ${this.nhan_vien_selected.full_name}?`);
    if (confirmation) {
      const newPassword = this.generateRandomPassword(6);

      this.authService.resetPassword(this.nhan_vien_selected.ma_nv, newPassword).subscribe(
        (success) => {
          if (success) {
            this.isResetSuccess = true;
            this.resetSuccessStr = '' + newPassword
          } else {
            this.isError = true;
            this.error = this.translate.instant('DLMK_TC')
          }
        },
        (error) => {
          if (error.status === 401) {
            localStorage.removeItem('token')
            this.router.navigate(['/login']);
          } else {
            this.isError = true;
            this.error = this.translate.instant('DLMK_KO_TC')
          }
        }
      );
    }
  }

  deleteNhanVien(id: number): void {
    const nhanVien = this.ds_nhan_vien.find(nv => nv.id === id);
    // console.log(nhanVien)
    if (confirm(this.translate.instant('XAC_NHAN_XOA_NV') + ' ' + nhanVien.ma_nv + ' ' + nhanVien.full_name)) {
      this.nhanVienService.deleteNhanVien(id).subscribe({
        next: () => {
          this.ds_nhan_vien = this.ds_nhan_vien.filter(nv => nv.id !== id);
          alert(this.translate.instant('XOA_NV_TC'));
        },
        error: (error) => {
          if (error.status === 401) {
            localStorage.removeItem('token')
            this.router.navigate(['/login']);
          } else if (error.status === 409) {
            alert(this.translate.instant('XOA_NV_KO_TC'));
          } else {
            alert(this.translate.instant('XOA_NV_KO_TC'));
          }
          console.error(error);
        }
      });
    }
  }

  private generateRandomPassword(length: number): string {
    const chars = 'ABCDEFGHIJKLMNPQRSTUVWXYZabcdefghijklmnpqrstuvwxyz123456789';
    let password = '';
    for (let i = 0; i < length; i++) {
      const randomIndex = Math.floor(Math.random() * chars.length);
      password += chars[randomIndex];
    }
    return password;
  }


  searchNhanVien(): void {
    const searchTerm = this.searchForm.get('searchTerm')?.value || 'Nội dung tìm kiếm';
    this.nhanVienService.searchNhanVien(searchTerm, this.currentPage, this.pageSize).subscribe(
      response => {
        this.ds_nhan_vien = response.employees;
        this.totalCount = response.totalRecords;
        this.isError = false;
        this.error = '';
      },
      error => {
        if (error.status === 401) {
          localStorage.removeItem('token')
          this.router.navigate(['/login']);
        } else {
          this.isError = true;
          this.error = this.translate.instant('CO_LOI_XR');
        }
      }
    );
  }


  searchNhanVienForm(): void {
    this.currentPage = 1;
    this.searchNhanVien();
  }

  goToPage(page: number): void {
    this.currentPage = page;
    this.searchNhanVien();
  }

  nextPage(): void {
    if (this.currentPage * this.pageSize < this.totalCount) {
      this.currentPage++;
      this.searchNhanVien();
    }
  }

  prePage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.searchNhanVien();
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
