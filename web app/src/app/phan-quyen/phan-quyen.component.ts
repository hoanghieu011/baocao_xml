import { Component } from '@angular/core';
import { NhanVienService, NhanVienDto, PaginatedResponse } from '../services/nhan-vien.service';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { TranslateService, TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-phan-quyen',
  standalone: true,
  imports: [FormsModule, CommonModule, ReactiveFormsModule, TranslateModule],
  templateUrl: './phan-quyen.component.html',
  styleUrls: ['./phan-quyen.component.css']
})
export class PhanQuyenComponent {
  nhanViens: NhanVienDto[] = [];
  totalCount: number = 0;
  currentPage: number = 1;
  pageSize: number = 15;
  searchForm: FormGroup;

  constructor(private nhanVienService: NhanVienService, private fb: FormBuilder,
    private router: Router, private translate: TranslateService
  ) {
    this.searchForm = this.fb.group({
      searchTerm: ['']
    });
    this.translate.setDefaultLang('vi');
    const savedLang = localStorage.getItem('language') || 'vi';
    this.translate.use(savedLang);
  }

  ngOnInit(): void {
    this.searchNhanVien();
  }

  searchNhanVien(): void {
    const searchTerm = this.searchForm.get('searchTerm')?.value || 'Nội dung tìm kiếm';
    this.nhanVienService.getPagedNhanVien(searchTerm, this.currentPage, this.pageSize).subscribe(
      (response: PaginatedResponse<NhanVienDto>) => {
        this.nhanViens = response.items;
        this.totalCount = response.totalCount;
      },
      (error: any) => {
        // console.error('Lỗi khi tải dữ liệu', error);
        if (error.status === 401) {
          localStorage.removeItem('token')
          this.router.navigate(['/login']);
        }
      }
    );
  }
  onRoleChange(nhanVien: NhanVienDto, event: Event): void {
    const checkbox = event.target as HTMLInputElement;
    const role = checkbox.getAttribute('data-role') ?? '';

    let rolesArray = nhanVien.role ? nhanVien.role.split(',') : [];
    if (checkbox.checked) {
      if (!rolesArray.includes(role)) rolesArray.push(role);
    } else {
      rolesArray = rolesArray.filter(r => r !== role);
    }

    const newRoles = rolesArray.join(',');
    const confirmUpdate = confirm(this.translate.instant('XAC_NHAN_CAP_NHAT_QUYEN') + ` ${nhanVien.ma_nv} - ${nhanVien.full_name}?`);

    if (confirmUpdate) {
      this.updateRole(nhanVien.ma_nv, newRoles, checkbox);
    } else {
      checkbox.checked = !checkbox.checked;
    }
  }


  updateRole(ma_nv: string, newRoles: string, checkbox: HTMLInputElement): void {
    this.nhanVienService.updateUserRole(ma_nv, newRoles).subscribe(
      () => {
        this.searchNhanVien();
      },
      (error) => {
        checkbox.checked = !checkbox.checked
        // console.error('Lỗi khi cập nhật quyền', error);
        if (error.status === 401) {
          localStorage.removeItem('token')
          this.router.navigate(['/login']);
        }
        else {
          alert(this.translate.instant('CO_LOI_XR'))
          this.searchNhanVien();
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
