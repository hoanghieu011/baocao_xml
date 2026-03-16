import { Component, OnInit } from '@angular/core';
import { BaoCaoService } from '../services/bao-cao.service';
import { FormBuilder, FormGroup, FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Select2Module, Select2UpdateEvent } from 'ng-select2-component';
import { Chart, registerables } from 'chart.js';
import { LeaveDays } from '../models/leave-days.model';
import { Router } from '@angular/router';

@Component({
  selector: 'app-bao-cao-nhan-vien',
  standalone: true,
  imports: [FormsModule, CommonModule, Select2Module],
  templateUrl: './bao-cao-nhan-vien.component.html',
  styleUrl: './bao-cao-nhan-vien.component.css'
})
export class BaoCaoNhanVienComponent implements OnInit {
  ds_types = [
    { value: 'Tháng', label: 'Tháng' },
    { value: 'Quý', label: 'Quý' },
    { value: 'Năm', label: 'Năm' }
  ];
  ds_thoi_gian: any[] = [];
  selectedReportType = this.ds_types[0].value;
  selectedTimeValue: any;

  constructor(private baoCaoService: BaoCaoService, private fb: FormBuilder, private router: Router) {
    this.searchForm = this.fb.group({
      searchTerm: ['']
    });
  }

  ngOnInit(): void {
    this.loadData();
  }
  update(event: Select2UpdateEvent<any>) {
    this.loadData()
  }
  update2(event: Select2UpdateEvent<any>) {
    this.searchNhanVienForm();
  }
  loadData() {
    switch (this.selectedReportType) {
      case 'Tháng':
        this.baoCaoService.getMonths().subscribe(data => {
          this.ds_thoi_gian = data.map(item => ({
            value: `${item.month}/${item.year}`,
            label: `Tháng ${item.month} năm ${item.year}`
          }));
          this.selectedTimeValue = this.ds_thoi_gian[0].value;
          this.searchNhanVien()
        });
        break;
      case 'Quý':
        this.baoCaoService.getQuarters().subscribe(data => {
          this.ds_thoi_gian = data.map(item => ({
            value: `Q${item.quarter}/${item.year}`,
            label: `Quý ${item.quarter} năm ${item.year}`
          }));
          this.selectedTimeValue = this.ds_thoi_gian[0].value;
        });
        break;
      case 'Năm':
        this.baoCaoService.getYears().subscribe(data => {
          this.ds_thoi_gian = data.map(item => ({
            value: `${item}`,
            label: `${item}`
          }));
          this.selectedTimeValue = this.ds_thoi_gian[0].value;
        });
        break;
    }
  }
  parseSelectedTimeValue(): [string, Date] | [null, null] {
    if (!this.selectedTimeValue) return [null, null];

    let date;
    switch (this.selectedReportType) {
      case 'Tháng':
        const [month, year] = this.selectedTimeValue.split('/').map(Number);
        date = new Date(year, month);
        return ['tháng', date];
      case 'Quý':
        const quarter = parseInt(this.selectedTimeValue[1], 10);
        const quarterYear = parseInt(this.selectedTimeValue.split('/')[1], 10);
        date = new Date(quarterYear, (quarter) * 3);
        return ['quý', date];
      case 'Năm':
        const year_1 = parseInt(this.selectedTimeValue) + 1
        date = new Date(year_1, 0, 1);
        return ['năm', date];
      default:
        return [null, null];
    }
  }
  ds_nhan_vien: any = []
  totalCount: number = 0;
  currentPage: number = 1;
  pageSize: number = 15;
  searchForm: FormGroup;
  titleModal: string = 'Cập nhật loại nghỉ phép'
  loai_phep_selected: any = null


  searchNhanVien(): void {
    const searchTerm = this.searchForm.get('searchTerm')?.value || 'Nội dung tìm kiếm';
    const [timeFrame, inputDate] = this.parseSelectedTimeValue();

    if (timeFrame && inputDate) {
      this.baoCaoService.getLeaveSummaryByEmployee(
        timeFrame,
        inputDate,
        searchTerm,
        this.currentPage,
        this.pageSize
      ).subscribe(response => {
        this.ds_nhan_vien = response.results; 
        this.totalCount = response.totalCount;
      }, error => {
        console.error('Error fetching employee leave summary:', error);
      });
    }
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