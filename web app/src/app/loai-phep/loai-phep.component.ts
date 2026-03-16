import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { LoaiPhepService } from '../services/loai-phep.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-loai-phep',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './loai-phep.component.html',
  styleUrls: ['./loai-phep.component.css']
})
export class LoaiPhepComponent implements OnInit {
  ds_loai_phep: any[] = [];
  totalCount: number = 0;
  currentPage: number = 1;
  pageSize: number = 15;
  searchForm: FormGroup;
  titleModal: string = 'Cập nhật loại nghỉ phép'
  loai_phep_selected: any = null
  inforForm: FormGroup
  error: string = ''
  isError: boolean = false

  isAdd: boolean = false
  closeEditModal() {
    this.isAdd = false
    this.isError = false
    this.loai_phep_selected = null
  }
  constructor(private loaiPhepService: LoaiPhepService, private fb: FormBuilder, private router: Router) {
    this.searchForm = this.fb.group({
      searchTerm: ['']
    });
    this.inforForm = this.fb.group({
      ten_loai_phep: ['', Validators.required],
      so_ngay_nghi: ['', Validators.required]
    })
  }
  ngOnInit(): void {
    this.searchLoaiPhep();
  }
  updateLoaiPhep(id: number): void {
    const loaiPhep = this.ds_loai_phep.find(lp => lp.id === id);
    if (loaiPhep) {
      this.loai_phep_selected = loaiPhep;
      this.inforForm.patchValue({
        ten_loai_phep: loaiPhep.ten_loai_phep,
        so_ngay_nghi: loaiPhep.so_ngay_nghi
      });
      this.titleModal = 'Cập nhật loại nghỉ phép';
    }
  }

  addLoaiPhep(): void {
    this.isAdd = true
    this.loai_phep_selected = null;
    this.inforForm.reset();
    this.titleModal = 'Thêm loại phép mới';
  }

  saveLoaiPhep(): void {
    if (this.inforForm.valid) {
      const loaiPhepData = {
        ten_loai_phep: this.inforForm.get('ten_loai_phep')?.value,
        so_ngay_nghi: this.inforForm.get('so_ngay_nghi')?.value
      };
      
      if (this.loai_phep_selected) {
        this.loaiPhepService.updateLoaiPhep(this.loai_phep_selected.id, loaiPhepData).subscribe(
          response => {
            this.searchLoaiPhep();
            this.isError = false
            this.closeEditModal();
            alert('Cập nhật loại phép thành công!');
          },
          error => {
            console.error('Lỗi khi cập nhật loại phép:', error);
            this.isError = true
            this.error = 'Có lỗi xảy ra, cập nhật loại nghỉ phép không thành công.'
          }
        );
      } else {
        this.loaiPhepService.createLoaiPhep(loaiPhepData).subscribe(
          response => {
            this.searchLoaiPhep();
            this.isAdd = false
            this.isError = false
            this.closeEditModal();
            alert('Thêm loại phép mới thành công!');
          },
          error => {
            console.error('Lỗi khi thêm loại phép:', error);
            this.isError = true
            this.error = 'Có lỗi xảy ra, thêm loại nghỉ phép không thành công.'
          }
        );
      }
    }
    else {
      this.error = 'Thông tin nghỉ phép không hợp lệ.'
      this.isError = true
    }
  }

  searchLoaiPhep(): void {
    const searchTerm = this.searchForm.get('searchTerm')?.value || 'Nội dung tìm kiếm';
    this.loaiPhepService.getPagedLoaiPhep(this.currentPage, this.pageSize, searchTerm).subscribe(
      response => {
        this.ds_loai_phep = response.data;
        this.totalCount = response.totalRecords;
      },
      error => {
        console.error('Lỗi khi lấy dữ liệu loại phép:', error);
        if (error.status === 401) {
          localStorage.removeItem('token')
          this.router.navigate(['/login']);
        }
      }
    );
  }
  searchLoaiPhepForm(): void {
    this.currentPage = 1;
    this.searchLoaiPhep();
  }
  goToPage(page: number): void {
    this.currentPage = page;
    this.searchLoaiPhep();
  }
  nextPage(): void {
    if (this.currentPage * this.pageSize < this.totalCount) {
      this.currentPage++;
      this.searchLoaiPhep();
    }
  }
  prePage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.searchLoaiPhep();
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
