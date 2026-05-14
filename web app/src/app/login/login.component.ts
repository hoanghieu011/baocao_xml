import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { FormsModule } from '@angular/forms';
import { TranslateService, TranslateModule  } from '@ngx-translate/core';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, CommonModule, TranslateModule ],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {
  user_name: string = '';
  password: string = '';
  rememberMe: boolean = false;
  loginFalse: boolean = false;
  connectFalse: boolean = false;
  hidePassword: boolean = true;

  captchaText: string = '';
  userCaptcha: string = '';
  captchaImage: string = '';
  captchaValid: boolean = true

  constructor(private authService: AuthService, private router: Router, private translate: TranslateService) {
    this.generateCaptcha();
    this.translate.setDefaultLang('vi'); 
    const savedLang = localStorage.getItem('language') || 'vi';
    this.translate.use(savedLang);
  }
  togglePasswordVisibility() {
    this.hidePassword = !this.hidePassword;
  }
  switchLanguage(lang: string) {
    this.translate.use(lang);
    localStorage.setItem('language', lang);
  }

  ngOnInit() {
    const token = localStorage.getItem('token');
    if (this.authService.isLoggedIn()) {
      this.authService.redirectBasedOnRole();
      return;
    }
    const storedEmail = localStorage.getItem('user_name');
    const storedPassword = localStorage.getItem('password');

    if (storedEmail && storedPassword) {
      this.user_name = storedEmail;
      this.password = storedPassword;
      this.rememberMe = true;
    }
  }
  refreshCaptcha(){
    this.generateCaptcha();
    this.userCaptcha = ''
  }

  generateCaptcha() {
    const chars = '0123456789';
    this.captchaText = Array.from({ length: 4 }, () => chars.charAt(Math.floor(Math.random() * chars.length))).join('');
    this.drawCaptcha();
  }

  drawCaptcha() {
    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');
    canvas.width = 130;
    canvas.height = 60;

    if (ctx) {
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      ctx.fillStyle = '#f3f3f3';
      ctx.fillRect(0, 0, canvas.width, canvas.height);

      ctx.font = '40px Arial';
      ctx.fillStyle = '#089';
      ctx.fillText(this.captchaText, 20, 40);

      for (let i = 0; i < 5; i++) {
        ctx.beginPath();
        ctx.moveTo(Math.random() * canvas.width, Math.random() * canvas.height);
        ctx.lineTo(Math.random() * canvas.width, Math.random() * canvas.height);
        ctx.strokeStyle = 'rgba(0, 0, 0, 0.5)';
        ctx.stroke();
      }

      this.captchaImage = canvas.toDataURL();
    } else {
      console.error('Cannot get canvas context');
    }
  }

  onSubmit() {
    this.loginFalse = false
    this.captchaValid = true
    this.connectFalse = false
    // if (this.userCaptcha !== this.captchaText) {
    //   this.captchaValid = false
    //   this.refreshCaptcha();
    //   return;
    // }
    if (!this.user_name || !this.password) {
      this.loginFalse = true;
      this.connectFalse = false;
      return;
    }
    this.authService.login(this.user_name, this.password).subscribe(
      (isLoggedIn: boolean) => {
        if (isLoggedIn) {
          // localStorage.setItem('full_name')
          localStorage.setItem('user_name', this.user_name);
          if (this.rememberMe) {
            
            localStorage.setItem('password', this.password);
          } else {
            localStorage.removeItem('password');
          }
          this.authService.redirectBasedOnRole();
        } else {
          this.loginFalse = true;
          this.connectFalse = false;
        }
      },
      (error) => {
        this.refreshCaptcha();
        if (error.status === 401) {
          this.loginFalse = true;
          this.connectFalse = false;
        } else if (error.status === 0) {
          this.connectFalse = true;
          this.loginFalse = false;
        }
        console.error('Error:', error);
      }
    );
  }
}
