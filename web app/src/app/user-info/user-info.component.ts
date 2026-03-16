import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NghiPhepService } from '../services/nghi-phep.service';
import { AuthService } from '../services/auth.service';
import { TranslateService, TranslateModule } from '@ngx-translate/core';
import {PhepTonService} from '../services/phep-ton.service'

@Component({
  selector: 'app-user-info',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  templateUrl: './user-info.component.html',
  styleUrls: ['./user-info.component.css'],
})
export class UserInfoComponent implements OnInit {
  userInfo: any;
  nghiPhepResults: { [key: string]: number } = {};   totalDaysOff: number = 0;
  totalDaysRemaining: number = 0;
  currentYear: number;
  phepTonDetails: any = null;

  constructor(private nghiPhepService: NghiPhepService, private authService: AuthService, 
              private translate: TranslateService, private phepTonService: PhepTonService) {
    this.userInfo = authService.getUserInfo();
    this.currentYear = new Date().getFullYear();
    this.translate.setDefaultLang('vi');
    const savedLang = localStorage.getItem('language') || 'vi';
    this.translate.use(savedLang);
  }

  ngOnInit(): void {
    if (this.userInfo?.ma_nv) {
      this.loadPhepTonData(this.userInfo.ma_nv);
    }
  }
  loadPhepTonData(maNV: string): void {
    this.phepTonService.getPhepTonByMaNV(maNV).subscribe(
      (data) => {
        this.totalDaysOff = data.totalNgayNghi;
        this.totalDaysRemaining = data.phepTon;
        this.phepTonDetails = data;
        this.totalDaysOff = data.totalNgayNghi;
        this.totalDaysRemaining = data.phepTon;

        // Chuyển dữ liệu nhận được thành object { lyDo: số ngày }
        this.nghiPhepResults = {};
        data.chiTietNgayNghi.forEach((item: any) => {
          this.nghiPhepResults[item.lyDo] = item.soNgay;
        });
      },
      (error) => {
        console.error('Lỗi khi lấy dữ liệu phép tồn:', error);
      }
    );
  }
  getSoNgayDaNghi(lyDo: string): number {
    return this.nghiPhepResults[lyDo] || 0;
  }
  removeVietnameseTones(str: string): string {
    return str
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")  
      .replace(/đ/g, "d")   
      .replace(/Đ/g, "D");  
  }
}
