import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NghiPhepService } from '../services/nghi-phep.service';
import { AuthService } from '../services/auth.service';
import { TranslateService, TranslateModule } from '@ngx-translate/core';
import { OfficerService } from '../services/officer.service'

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
  phepTonDetails: any = null;

  constructor(private nghiPhepService: NghiPhepService, private authService: AuthService, 
              private translate: TranslateService, private officerService: OfficerService) {
    this.translate.setDefaultLang('vi');
    const savedLang = localStorage.getItem('language') || 'vi';
    this.translate.use(savedLang);
  }

  ngOnInit(): void {
    this.loadTtOfficer();
  }

  loadTtOfficer(){
    this.officerService.getTtOfficer().subscribe({
      next: (res) => {
        const data = res?.data || [];
        this.userInfo = data[0];
      
      },
      error: (err) => {
        console.error(err);
      }
    });
  }
  
  getGioiTinh(gt: number): string {
    if (gt === 1) return 'Nam';
    if (gt === 2) return 'Nữ';
    return 'Khác';
  }

  formatDate(date: string): string {
    if (!date) return '-';
    const d = new Date(date);
    if (isNaN(d.getTime())) return '-';

    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();

    return `${day}/${month}/${year}`;
  }

  removeVietnameseTones(str: string): string {
    return str
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")  
      .replace(/đ/g, "d")   
      .replace(/Đ/g, "D");  
  }
}
