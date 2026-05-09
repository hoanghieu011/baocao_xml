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
  soNgayTangCuong?: number;
  diemTruc: number;
  diemCongBANT: number;
  diemTHPTTTheoDD: number;
  diemBNNDCD: number;
  diemBNNDTH: number;
  diemBNNDCDNhapVien: number;
  tongCong: number;
  datCtkh: number;
  diemTHTheoBS?: number;
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
  officerTypeOptions = [
    { value: 'BAC_SI', label: 'Bác sĩ' },
    { value: 'DIEU_DUONG', label: 'Điều dưỡng' },
  ];
  loaiBaoCao: string = 'BAC_SI';
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
        this.dsDiemCtkh = [];
        this.loading = false;
        // this.addToast('Tải dữ liệu thành công', 'success');
        console.log(res);
        if(!res.data || res.data.length === 0) {
          this.addToast('Không có dữ liệu trong khoảng thời gian đã chọn', 'warning');
          return;
        }
        let curKhoa = '';
          let tongDiemKeHoach = 0, tongDiemCdKham = 0, tongDiemCdDieuTri = 0, tongDiemPTTCD = 0, tongDiemPTTTH = 0, tongDiemTangCuong = 0, tongSoNgayTangCuong = 0, tongDiemTruc = 0, tongDiemCongBANT = 0,tongDiemTHPTTTheoDD = 0,tongDiemBNNDCD = 0,tongDiemBNNDTH = 0,tongDiemBNNDCDNhapVien = 0;
          let diemKeHoachKhoa = 0, diemCdKhamKhoa = 0, diemCdDieuTriKhoa = 0, diemPTTCDKhoa = 0, diemPTTTHKhoa = 0, diemTangCuongKhoa = 0, soNgayTangCuongKhoa = 0, diemTrucKhoa = 0, diemCongBANTKhoa = 0,diemTHPTTTheoDDKhoa = 0,diemBNNDCDKhoa = 0,diemBNNDTHKhoa = 0,diemBNNDCDNhapVienKhoa = 0;
          let tongDiemTHBS = 0, tongDiemNhapVienBS = 0;
          let tongDiemTHBSTheoKhoa = 0, tongDiemNhapVienBSTheoKhoa = 0;
          let countGroup = 0;
          let countItem = 1;
          let currentGroupIndex = 0;
          for(let i = 0; i < res.data.length; i++) {
            const item = res.data[i];
            if(curKhoa != item.khoa) {
              countItem = 1;
              if(curKhoa != '') {
                let tongDiemTHKhoa = this.loaiBaoCao==='BAC_SI' ? diemCdKhamKhoa + diemCdDieuTriKhoa + diemPTTCDKhoa + diemPTTTHKhoa + diemTangCuongKhoa + diemTrucKhoa + diemCongBANTKhoa + diemTHPTTTheoDDKhoa + diemBNNDCDKhoa + diemBNNDTHKhoa + diemBNNDCDNhapVienKhoa : diemTrucKhoa + diemTangCuongKhoa;
                this.dsDiemCtkh[currentGroupIndex] = {
                  ...this.dsDiemCtkh[currentGroupIndex],
                  diemKeHoach: diemKeHoachKhoa,
                  diemCdKham: diemCdKhamKhoa,
                  diemCDDieuTri: diemCdDieuTriKhoa,
                  diemPTTCD: diemPTTCDKhoa,
                  diemPTTTH: diemPTTTHKhoa,
                  diemTangCuong: diemTangCuongKhoa,
                  soNgayTangCuong: soNgayTangCuongKhoa,
                  diemTruc: diemTrucKhoa,
                  diemCongBANT: diemCongBANTKhoa,
                  diemTHPTTTheoDD: diemTHPTTTheoDDKhoa,
                  diemBNNDCD: diemBNNDCDKhoa,
                  diemBNNDTH: diemBNNDTHKhoa,
                  diemTHTheoBS: tongDiemTHBSTheoKhoa,
                  diemBNNDCDNhapVien: diemBNNDCDNhapVienKhoa,
                  tongCong: tongDiemTHKhoa + (this.loaiBaoCao==='DIEU_DUONG' ? tongDiemTHBSTheoKhoa : 0) ,
                  datCtkh: diemKeHoachKhoa !== 0 ? (((tongDiemTHKhoa + (this.loaiBaoCao==='DIEU_DUONG' ? tongDiemTHBSTheoKhoa : 0)) / (diemKeHoachKhoa )) * 100) : 0
                };

                if(this.loaiBaoCao==='DIEU_DUONG') {
                  let countDD = this.dsDiemCtkh.length - currentGroupIndex - 1;
                  console.log(countDD);
                  for(let j = currentGroupIndex + 1; j < this.dsDiemCtkh.length; j++) {
                    this.dsDiemCtkh[j].diemTHTheoBS = countDD > 0 ? (tongDiemTHBSTheoKhoa / countDD) : 0;
                    this.dsDiemCtkh[j].tongCong = this.dsDiemCtkh[j].tongCong + (countDD > 0 ? (tongDiemTHBSTheoKhoa / countDD) : 0);
                    this.dsDiemCtkh[j].datCtkh = this.dsDiemCtkh[j].diemKeHoach !== 0 ? (this.dsDiemCtkh[j].tongCong*100 /this.dsDiemCtkh[j].diemKeHoach) : 0
                  }
                }
                let countItemKhoa = this.dsDiemCtkh.length - currentGroupIndex - 1;
                if(countItemKhoa==0) this.dsDiemCtkh.pop();
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
              soNgayTangCuongKhoa = 0;
              diemTrucKhoa = 0;
              diemCongBANTKhoa = 0;
              diemTHPTTTheoDDKhoa = 0;
              diemBNNDCDKhoa = 0;
              diemBNNDTHKhoa = 0;
              diemBNNDCDNhapVienKhoa = 0;
              tongDiemTHBSTheoKhoa = 0;
              tongDiemNhapVienBSTheoKhoa = 0;
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
                soNgayTangCuong: 0,
                diemTruc: 0,
                diemCongBANT: 0,
                diemTHPTTTheoDD: 0,
                diemBNNDCD: 0,
                diemBNNDTH: 0,
                diemBNNDCDNhapVien: 0,
                tongCong: 0,
                diemTHTheoBS: 0,
                datCtkh: 0
              });
            }
          
            if(item.officerType == 4) { // bác sĩ
              tongDiemTHBS += ((item.diemCdKham || 0) + (item.diemCDDieuTri || 0) + (item.diemPTTCD || 0) + (item.diemPTTTH || 0) + (item.diemTangCuong || 0) + (item.diemTruc || 0) + (item.diemCongBANT || 0) + (item.diemTHPTTTheoDD || 0) + (item.diemBNNDCD || 0) + (item.diemBNNDTH || 0) + (item.diemBNNDCDNhapVien || 0));
              tongDiemNhapVienBS += (item.diemCDDieuTri || 0);
              tongDiemTHBSTheoKhoa += ((item.diemCdKham || 0) + (item.diemCDDieuTri || 0) + (item.diemPTTCD || 0) + (item.diemPTTTH || 0) + (item.diemTangCuong || 0) + (item.diemTruc || 0) + (item.diemCongBANT || 0) + (item.diemTHPTTTheoDD || 0) + (item.diemBNNDCD || 0) + (item.diemBNNDTH || 0) + (item.diemBNNDCDNhapVien || 0));
              tongDiemNhapVienBSTheoKhoa += (item.diemCDDieuTri || 0);
            }
            let pushedItem = ((this.loaiBaoCao==='BAC_SI' && item.officerType == 4) || (this.loaiBaoCao==='DIEU_DUONG' && item.officerType != 4)) ? item : null;
            // các đầu điểm cộng dồn theo khoa của bác sĩ / điều dưỡng
            diemKeHoachKhoa += pushedItem ? pushedItem.diemKeHoach || 0 : 0;
            diemCdKhamKhoa += pushedItem ? pushedItem.diemCdKham || 0 : 0;
            diemCdDieuTriKhoa += pushedItem ? pushedItem.diemCDDieuTri || 0 : 0;
            diemPTTCDKhoa += pushedItem ? pushedItem.diemPTTCD || 0 : 0;
            diemPTTTHKhoa += pushedItem ? pushedItem.diemPTTTH || 0 : 0;
            diemTangCuongKhoa += pushedItem ? pushedItem.diemTangCuong || 0 : 0;
            diemTrucKhoa += pushedItem ? pushedItem.diemTruc || 0 : 0;
            diemCongBANTKhoa += pushedItem ? pushedItem.diemCongBANT || 0 : 0;
            diemTHPTTTheoDDKhoa += pushedItem ? pushedItem.diemTHPTTTheoDD || 0 : 0;
            diemBNNDCDKhoa += pushedItem ? pushedItem.diemBNNDCD || 0 : 0;
            diemBNNDTHKhoa += pushedItem ? pushedItem.diemBNNDTH || 0 : 0;
            diemBNNDCDNhapVienKhoa += (pushedItem ? pushedItem.diemBNNDCDNhapVien || 0 : 0);
            soNgayTangCuongKhoa += pushedItem ? pushedItem.soNgayTangCuong || 0 : 0;

            tongDiemKeHoach += pushedItem ? pushedItem.diemKeHoach || 0 : 0;
            tongDiemCdKham += pushedItem ? pushedItem.diemCdKham || 0 : 0;
            tongDiemCdDieuTri += pushedItem ? pushedItem.diemCDDieuTri || 0 : 0;
            tongDiemPTTCD += pushedItem ? pushedItem.diemPTTCD || 0 : 0;
            tongDiemPTTTH += pushedItem ? pushedItem.diemPTTTH || 0 : 0;
            tongDiemTangCuong += pushedItem ? pushedItem.diemTangCuong || 0 : 0;
            tongSoNgayTangCuong += pushedItem ? pushedItem.soNgayTangCuong || 0 : 0;
            tongDiemTruc += pushedItem ? pushedItem.diemTruc || 0 : 0;
            tongDiemCongBANT += pushedItem ? pushedItem.diemCongBANT || 0 : 0;
            tongDiemTHPTTTheoDD += pushedItem ? pushedItem.diemTHPTTTheoDD || 0 : 0;
            tongDiemBNNDCD += pushedItem ? pushedItem.diemBNNDCD || 0 : 0;
            tongDiemBNNDTH += pushedItem ? pushedItem.diemBNNDTH || 0 : 0;
            tongDiemBNNDCDNhapVien += (pushedItem ? pushedItem.diemBNNDCDNhapVien || 0 : 0);
            if(pushedItem) {
              let tongDiemTHItem = this.loaiBaoCao ==='BAC_SI' ? (pushedItem.diemCdKham || 0) + (pushedItem.diemCDDieuTri || 0) + (pushedItem.diemPTTCD || 0) + (pushedItem.diemPTTTH || 0) + (pushedItem.diemTangCuong || 0) + (pushedItem.diemTruc || 0) + (pushedItem.diemCongBANT || 0) + (pushedItem.diemTHPTTTheoDD || 0) + (pushedItem.diemBNNDCD || 0) + (pushedItem.diemBNNDTH || 0) + (pushedItem.diemBNNDCDNhapVien || 0) :
               (pushedItem.diemTruc || 0) + (pushedItem.diemTangCuong || 0);
              let tempItem: ReportRow = {
                type: 'item',
                stt: String(countItem),
                officerName: pushedItem.officerName,
                officerType: pushedItem.officerType,
                diemKeHoach: (pushedItem.diemKeHoach || 0),
                diemCdKham: pushedItem.diemCdKham || 0,
                diemCDDieuTri: pushedItem.diemCDDieuTri || 0,
                diemPTTCD: (pushedItem.diemPTTCD || 0),
                diemPTTTH: (pushedItem.diemPTTTH || 0),
                diemTangCuong: pushedItem.diemTangCuong || 0,
                soNgayTangCuong: pushedItem.soNgayTangCuong || 0,
                diemTruc: pushedItem.diemTruc || 0,
                diemCongBANT: pushedItem.diemCongBANT || 0,
                diemTHPTTTheoDD: pushedItem.diemTHPTTTheoDD || 0,
                diemBNNDCD: pushedItem.diemBNNDCD || 0,
                diemBNNDTH: pushedItem.diemBNNDTH || 0,
                diemBNNDCDNhapVien: pushedItem.diemBNNDCDNhapVien || 0,
                tongCong: tongDiemTHItem ,
                diemTHTheoBS: 0,
                datCtkh: 0
              };
              tempItem.datCtkh = tempItem.diemKeHoach !== 0 ? ((tongDiemTHItem / (tempItem.diemKeHoach )) * 100) : 0;
              this.dsDiemCtkh.push(tempItem);
              countItem++;
            }
          }
        
          if(curKhoa != '') {
            let tongDiemTHKhoa = this.loaiBaoCao==='BAC_SI' ? diemCdKhamKhoa + diemCdDieuTriKhoa + diemPTTCDKhoa + diemPTTTHKhoa + diemTangCuongKhoa + diemTrucKhoa + diemCongBANTKhoa + diemTHPTTTheoDDKhoa + diemBNNDCDKhoa + diemBNNDTHKhoa + diemBNNDCDNhapVienKhoa :
            diemTrucKhoa+diemTangCuongKhoa;
            this.dsDiemCtkh[currentGroupIndex] = {
              ...this.dsDiemCtkh[currentGroupIndex],
              diemKeHoach: diemKeHoachKhoa,
              diemCdKham: diemCdKhamKhoa,
              diemCDDieuTri: diemCdDieuTriKhoa,
              diemPTTCD: diemPTTCDKhoa,
              diemPTTTH: diemPTTTHKhoa,
              diemTangCuong: diemTangCuongKhoa,
              soNgayTangCuong: soNgayTangCuongKhoa,
              diemTruc: diemTrucKhoa,
              diemTHTheoBS: tongDiemTHBSTheoKhoa,
              diemCongBANT: diemCongBANTKhoa,
              diemTHPTTTheoDD: diemTHPTTTheoDDKhoa,
              diemBNNDCD: diemBNNDCDKhoa,
              diemBNNDTH: diemBNNDTHKhoa,
              diemBNNDCDNhapVien: diemBNNDCDNhapVienKhoa,
              tongCong: tongDiemTHKhoa + (this.loaiBaoCao==='DIEU_DUONG' ? tongDiemTHBSTheoKhoa : 0) ,
              datCtkh:  diemKeHoachKhoa !== 0 ? (((tongDiemTHKhoa + (this.loaiBaoCao==='DIEU_DUONG' ? tongDiemTHBSTheoKhoa : 0)) / (diemKeHoachKhoa )) * 100) : 0
            };
            let countItemKhoa = this.dsDiemCtkh.length - currentGroupIndex - 1;
            if(countItemKhoa==0) this.dsDiemCtkh.pop();
            let countDD = this.dsDiemCtkh.length - currentGroupIndex - 1;
            for(let j = currentGroupIndex + 1; j < this.dsDiemCtkh.length; j++) {
              this.dsDiemCtkh[j].diemTHTheoBS = countDD > 0 ? (tongDiemTHBSTheoKhoa / countDD) : 0;
              this.dsDiemCtkh[j].tongCong = this.dsDiemCtkh[j].tongCong + (this.loaiBaoCao === 'DIEU_DUONG' ? (countDD > 0 ? (tongDiemTHBSTheoKhoa / countDD) : 0) : 0);
              this.dsDiemCtkh[j].datCtkh = this.dsDiemCtkh[j].diemKeHoach !== 0 ? (this.dsDiemCtkh[j].tongCong*100 /this.dsDiemCtkh[j].diemKeHoach) : 0
            }
          }
        
          let tongDiemTH = this.loaiBaoCao==='BAC_SI' ? tongDiemCdKham + tongDiemCdDieuTri + tongDiemPTTCD + tongDiemPTTTH + tongDiemTangCuong + tongDiemTruc + tongDiemCongBANT + tongDiemTHPTTTheoDD + tongDiemBNNDCD + tongDiemBNNDTH + tongDiemBNNDCDNhapVien : tongDiemTruc + tongDiemTangCuong;
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
            soNgayTangCuong: tongSoNgayTangCuong,
            diemTHTheoBS: tongDiemTHBS,
            diemTruc: tongDiemTruc,
            diemCongBANT: tongDiemCongBANT,
            diemTHPTTTheoDD: tongDiemTHPTTTheoDD,
            diemBNNDCD: tongDiemBNNDCD,
            diemBNNDTH: tongDiemBNNDTH,
            diemBNNDCDNhapVien: tongDiemBNNDCDNhapVien,
            tongCong: tongDiemTH + (this.loaiBaoCao==='DIEU_DUONG' ? tongDiemTHBS : 0) ,
            datCtkh: tongDiemKeHoach !== 0 ? (((tongDiemTH + (this.loaiBaoCao==='DIEU_DUONG' ? tongDiemTHBS : 0)) / (tongDiemKeHoach )) * 100)  : 0   
          });
          
          console.log(this.dsDiemCtkh);
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
  onLoaiBaoCaoChange(event: Select2UpdateEvent<any>) {
    this.loaiBaoCao = event.value;
    this.loadData();
  }
  exportExcel(){
      this.loadingExcel = true;
      this.baoCaoDiemCtkhService.exportExcel(this.tuThang, this.tuNam, this.denThang, this.denNam, this.loaiBaoCao === 'BAC_SI' ? 0 : 1).subscribe({
        next: (res) => {
          this.loadingExcel = false;
          const blob = new Blob([res], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `BaoCaoDiemCtkh_${this.tuNam}${String(this.tuThang).padStart(2, '0')}_${this.denNam}${String(this.denThang).padStart(2, '0')}.xlsx`;
          a.click();
          window.URL.revokeObjectURL(url);
          this.addToast('Xuất Excel thành công', 'success');
        }
        ,error: (err) => {
          this.loadingExcel = false;
          this.addToast('Xuất Excel thất bại');
          console.error(err);
        }
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
