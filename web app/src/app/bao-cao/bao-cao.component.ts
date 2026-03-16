import { Component, OnInit } from '@angular/core';
import { BaoCaoRequest, BaoCaoService } from '../services/bao-cao.service';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Select2Module, Select2UpdateEvent } from 'ng-select2-component';
import { TranslateService, TranslateModule } from '@ngx-translate/core';
import { HttpClient } from '@angular/common/http';
import { saveAs } from 'file-saver';


@Component({
  selector: 'app-bao-cao',
  standalone: true,
  imports: [FormsModule, CommonModule, Select2Module, TranslateModule, ReactiveFormsModule],
  templateUrl: './bao-cao.component.html',
  styleUrls: ['./bao-cao.component.css']
})
export class BaoCaoComponent implements OnInit {
  loading: boolean = false
  searchForm: FormGroup
  ds_nhan_vien: any[] = [];
  totalCount: number = 0;
  currentPage: number = 1;
  pageSize: number = 15;
  today: string = new Date().toISOString().split('T')[0];
  constructor(private fb: FormBuilder, private translate: TranslateService,
    private http: HttpClient, private baoCaoService: BaoCaoService
  ) {
    this.translate.setDefaultLang('vi');
    const savedLang = localStorage.getItem('language') || 'vi';
    this.translate.use(savedLang);
    this.searchForm = this.fb.group({
      searchTerm: [''],
      nghi_tu: ['', Validators.required],
      nghi_den: ['', Validators.required],
    })
  }
  ngOnInit(): void {
    const now = new Date();
    const firstDay = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().split('T')[0]; // Ngày đầu tháng
    const lastDay = new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString().split('T')[0]; // Ngày cuối tháng
    this.searchForm = this.fb.group({
      searchTerm: [''],
      nghi_tu: [firstDay],
      nghi_den: [lastDay]
    });
    this.searchBaoCaoForm()
  }
  exportExcel() {
    if (this.searchForm.invalid) {
      alert(this.translate.instant('CHON_KHOANG_THOI_GIAN'));
      return;
    }
    this.loading = true
    const request = {
      tuNgay: this.searchForm.get('nghi_tu')?.value,
      denNgay: this.searchForm.get('nghi_den')?.value
    };

    this.baoCaoService.xuatBaoCaoNghiPhep(request).subscribe({
      next: (blob: Blob) => {
        const fileName = `BaoCao_NghiPhep_${request.tuNgay}_to_${request.denNgay}.xlsx`;
        saveAs(blob, fileName);
        this.loading = false
      },
      error: (err) => {
        this.loading = false
        console.error('Lỗi xuất báo cáo:', err);
        alert(this.translate.instant('CO_LOI_XR'));
      }
    });
  }
  searchBaoCaoForm() {
    if (this.searchForm.invalid) {
      alert(this.translate.instant('CHON_KHOANG_THOI_GIAN'));
      return;
    }
    this.currentPage = 1;
    this.searchBaoCao();
  }
  searchBaoCao() {
    if (this.searchForm.invalid) {
      alert(this.translate.instant('CHON_KHOANG_THOI_GIAN'));
      return;
    }
    const request: BaoCaoRequest = {
      TuNgay: this.searchForm.get('nghi_tu')?.value,
      DenNgay: this.searchForm.get('nghi_den')?.value,
      searchTerm: this.searchForm.get('searchTerm')?.value || 'All',
      Page: this.currentPage,
      PageSize: this.pageSize
    };

    this.baoCaoService.getBaoCaoNghiPhep(request)
      .subscribe({
        next: (response) => {
          this.totalCount = response.totalCount;
          this.ds_nhan_vien = response.items;
        },
        error: (error) => {
          this.ds_nhan_vien = []
          console.error('Error fetching report:', error);
          alert(this.translate.instant('CO_LOI_XR'));
        }
      });
  }
  onNghiTuChange() {
    const nghi_tu = this.searchForm.get('nghi_tu')?.value;
    const nghi_den = this.searchForm.get('nghi_den')?.value;
    if (nghi_tu && nghi_den && new Date(nghi_tu) > new Date(nghi_den)) {
      this.searchForm.get('nghi_den')?.setValue('');
      this.ds_nhan_vien = [];
    }
    else {
      if (!this.searchForm.invalid && this.searchForm.value.nghi_den != ''){
        this.searchBaoCaoForm()
      }
      else{
        this.ds_nhan_vien = []
      }
    }
  }
  onNghiDenChange() {
    if (!this.searchForm.invalid) {
      this.searchBaoCaoForm()
    }
    else {
      this.ds_nhan_vien = []
    }
  }
  goToPage(page: number): void {
    this.currentPage = page;
    this.searchBaoCao();
  }

  nextPage(): void {
    if (this.currentPage * this.pageSize < this.totalCount) {
      this.currentPage++;
      this.searchBaoCao();
    }
  }

  prePage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.searchBaoCao();
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
