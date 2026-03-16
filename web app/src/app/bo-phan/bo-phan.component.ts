import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { BoPhanService } from '../services/bo-phan.service';

@Component({
  selector: 'app-bo-phan',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './bo-phan.component.html',
  styleUrls: ['./bo-phan.component.css']
})
export class BoPhanComponent implements OnInit {
  ds_bo_phan: any[] = [];
  totalCount: number = 0;
  currentPage: number = 1;
  pageSize: number = 15;
  searchForm: FormGroup;
  titleModal: string = 'Cập nhật bộ phận';
  bo_phan_selected: any = null;
  inforForm: FormGroup;
  error: string = '';
  isError: boolean = false;

  isAdd: boolean = false;

  constructor(
    private boPhanService: BoPhanService,
    private fb: FormBuilder,
    private router: Router
  ) {
    this.searchForm = this.fb.group({
      searchTerm: ['']
    });
    this.inforForm = this.fb.group({
      ten_bo_phan: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.searchBoPhan();
  }

  closeEditModal(): void {
    this.isAdd = false;
    this.isError = false;
    this.bo_phan_selected = null;
  }

  updateBoPhan(id: number): void {
    const boPhan = this.ds_bo_phan.find(bp => bp.id === id);
    if (boPhan) {
      this.bo_phan_selected = boPhan;
      this.inforForm.patchValue({
        ten_bo_phan: boPhan.ten_bo_phan
      });
      this.titleModal = 'Cập nhật bộ phận';
    }
  }

  addBoPhan(): void {
    this.isAdd = true;
    this.bo_phan_selected = null;
    this.inforForm.reset();
    this.titleModal = 'Thêm bộ phận mới';
  }

  saveBoPhan(): void {
    if (this.inforForm.valid) {
      const boPhanData: any = this.inforForm.value;

      if (this.bo_phan_selected) {
        this.boPhanService.updateBoPhan(this.bo_phan_selected.id, boPhanData.ten_bo_phan).subscribe(
          () => {
            this.searchBoPhan();
            this.isError = false;
            this.closeEditModal();
            alert('Cập nhật bộ phận thành công!');
          },
          error => {
            if (error.status === 401) {
              this.router.navigate(['/login']);
            } else {
              console.error('Lỗi khi cập nhật bộ phận:', error);
              this.isError = true;
              this.error = 'Có lỗi xảy ra, cập nhật bộ phận không thành công.';
            }
          }
        );
      } else {
        this.boPhanService.createBoPhan(boPhanData.ten_bo_phan).subscribe(
          () => {
            this.searchBoPhan();
            this.isAdd = false;
            this.isError = false;
            this.closeEditModal();
            alert('Thêm bộ phận mới thành công!');
          },
          error => {
            if (error.status === 401) {
              this.router.navigate(['/login']);
            } else {
              console.error('Lỗi khi thêm bộ phận:', error);
              this.isError = true;
              this.error = 'Có lỗi xảy ra, thêm bộ phận không thành công.';
            }
          }
        );
      }
    } else {
      this.error = 'Thông tin bộ phận không hợp lệ.';
      this.isError = true;
    }
  }

  searchBoPhan(): void {
    const searchTerm = this.searchForm.get('searchTerm')?.value || 'Nội dung tìm kiếm';
    this.boPhanService.getBoPhans(this.currentPage, this.pageSize, searchTerm).subscribe(
      response => {
        this.ds_bo_phan = response.items;
        this.totalCount = response.totalItems;
      },
      error => {
        console.error('Lỗi khi lấy dữ liệu bộ phận:', error);
        if (error.status === 401) {
          localStorage.removeItem('token')
          this.router.navigate(['/login']);
        } else {
          console.log('Không thể tải bộ phận.')
        }
      }
    );
  }

  searchBoPhanForm(): void {
    this.currentPage = 1;
    this.searchBoPhan();
  }

  goToPage(page: number): void {
    this.currentPage = page;
    this.searchBoPhan();
  }

  nextPage(): void {
    if (this.currentPage * this.pageSize < this.totalCount) {
      this.currentPage++;
      this.searchBoPhan();
    }
  }

  prePage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.searchBoPhan();
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
