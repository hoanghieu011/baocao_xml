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
import { PhepTonService } from '../services/phep-ton.service'
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-phep-ton',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, Select2Module, TranslateModule],
  templateUrl: './phep-ton.component.html',
  styleUrl: './phep-ton.component.css'
})
export class PhepTonComponent {
  loading: boolean = false;
  selectedYear: string = '';
  @ViewChild('fileInput') fileInput!: ElementRef;

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      return;
    }
    const file: File = input.files[0];
    this.loading = true;
    this.phepTonService.importPhepTon(this.selectedYear, file).subscribe({
      next: (blob: Blob) => {
        alert(this.translate.instant('CAP_NHAT_PT_TC'))
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'import_result.xlsx';
        a.click();
        window.URL.revokeObjectURL(url);
        this.loading = false;
        this.loadYears();
      },
      error: (err: HttpErrorResponse) => {
        if (err.error instanceof Blob) {
          const reader = new FileReader();
          reader.onload = (e: any) => {
            try {
              const errorJson = JSON.parse(e.target.result);
              if (errorJson.code == 1) {
                alert(this.translate.instant('DU_LIEU_PT_DA_TON_TAI') + ' (' + this.selectedYear + ')')
              }
              else if (errorJson.code == 2) {
                alert(this.translate.instant('NAM_KO_HL'))
              }
              else {
                alert(this.translate.instant('CO_LOI_XR'))
              }
            } catch (ex) {
              alert(this.translate.instant('CO_LOI_XR'))
            }
          };
          reader.readAsText(err.error);
          this.loading = false
        } else {
          alert(this.translate.instant('CO_LOI_XR'))
          console.error('Lỗi:', err);
          this.loading = false
        }
      }
    });
  }
  importExcel() {
    const isConfirmed = confirm(this.translate.instant('XAC_NHAN_IMPORT_PT'));
    if (!isConfirmed) {
      return;
    }
    const year = prompt(this.translate.instant('NHAP_NAM'));
    if (year && year.trim()) {
      this.selectedYear = year.trim();
      this.fileInput.nativeElement.click();
    }
  }

  ds_nhan_vien: any[] = [];
  ds_year: any[] = [];
  totalCount: number = 0;
  currentPage: number = 1;
  pageSize: number = 15;
  searchForm: FormGroup;
  selected_year: string = '';
  titleModal: string = ''

  nhan_vien_selected: any = null
  inforForm: FormGroup;
  isError: boolean = false
  error: string = ''
  closeEditModal() {
    this.nhan_vien_selected = null;
    this.isError = false
  }
  openEdit(nhan_vien: any) {
    this.nhan_vien_selected = { ...nhan_vien };
    this.inforForm.patchValue({ phep_ton: nhan_vien.phepTon });
    this.titleModal = `${nhan_vien.maNv}   ${nhan_vien.tenNhanVien}`;
  }
  saveNhanVien() {
    console
    if (this.inforForm.invalid) {
      this.isError = true;
      this.error = 'SO_NGAY_PHEP_TON_KHONG_HOP_LE';
      return;
    }

    const phepTonValue = Number(this.inforForm.get('phep_ton')?.value);
    if (!Number.isInteger(phepTonValue) || phepTonValue < 0) {
      this.isError = true;
      this.error = 'SO_NGAY_PHEP_TON_KHONG_HOP_LE';
      return;
    }

    this.phepTonService.updatePhepTon(this.nhan_vien_selected.maNv, this.selected_year, phepTonValue)
      .subscribe(
        () => {
          this.nhan_vien_selected.phepTon = phepTonValue;
          alert(this.translate.instant('CAP_NHAT_PHEP_TON_TC'))
          this.closeEditModal();
          this.searchNhanVien();
          this.isError = false
        },
        (error) => {
          this.isError = true;
          this.error = 'CO_LOI_XR';
        }
      );
  }

  update(event: Select2UpdateEvent<any>) {
    this.searchNhanVienForm();
  }
  removeVietnameseTones(str: string): string {
    return str
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .replace(/đ/g, "d")
      .replace(/Đ/g, "D");
  }

  constructor(
    private authService: AuthService,
    private nhanVienService: NhanVienService,
    private fb: FormBuilder,
    private router: Router,
    private translate: TranslateService,
    private phepTonService: PhepTonService
  ) {
    this.searchForm = this.fb.group({
      searchTerm: [''],
      year: ['']
    });
    this.inforForm = this.fb.group({
      phep_ton: [
        '',
        [Validators.required, Validators.pattern('^[0-9]+$')]
      ]
    });

    this.translate.setDefaultLang('vi');
    const savedLang = localStorage.getItem('language') || 'vi';
    this.translate.use(savedLang);
  }

  ngOnInit(): void {
    this.loadYears();
  }
  loadYears(): void {
    this.phepTonService.getDistinctYears().subscribe(
      (years) => {
        this.ds_year = years.map(y => ({ value: y, label: y }));
        if (this.ds_year.length > 0) {
          this.selected_year = this.ds_year[0].value
          this.searchNhanVien()
        }
      },
      (error) => {
        // console.error('Lỗi khi tải danh sách năm:', error);
      }
    );
  }


  searchNhanVien(): void {
    const searchTerm = this.searchForm.get('searchTerm')?.value || 'All';
    const year = this.selected_year || '';

    this.phepTonService.getDsPhepTon(year, searchTerm, this.currentPage, this.pageSize).subscribe(
      (response) => {
        this.ds_nhan_vien = response.items;
        this.totalCount = response.totalCount;
      },
      (error) => {
        // console.error('Lỗi khi tải danh sách nhân viên:', error);
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
