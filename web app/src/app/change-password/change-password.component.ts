import { Component } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { TranslateService, TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  templateUrl: './change-password.component.html',
  styleUrls: ['./change-password.component.css']
})
export class ChangePasswordComponent {
  email: string = '';
  oldPassword: string = '';
  newPassword: string = '';
  confirmPassword: string = '';
  message: string = '';
  error: string = '';
  userInfo: any;

  constructor(private authService: AuthService, private translate: TranslateService, private router: Router, private http: HttpClient) {
    this.userInfo = this.authService.getUserInfo();
    console.log(this.userInfo.USER_NAME);
    this.email = this.userInfo.USER_NAME;
    this.translate.setDefaultLang('vi');
    const savedLang = localStorage.getItem('language') || 'vi';
    this.translate.use(savedLang);
  }

  changePassword(event: Event): void {
    if (this.oldPassword === '' || this.newPassword === '' || this.confirmPassword === '') {
      this.error = 'VL_NHAP_DU_TT';
      this.message = '';
      return;
    } else if (this.newPassword !== this.confirmPassword) {
      this.error = 'MK_KHONG_KHOP';
      this.message = '';
      return;
    } else if (this.oldPassword === this.newPassword) {
      this.error = 'MK_TRUNG'
      this.message = '';
      return;
    }

    this.authService.changePassword(this.email, this.oldPassword, this.newPassword).subscribe(success => {
      if (success) {
        this.message = 'DOI_MK_THANH_CONG';
        this.error = '';
        this.oldPassword = '';
        this.newPassword = '';
        this.confirmPassword = '';
      } else {
        this.error = 'DOI_MK_KO_TC';
        this.message = '';
        this.oldPassword = '';
        this.newPassword = '';
        this.confirmPassword = '';
      }
    },
      (error: HttpErrorResponse) => {
        if (error.status === 401) {
          localStorage.removeItem('token')
          this.router.navigate(['/login']);
        } else {
          this.oldPassword = '';
          this.newPassword = '';
          this.confirmPassword = '';
          // console.error('Đổi mật khẩu không thành công', error);
        }
      }
    );
  }

  onOldPasswordChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.oldPassword = input.value;
  }

  onNewPasswordChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.newPassword = input.value;
  }

  onConfirmPasswordChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.confirmPassword = input.value;
  }
}
