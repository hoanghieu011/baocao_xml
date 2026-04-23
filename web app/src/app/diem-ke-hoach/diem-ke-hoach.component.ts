import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component } from '@angular/core';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ToastModule } from '@coreui/angular';
import { Select2Module, Select2UpdateEvent } from 'ng-select2-component';
import { OrganizationService } from '../services/org-organization.service';
import { DiemKeHoachService, ThemDiemKeHoach, UpdateDiemKeHoach } from '../services/diem-ke-hoach.service';
import { TranslateModule } from '@ngx-translate/core';
import { TangCuongService } from '../services/tang-cuong.service';
import { forkJoin } from 'rxjs';

type TangCuong = {
  diemKeHoachId: number;
  khoaId: number;
  soNgay: number;
  diem: number;
  id?: number; 
};

@Component({
  selector: 'app-diem-ke-hoach',
  standalone: true,
  imports: [FormsModule, Select2Module, CommonModule, ToastModule, ReactiveFormsModule, TranslateModule],
  templateUrl: './diem-ke-hoach.component.html',
  styleUrl: './diem-ke-hoach.component.css'
})

export class DiemKeHoachComponent  {
  Math = Math;
  ds_khoa: any[] = [
  ];
  ds_khoa_tang_cuong: any[] = [
  ];
  toasts: any[] = [];
  ds_thang = Array.from({ length: 12 }, (_, i) => ({ value: i + 1, label: `Tháng ${i + 1}` }));
  ds_nam: any[] =  [];
  isShowModal = false;
  ds_diemkehoach: any[] = [];
  selectedDiemKeHoach: any = null;
  loading = false;
  formDataDiemKeHoach =  new FormGroup({
    khoa: new FormControl(0, [Validators.required]),
    bacSiId: new FormControl(0, [Validators.required]),
    diemKeHoach: new FormControl(0, [Validators.required]),
    soBuoiTruc: new FormControl(0, [Validators.required]),
    diemTruc: new FormControl(0, [Validators.required]),
    diemLayMauXN: new FormControl(0, [Validators.required]),
    thangNam: new FormControl(''),
  });
  dataTangCuong: TangCuong[] = [
  ];
  totalRecords = 0;
  pageSize = 25;
  pageSizeOptions = [10, 25, 50, 100];
  currentPage = 1;
  hesoTruc = [
    1, 1, 1, 1, 12, 1, 8, 1, 1, 1, 1, 1
  ]

  cur_khoa: number = 0;
  cur_thang: number = new Date().getMonth() + 1;
  cur_nam: number = new Date().getFullYear();
  constructor(private organizationService: OrganizationService, private diemKeHoachService: DiemKeHoachService, private tangCuongService: TangCuongService, private cd: ChangeDetectorRef) { }
  ngOnInit(): void {
    let currentYear = new Date().getFullYear();
    for (let year = currentYear - 2; year <= currentYear + 2; year++) {
      this.ds_nam.push({ value: year, label: `Năm ${year}` });
    }
    this.loadDsOrganization();
  }

  loadDsOrganization() {
    this.organizationService.getDsOrganization().subscribe({
      next: (res) => {
        const data = res?.ds_organization || [];

        this.ds_khoa = [
          ...data.map((x: any) => ({
            value: x.org_id,
            label: x.ma_khoa + ' - ' + x.org_name
          }))
        ];
        this.cur_khoa = 0;
      },
      error: (err) => {
        console.error(err);
        this.ds_khoa = [{ value: '', label: '' }];
        this.cur_khoa = 0;
      }
    });
  }

  getDsTangCuongByDiemKeHoachId() {
    if (!this.selectedDiemKeHoach) {
      this.addToast('Vui lòng chọn điểm kế hoạch', 'warning');
      return;
    }
    if(this.selectedDiemKeHoach.diemKeHoachId == 0) {
      this.dataTangCuong = [];
      const newTangCuong: TangCuong = {
        diemKeHoachId: this.selectedDiemKeHoach.diemKeHoachId,
        khoaId: this.selectedDiemKeHoach.khoaId,
        soNgay: 0,
        diem: 0
      };
      this.dataTangCuong.push(newTangCuong);
      return;
    }
    this.loading = true;
    this.tangCuongService.getAllTangCuongByDiemKeHoachId(this.selectedDiemKeHoach.diemKeHoachId).subscribe({
      next: (res) => {
        this.dataTangCuong = res?.dsTangCuong || [];
        this.loading = false;
        console.log('Data Tăng cường:', this.dataTangCuong);
        console.log('Selected Điểm kế hoạch:', this.selectedDiemKeHoach);
      },
      error: (err) => {
        console.error(err);
        this.loading = false;
       this.dataTangCuong = [];
       this.addToast('Đã xảy ra lỗi khi lấy dữ liệu tăng cường', 'danger');
      }
    });
  }


  onKhoaChange(event: Select2UpdateEvent<any>) {
    this.cur_khoa = event.value;
    this.ds_khoa_tang_cuong = this.ds_khoa.filter(k => k.value !== this.cur_khoa);
    this.loadDiemKeHoach();
  }

  onThangChange(event: Select2UpdateEvent<any>) {
    this.cur_thang = event.value;
  }
  onNamChange(event: Select2UpdateEvent<any>) {
    this.cur_nam = event.value;
  }

  onCloseModal() {
    this.isShowModal = false;
    this.formDataDiemKeHoach.reset();
  }

  onSaveDiemKeHoach() {
    if (this.formDataDiemKeHoach.invalid) {
      this.addToast('Vui lòng điền đầy đủ thông tin', 'warning');
      return;
    }
    console.log('Form Data:', this.formDataDiemKeHoach.value);
    console.log('data tănng cường:', this.dataTangCuong);
    this.loading = true;
    var diemKeHoachReq;
    var diemKeHoach; 
    if(this.selectedDiemKeHoach?.diemKeHoachId > 0) {
      diemKeHoach = {
        diemKeHoachId: parseInt(this.selectedDiemKeHoach.diemKeHoachId + '') || 0,
        diemKeHoach: this.formDataDiemKeHoach.get('diemKeHoach')?.value || 0,
        soBuoiTruc: this.formDataDiemKeHoach.get('soBuoiTruc')?.value || 0,
        diemTruc: this.formDataDiemKeHoach.get('diemTruc')?.value || 0,
        soBenhNhan: this.formDataDiemKeHoach.get('soBenhNhan')?.value || 0,
        diemLayMau: this.formDataDiemKeHoach.get('diemLayMauXN')?.value || 0,
        diemTrucCC: this.formDataDiemKeHoach.get('diemTrucCC')?.value || 0,
        officerType: parseInt(this.selectedDiemKeHoach.officerType + '') || 0,
      } as UpdateDiemKeHoach;
      diemKeHoachReq = this.diemKeHoachService.updateDiemKeHoach(diemKeHoach);
    }
    else {
      diemKeHoach = {
        diemKeHoach: this.formDataDiemKeHoach.get('diemKeHoach')?.value || 0,
        soBuoiTruc: this.formDataDiemKeHoach.get('soBuoiTruc')?.value || 0,
        soBenhNhan: this.formDataDiemKeHoach.get('soBenhNhan')?.value || 0,
        diemTruc: this.formDataDiemKeHoach.get('diemTruc')?.value || 0,
        diemTrucCC: this.formDataDiemKeHoach.get('diemTrucCC')?.value || 0,
        diemLayMau: this.formDataDiemKeHoach.get('diemLayMauXN')?.value || 0,
        bacSiId: parseInt(this.selectedDiemKeHoach?.bacSiId+ '') || 0,
        bacSi: this.selectedDiemKeHoach?.officerName  || '',
        thangNam: `${this.cur_thang}${this.cur_nam}`,
        khoaId: this.cur_khoa
      } as ThemDiemKeHoach;
      diemKeHoachReq = this.diemKeHoachService.themDiemKeHoach(diemKeHoach);
    }
    diemKeHoachReq.subscribe({
      next: (res) => {
        this.loading = false;
        var diemKeHoachId = this.selectedDiemKeHoach?.diemKeHoachId || res?.diemKeHoachId || 0;
        this.tangCuongService.batchInsertOrUpdateTangCuong(diemKeHoachId, this.dataTangCuong.map((tc) => ({
          ...tc,
          diemKeHoachId
        }))).subscribe({
          next: () => {
            this.addToast('Lưu điểm kế hoạch thành công', 'success');
            this.onCloseModal();
            this.loadDiemKeHoach();
          },
          error: (err) => {
            console.error(err);
            this.addToast('Đã xảy ra lỗi khi lưu điểm kế hoạch', 'danger');
          }
        });

      },
      error: (err) => {
        console.error(err);
        this.loading = false;
        this.addToast('Đã xảy ra lỗi khi lưu điểm kế hoạch', 'danger');
      }
    });

  }

  submitFormSearch(){
      this.currentPage = 1;
      this.loadDiemKeHoach();
  }
  loadDiemKeHoach() {
    if (!this.cur_thang || !this.cur_nam) {
      this.addToast('Vui lòng chọn tháng và năm', 'warning');
      return;
    }
    if (!this.cur_khoa) {
      this.addToast('Vui lòng chọn khoa', 'warning');
      return;
    }
    let thangNam = `${this.cur_thang}${this.cur_nam}`;
    this.loading = true;
    this.diemKeHoachService.getDsDiemKeHoach(this.currentPage, this.pageSize, thangNam, this.cur_khoa+'').subscribe({
      next: (res) => {
        this.loading = false;
        this.ds_diemkehoach = res?.dsDiemKeHoach || [];
        this.totalRecords = res?.totalRecords || 0;
      },
      error: (err) => {
        this.loading = false;
        console.error(err);
      }
    });

  }
  goToPage(page: number): void {
    this.currentPage = page;
    this.loadDiemKeHoach();
  }
  nextPage(): void {
    if (this.currentPage * this.pageSize < this.totalRecords) {
      this.currentPage++;
      this.loadDiemKeHoach();
    }
  }

  prePage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadDiemKeHoach();
    }
  }
   onPageSizeChange(newSize: number) {
    this.pageSize = Number(newSize);
    this.currentPage = 1;
    this.loadDiemKeHoach();
  }
  getLimitedPageNumbers(): number[] {
    const totalPages = Math.ceil(this.totalRecords / this.pageSize);
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
  onThemDiemKeHoachBtnClick(diemKeHoach: any) {
    this.selectedDiemKeHoach = diemKeHoach;
    this.isShowModal = true;
    this.getDsTangCuongByDiemKeHoachId();
    this.formDataDiemKeHoach =  new FormGroup({
      khoa: new FormControl(this.cur_khoa, [Validators.required]),
      bacSiId: new FormControl(this.selectedDiemKeHoach.bacSiId, [Validators.required]),
      diemKeHoach: new FormControl(this.selectedDiemKeHoach.diemKeHoach, [Validators.required]),
      soBuoiTruc: new FormControl(this.selectedDiemKeHoach.soBuoiTruc, [Validators.required]),
      diemTruc: new FormControl(this.selectedDiemKeHoach.diemTruc, [Validators.required]),
      diemLayMauXN: new FormControl(this.selectedDiemKeHoach.diemLayMau, [Validators.required]),
      thangNam: new FormControl(this.selectedDiemKeHoach.thangNam, [Validators.required]),
    });
  }
  onSoBuoiTrucChange($event: any) {
    const soBuoiTruc = $event.target.value || 0;
    const diemTruc = soBuoiTruc * this.hesoTruc[this.selectedDiemKeHoach.officerType] || 0;
    this.formDataDiemKeHoach.get('diemTruc')?.setValue(diemTruc);
    this.cd.detectChanges();
  }
  onDeleteTangCuong(index: number) {
    // Gọi API để xóa tăng cường
    // Sau khi xóa thành công, gọi lại getDsTangCuongByDiemKeHoachId() để cập nhật lại danh sách tăng cường
    var res = window.confirm('Bạn có chắc chắn muốn xóa tăng cường này?');
    if (res) {
      this.dataTangCuong.splice(index, 1);
    }
  }

  onThemTangCuong() {
    var tblTangCuong = document.getElementById('tbl-tangcuong') as HTMLDivElement;
    if (tblTangCuong) {
      const newTangCuong: TangCuong = {
        diemKeHoachId: this.selectedDiemKeHoach.diemKeHoachId,
        khoaId: this.selectedDiemKeHoach.khoaId,
        soNgay: 0,
        diem: 0
      };
      this.dataTangCuong.push(newTangCuong);
    }
  }
  onKhoaTangCuongChange(event: Select2UpdateEvent<any>, index: number) {
    this.dataTangCuong[index].khoaId = event.value;
  }


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
}
