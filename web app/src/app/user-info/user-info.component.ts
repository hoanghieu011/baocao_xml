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
  phepTonDetails: any = null;

  constructor(private nghiPhepService: NghiPhepService, private authService: AuthService, 
              private translate: TranslateService, private phepTonService: PhepTonService) {
    this.userInfo = authService.getUserInfo();
    this.translate.setDefaultLang('vi');
    const savedLang = localStorage.getItem('language') || 'vi';
    this.translate.use(savedLang);
  }

  ngOnInit(): void {
    
  }
  
  removeVietnameseTones(str: string): string {
    return str
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")  
      .replace(/đ/g, "d")   
      .replace(/Đ/g, "D");  
  }
}
