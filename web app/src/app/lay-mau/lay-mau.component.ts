import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ToastModule } from '@coreui/angular';
import { Select2Module, Select2UpdateEvent } from 'ng-select2-component';

@Component({
  selector: 'app-lay-mau',
  standalone: true,
  imports: [FormsModule, Select2Module, CommonModule, ToastModule, ReactiveFormsModule],
  templateUrl: './lay-mau.component.html',
  styleUrl: './lay-mau.component.css'
})
export class LayMauComponent  {
  ds_khoa: any[] = [
    {MA_KHOA: 'K01', label:'Khoa Khám bệnh', value: 43360},
    {MA_KHOA: 'K0203', label:'Khoa HSCC - Nội', value: 43540},
    {MA_KHOA: 'K1631', label:'Khoa YHCT và PHCN', value: 43560},
    {MA_KHOA: 'K1319282930', label:'Khoa Ngoại - Chuyên khoa', value: 43580},
  ];
  cur_khoa: number = 0;
  ds_bacsi_all: any[] = [
    {OFFICIER_ID: 58, OFFICIER_NAME:'Nguyễn Đức Thái', OFFICIER_TYPE: 4, KHOAID: 43360},
    {OFFICIER_ID: 54, OFFICIER_NAME:'Bùi Mạnh Hà', OFFICIER_TYPE: 4, KHOAID: 43360},
    {OFFICIER_ID: 45, OFFICIER_NAME:'Nguyễn Văn Thự', OFFICIER_TYPE: 4, KHOAID: 43540},
    {OFFICIER_ID: 65, OFFICIER_NAME:'Vũ Công Đức', OFFICIER_TYPE: 4, KHOAID: 43540},
    {OFFICIER_ID: 47, OFFICIER_NAME:'Nguyễn Ngọc Tri', OFFICIER_TYPE: 4, KHOAID: 43580},
    {OFFICIER_ID: 13, OFFICIER_NAME:'Nguyễn Thị Thuỷ', OFFICIER_TYPE: 4, KHOAID: 43560},
  ];
  ds_bacsi: any[] = [];
  ds_dieuduong_all: any[] = [
    {OFFICIER_ID: 15, OFFICIER_NAME:'Phùng Thị Oanh', OFFICIER_TYPE: 6, KHOAID: 43360},
    {OFFICIER_ID: 17, OFFICIER_NAME:'Nguyễn Thị Ngọc Bích', OFFICIER_TYPE: 6, KHOAID: 43360},
    {OFFICIER_ID: 77, OFFICIER_NAME:'Đinh Thị Nguyệt', OFFICIER_TYPE: 6, KHOAID: 43360},
    {OFFICIER_ID: 6, OFFICIER_NAME:'Lê Thị Minh', OFFICIER_TYPE: 6, KHOAID: 43540},
    {OFFICIER_ID: 31, OFFICIER_NAME:'Nguyễn Thị Thanh Tâm', OFFICIER_TYPE: 6, KHOAID: 43540},
    {OFFICIER_ID: 66, OFFICIER_NAME:'Đào Thị Tuyết Mai', OFFICIER_TYPE: 6, KHOAID: 43540},
    {OFFICIER_ID: 7, OFFICIER_NAME:'Lê Đức Duy', OFFICIER_TYPE: 6, KHOAID: 43580},
    {OFFICIER_ID: 23, OFFICIER_NAME:'Nguyễn Thanh Nga', OFFICIER_TYPE: 6, KHOAID: 43580},
    {OFFICIER_ID: 20, OFFICIER_NAME:'Trần Thị Hương', OFFICIER_TYPE: 6, KHOAID: 43560},
    {OFFICIER_ID: 26, OFFICIER_NAME:'Vũ Thị Hà', OFFICIER_TYPE: 6, KHOAID: 43560},

  ]
  ds_dieuduong: any[] = [];

  toasts: any[] = [];

  isShowModal = false;
  loading = false;
  formData =  new FormGroup({
     khoa: new FormControl(0, [Validators.required]),
    // ds_bacsi_chon: [],
    // ds_dieuduong_chon: [],
    maumau_soluot: new FormControl(0, [Validators.required]),
    maumau_sodiem: new FormControl(0, [Validators.required]),
    maunuoctieu_soluot: new FormControl(0, [Validators.required]),
    maunuoctieu_sodiem: new FormControl(0, [Validators.required]),
    tongdiem: new FormControl(0, [Validators.required]),
    ghichu: new FormControl(''),
    thang: new FormControl(new Date().getMonth() + 1),
    nam: new FormControl(new Date().getFullYear()),
  });
  ngOnInit(): void {
  }

  onKhoaChange(event: Select2UpdateEvent<any>) {
    this.cur_khoa = event.value;
    this.loadDsBacSi();
    this.loadDsDieuDuong();
    // this.addToast('Đã chọn khoa: ' + this.ds_khoa.find((k) => k.ORG_ID === this.cur_khoa)?.ORG_NAME, 'success');
  }

  onCloseModal() {
    this.isShowModal = false;
    this.formData.reset();
  }

  onSaveLayMau() {
    if (this.formData.invalid) {
      this.addToast('Vui lòng điền đầy đủ thông tin', 'warning');
      return;
    }
    console.log('Form Data:', this.formData.value);
    this.loading = true;
    setTimeout(() => {
      this.loading = false;
      this.addToast('Lấy mẫu thành công', 'success');
      // this.onCloseModal();
    }, 2000);
  }

  loadDsBacSi() {
    this.ds_bacsi = this.ds_bacsi_all.filter((item: any) => item.KHOAID === this.cur_khoa);
  }

  loadDsDieuDuong() {
    this.ds_dieuduong = this.ds_dieuduong_all.filter((item: any) => item.KHOAID === this.cur_khoa);
  }

  onThemLayMauBtnClick(){
    this.isShowModal = true;
    this.formData =  new FormGroup({
     khoa: new FormControl(0, [Validators.required]),
    // ds_bacsi_chon: [],
    // ds_dieuduong_chon: [],
    maumau_soluot: new FormControl(0, [Validators.required]),
    maumau_sodiem: new FormControl(0, [Validators.required]),
    maunuoctieu_soluot: new FormControl(0, [Validators.required]),
    maunuoctieu_sodiem: new FormControl(0, [Validators.required]),
    tongdiem: new FormControl(0, [Validators.required]),
    ghichu: new FormControl(''),
    thang: new FormControl(new Date().getMonth() + 1),
    nam: new FormControl(new Date().getFullYear()),
  });
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
