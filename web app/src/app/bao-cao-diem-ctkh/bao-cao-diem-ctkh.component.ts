import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component } from '@angular/core';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ToastModule } from '@coreui/angular';
import { Select2Module, Select2UpdateEvent } from 'ng-select2-component';
import { TranslateModule } from '@ngx-translate/core';
import { count, forkJoin, of } from 'rxjs';
import { BaoCaoDiemCtkhService } from '../services/bc-diem-ctkh.service';

type TangCuong = {
  diemKeHoachId: number;
  khoaId: number;
  soNgay: number;
  diem: number;
  id?: number; 
};

type ReportType = 'group' | 'item' | 'grandTotal';
type ReportRow = {
  type: ReportType;
  stt: string;
  officerName?: string;
  khoa?: string;
  khoaId?: number;
  officerType: number;
  diemKeHoach: number;
  diemCdKham: number;
  diemCDDieuTri: number;
  diemPTTCD: number;
  diemPTTTH: number;
  diemTangCuong: number;
  diemTruc: number;
  diemCongBANT: number;
  diemTHPTTTheoDD: number;
  diemBNNDCD: number;
  diemBNNDTH: number;
  diemBNNDCDNhapVien: number;
  tongCong: number;
  datCtkh: number;
}

@Component({
  selector: 'app-bao-cao-diem-ctkh',
  standalone: true,
  imports: [FormsModule, Select2Module, CommonModule, ToastModule, ReactiveFormsModule, TranslateModule],
  templateUrl: './bao-cao-diem-ctkh.component.html',
  styleUrl: './bao-cao-diem-ctkh.component.css'
})

export class BCDiemCtkhComponent  {
  Math = Math;
  tenBenhVien = localStorage.getItem('TenBenhVien') || 'Tên_CSYT';
  tieuDeMoRong = '';
  toasts: any[] = [];
  dsDiemCtkh: ReportRow[] = [];
  loading = false;
  loadingExcel = false;
  dsThang = Array.from({ length: 12 }, (_, i) => ({ value: i + 1, label: `Tháng ${i + 1}` }));
  dsNam: any[] = []
  tuThang: number = new Date().getMonth() + 1;
  denThang: number = new Date().getMonth() + 1;
  tuNam: number = new Date().getFullYear();
  denNam: number = new Date().getFullYear();
  constructor(private baoCaoDiemCtkhService: BaoCaoDiemCtkhService, private cd: ChangeDetectorRef) { }
  ngOnInit(): void {
    let namHienTai = new Date().getFullYear();
    for (let year = namHienTai - 2; year <= namHienTai + 2; year++) {
      this.dsNam.push({ value: year, label: `Năm ${year}` });
    }
    this.generateTieuDeMoRong();
  }
  loadData() {
    this.loading = true;
    this.baoCaoDiemCtkhService.getDsDiemCtkh(this.tuThang, this.tuNam, this.denThang, this.denNam).subscribe({
      next: (res) => {
        this.loading = false;
        this.addToast('Tải dữ liệu thành công', 'success');
        console.log(res);
        if(res.data && res.data.length > 0) {
          let curKhoa = '';
          let tongDiemKeHoach = 0, tongDiemCdKham = 0, tongDiemCdDieuTri = 0, tongDiemPTTCD = 0, tongDiemPTTTH = 0, tongDiemTangCuong = 0, tongDiemTruc = 0, tongDiemCongBANT = 0,tongDiemTHPTTTheoDD = 0,tongDiemBNNDCD = 0,tongDiemBNNDTH = 0,tongDiemBNNDCDNhapVien = 0;
          let diemKeHoachKhoa = 0, diemCdKhamKhoa = 0, diemCdDieuTriKhoa = 0, diemPTTCDKhoa = 0, diemPTTTHKhoa = 0, diemTangCuongKhoa = 0, diemTrucKhoa = 0, diemCongBANTKhoa = 0,diemTHPTTTheoDDKhoa = 0,diemBNNDCDKhoa = 0,diemBNNDTHKhoa = 0,diemBNNDCDNhapVienKhoa = 0;
          let countGroup = 0;
          let countItem = 1;
          let currentGroupIndex = 0;
          for(let i = 0; i < res.data.length; i++) {
            const item = res.data[i];
            if(curKhoa != item.khoa) {
              countItem = 1;
              if(curKhoa != '') {
                this.dsDiemCtkh[currentGroupIndex] = {
                  ...this.dsDiemCtkh[currentGroupIndex],
                  diemKeHoach: diemKeHoachKhoa,
                  diemCdKham: diemCdKhamKhoa,
                  diemCDDieuTri: diemCdDieuTriKhoa,
                  diemPTTCD: diemPTTCDKhoa,
                  diemPTTTH: diemPTTTHKhoa,
                  diemTangCuong: diemTangCuongKhoa,
                  diemTruc: diemTrucKhoa,
                  diemCongBANT: diemCongBANTKhoa,
                  diemTHPTTTheoDD: diemTHPTTTheoDDKhoa,
                  diemBNNDCD: diemBNNDCDKhoa,
                  diemBNNDTH: diemBNNDTHKhoa,
                  diemBNNDCDNhapVien: diemBNNDCDNhapVienKhoa,
                  tongCong: (diemCdKhamKhoa + diemCdDieuTriKhoa + diemPTTCDKhoa + diemPTTTHKhoa + diemTangCuongKhoa + diemTrucKhoa + diemCongBANTKhoa + diemTHPTTTheoDDKhoa + diemBNNDCDKhoa + diemBNNDTHKhoa + diemBNNDCDNhapVienKhoa) ,
                  datCtkh: diemKeHoachKhoa !== 0 ? (((diemCdKhamKhoa + diemCdDieuTriKhoa + diemPTTCDKhoa + diemPTTTHKhoa + diemTangCuongKhoa + diemTrucKhoa + diemCongBANTKhoa + diemTHPTTTheoDDKhoa + diemBNNDCDKhoa + diemBNNDTHKhoa + diemBNNDCDNhapVienKhoa) / (diemKeHoachKhoa )) * 100) : 0
                };
              }
              curKhoa = item.khoa;
              countGroup++;
              currentGroupIndex = this.dsDiemCtkh.length;
              diemKeHoachKhoa = 0;
              diemCdKhamKhoa = 0;
              diemCdDieuTriKhoa = 0;
              diemPTTCDKhoa = 0;
              diemPTTTHKhoa = 0;
              diemTangCuongKhoa = 0;
              diemTrucKhoa = 0;
              diemCongBANTKhoa = 0;
              diemTHPTTTheoDDKhoa = 0;
              diemBNNDCDKhoa = 0;
              diemBNNDTHKhoa = 0;
              diemBNNDCDNhapVienKhoa = 0;
              this.dsDiemCtkh.push({
                type: 'group',
                stt: this.convertToRoman(countGroup),
                khoa: item.khoa,
                khoaId: item.khoa_id,
                officerType: item.officerType,
                diemKeHoach: 0,
                diemCdKham: 0,
                diemCDDieuTri: 0,
                diemPTTCD: 0,
                diemPTTTH: 0,
                diemTangCuong: 0,
                diemTruc: 0,
                diemCongBANT: 0,
                diemTHPTTTheoDD: 0,
                diemBNNDCD: 0,
                diemBNNDTH: 0,
                diemBNNDCDNhapVien: 0,
                tongCong: 0,
                datCtkh: 0
              });
            }
            diemKeHoachKhoa += item.diemKeHoach || 0;
            diemCdKhamKhoa += item.diemCdKham || 0;
            diemCdDieuTriKhoa += item.diemCDDieuTri || 0;
            diemPTTCDKhoa += item.diemPTTCD || 0;
            diemPTTTHKhoa += item.diemPTTTH || 0;
            diemTangCuongKhoa += item.diemTangCuong || 0;
            diemTrucKhoa += item.diemTruc || 0;
            diemCongBANTKhoa += item.diemCongBANT || 0;
            diemTHPTTTheoDDKhoa += item.diemTHPTTTheoDD || 0;
            diemBNNDCDKhoa += item.diemBNNDCD || 0;
            diemBNNDTHKhoa += item.diemBNNDTH || 0;
            diemBNNDCDNhapVienKhoa += (item.diemBNNDCDNhapVien || 0);

            tongDiemKeHoach += item.diemKeHoach || 0;
            tongDiemCdKham += item.diemCdKham || 0;
            tongDiemCdDieuTri += item.diemCDDieuTri || 0;
            tongDiemPTTCD += item.diemPTTCD || 0;
            tongDiemPTTTH += item.diemPTTTH || 0;
            tongDiemTangCuong += item.diemTangCuong || 0;
            tongDiemTruc += item.diemTruc || 0;
            tongDiemCongBANT += item.diemCongBANT || 0;
            tongDiemTHPTTTheoDD += item.diemTHPTTTheoDD || 0;
            tongDiemBNNDCD += item.diemBNNDCD || 0;
            tongDiemBNNDTH += item.diemBNNDTH || 0;
            tongDiemBNNDCDNhapVien += (item.diemBNNDCDNhapVien || 0);
            let tempItem: ReportRow = {
              type: 'item',
              stt: String(countItem),
              officerName: item.officerName,
              officerType: item.officerType,
              diemKeHoach: (item.diemKeHoach || 0),
              diemCdKham: item.diemCdKham || 0,
              diemCDDieuTri: item.diemCDDieuTri || 0,
              diemPTTCD: (item.diemPTTCD || 0),
              diemPTTTH: (item.diemPTTTH || 0),
              diemTangCuong: item.diemTangCuong || 0,
              diemTruc: item.diemTruc || 0,
              diemCongBANT: item.diemCongBANT || 0,
              diemTHPTTTheoDD: item.diemTHPTTTheoDD || 0,
              diemBNNDCD: item.diemBNNDCD || 0,
              diemBNNDTH: item.diemBNNDTH || 0,
              diemBNNDCDNhapVien: item.diemBNNDCDNhapVien || 0,
              tongCong: ((item.diemCdKham || 0) + (item.diemCDDieuTri || 0) + (item.diemPTTCD || 0) + (item.diemPTTTH || 0) + (item.diemTangCuong || 0) + (item.diemTruc || 0) + (item.diemCongBANT || 0) + (item.diemTHPTTTheoDD || 0) + (item.diemBNNDCD || 0) + (item.diemBNNDTH || 0) + (item.diemBNNDCDNhapVien || 0)) ,
              datCtkh: 0
            };
            tempItem.datCtkh = tempItem.diemKeHoach !== 0 ? (((tempItem.diemCdKham + tempItem.diemCDDieuTri + tempItem.diemPTTCD + tempItem.diemPTTTH + tempItem.diemTangCuong + tempItem.diemTruc + tempItem.diemCongBANT + tempItem.diemTHPTTTheoDD + tempItem.diemBNNDCD + tempItem.diemBNNDTH + tempItem.diemBNNDCDNhapVien) / (tempItem.diemKeHoach )) * 100) : 0;
            this.dsDiemCtkh.push(tempItem);
            countItem++;
          }
          if(curKhoa != '') {
            this.dsDiemCtkh[currentGroupIndex] = {
              ...this.dsDiemCtkh[currentGroupIndex],
              diemKeHoach: diemKeHoachKhoa,
              diemCdKham: diemCdKhamKhoa,
              diemCDDieuTri: diemCdDieuTriKhoa,
              diemPTTCD: diemPTTCDKhoa,
              diemPTTTH: diemPTTTHKhoa,
              diemTangCuong: diemTangCuongKhoa,
              diemTruc: diemTrucKhoa,
              diemCongBANT: diemCongBANTKhoa,
              diemTHPTTTheoDD: diemTHPTTTheoDDKhoa,
              diemBNNDCD: diemBNNDCDKhoa,
              diemBNNDTH: diemBNNDTHKhoa,
              diemBNNDCDNhapVien: diemBNNDCDNhapVienKhoa,
              tongCong: (diemCdKhamKhoa + diemCdDieuTriKhoa + diemPTTCDKhoa + diemPTTTHKhoa + diemTangCuongKhoa + diemTrucKhoa + diemCongBANTKhoa + diemTHPTTTheoDDKhoa + diemBNNDCDKhoa + diemBNNDTHKhoa + diemBNNDCDNhapVienKhoa) ,
              datCtkh:  diemKeHoachKhoa !== 0 ? (((diemCdKhamKhoa + diemCdDieuTriKhoa + diemPTTCDKhoa + diemPTTTHKhoa + diemTangCuongKhoa + diemTrucKhoa + diemCongBANTKhoa + diemTHPTTTheoDDKhoa + diemBNNDCDKhoa + diemBNNDTHKhoa + diemBNNDCDNhapVienKhoa) / (diemKeHoachKhoa )) * 100) : 0
            };
          }
          this.dsDiemCtkh.push({
            type: 'grandTotal',
            stt: '',
            khoa: 'Tổng cộng',
            officerType: 0,
            diemKeHoach: tongDiemKeHoach,
            diemCdKham: tongDiemCdKham,
            diemCDDieuTri: tongDiemCdDieuTri,
            diemPTTCD: tongDiemPTTCD,
            diemPTTTH: tongDiemPTTTH,
            diemTangCuong: tongDiemTangCuong,
            diemTruc: tongDiemTruc,
            diemCongBANT: tongDiemCongBANT,
            diemTHPTTTheoDD: tongDiemTHPTTTheoDD,
            diemBNNDCD: tongDiemBNNDCD,
            diemBNNDTH: tongDiemBNNDTH,
            diemBNNDCDNhapVien: tongDiemBNNDCDNhapVien,
            tongCong: (tongDiemCdKham + tongDiemCdDieuTri + tongDiemPTTCD + tongDiemPTTTH + tongDiemTangCuong + tongDiemTruc + tongDiemCongBANT + tongDiemTHPTTTheoDD + tongDiemBNNDCD + tongDiemBNNDTH + tongDiemBNNDCDNhapVien) ,
            datCtkh: tongDiemKeHoach !== 0 ? (((tongDiemCdKham + tongDiemCdDieuTri + tongDiemPTTCD + tongDiemPTTTH + tongDiemTangCuong + tongDiemTruc + tongDiemCongBANT + tongDiemTHPTTTheoDD + tongDiemBNNDCD + tongDiemBNNDTH + tongDiemBNNDCDNhapVien) / (tongDiemKeHoach )) * 100)  : 0   
          });
        }
        this.cd.markForCheck();
      },
      error: (err) => {
        this.loading = false;
        this.addToast('Tải dữ liệu thất bại');
        console.error(err);
        this.cd.markForCheck();
      }
    });
  }
  generateTieuDeMoRong(){
    if(this.denNam == this.tuNam && this.denThang - this.tuThang <= 5) {
      let temp1 = '';
      for(let i = this.tuThang; i <= this.denThang; i++) {
        temp1 = temp1 + ` ${i} ` + '+';
      }
      temp1 = temp1.slice(0,-1);
      this.tieuDeMoRong = ` THÁNG ${temp1} NĂM ${this.tuNam}`
    }
    else {
      this.tieuDeMoRong = ` TỪ THÁNG ${this.tuThang} NĂM ${this.tuNam} ĐẾN THÁNG ${this.denThang} NĂM ${this.denNam}`;
    }
  }
  onTuThangChange(event: Select2UpdateEvent<any>) {
    this.tuThang = event.value;
    this.generateTieuDeMoRong();
  }
  onTuNamChange(event: Select2UpdateEvent<any>) {
    this.tuNam = event.value
    this.generateTieuDeMoRong();
  }
  onDenThangChange(event: Select2UpdateEvent<any>) {
    this.denThang = event.value;
    this.generateTieuDeMoRong();
  }
  onDenNamChange(event: Select2UpdateEvent<any>) {
    this.denNam = event.value;
    this.generateTieuDeMoRong();
  }
  exportExcel(){

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
  convertToRoman(num: number) {
    const lookup = {
      M: 1000, CM: 900, D: 500, CD: 400,
      C: 100, XC: 90, L: 50, XL: 40,
      X: 10, IX: 9, V: 5, IV: 4, I: 1
    };
    let roman = '';
    for (const [key, value] of Object.entries(lookup)) {
      while (num >= value) {
        roman += key;
        num -= value;
      }
    }
    return roman;
  }      
}
