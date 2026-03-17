import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { Router } from '@angular/router';
import { jwtDecode } from "jwt-decode";
import { HttpConfigService } from './http-config.service'

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  public loggedIn = false;
  private apiUrl = this.httpConfig.getApiUrl('Login')

  constructor(private http: HttpClient, private router: Router, private httpConfig: HttpConfigService) { }

  login(user_name: string, password: string): Observable<boolean> {
    const headers = new HttpHeaders({ 'Content-Type': 'application/json' });
    const body = { user_name, password };

    return this.http.post<{ message: string; token: string }>(this.apiUrl, body, { headers }).pipe(
      map((response) => {
        if (response.message === 'Login complete!') {
          this.loggedIn = true;

          localStorage.setItem('token', response.token);

          return true;
        } else {
          this.loggedIn = false;
          return false;
        }
      }),
      catchError((error) => {
        console.error('Login failed', error);
        return throwError(error);
      })
    );
  }
  getUserInfo(): any {
    const token = localStorage.getItem('token');
    if (!token) return null;

    try {
      const decodedToken: any = jwtDecode(token);
      return {
        USER_ID: decodedToken.USER_ID,
        FULL_NAME: decodedToken.FULL_NAME,
        USER_NAME: decodedToken.USER_NAME,
        OFFICER_ID: decodedToken.OFFICER_ID,
        USER_LEVEL: decodedToken.USER_LEVEL,
        CSYTID: decodedToken.CSYTID,
        STATUS: decodedToken.STATUS,
        NOTE: decodedToken.NOTE,
        roles: decodedToken.ROLE,
      };
    } catch (error) {
      return null;
    }
  }

  getRole(): string | null {
    const userInfo = this.getUserInfo();
    return userInfo?.roles || null;
  }

  hasRole(role: string): boolean {
    const userRoles = this.getRole()?.split(',') || [];
    return userRoles.includes(role);
  }

  public redirectBasedOnRole() {
    const userInfo = this.getUserInfo();
    const roles = userInfo?.roles?.split(',').map((r: string) => r.trim()) || [];

    if (roles.includes('tao_phieu')) {
      this.router.navigate(['/tao-phieu-nghi-phep']);
    } else if (roles.includes('xu_ly')) {
      this.router.navigate(['/xu-ly-phieu-nghi']);
    } else if (roles.includes('bao_cao')) {
      this.router.navigate(['/bao-cao']);
    } else if (roles.includes('admin')) {
      this.router.navigate(['/quan-ly-nhan-vien']);
    } else {
      this.router.navigate(['tai-khoan/thong-tin-ca-nhan']);
    }
  }

  changePassword(user_name: string, oldPassword: string, newPassword: string): Observable<boolean> {
    const token = localStorage.getItem('token');
    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
    const body = { user_name, oldPassword, newPassword };

    return this.http.post<{ message: string }>(`${this.apiUrl}/ChangePassword`, body, { headers }).pipe(
      map((response) => {
        return response.message === 'Đổi mật khẩu thành công.';
      }),
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          return throwError(() => error);
        }
        return of(false);
      })
    );
  }
  resetPassword(user_name: string, newPassword: string): Observable<boolean> {
    const body = { user_name, newPassword };
    const token = localStorage.getItem('token');
    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
    return this.http.post<{ message: string }>(`${this.apiUrl}/ResetPassword`, body, { headers }).pipe(
      map((response) => {
        return response.message === 'Đặt lại mật khẩu thành công!';
      }),
      catchError((error: HttpErrorResponse) => {
        console.error('Đổi mật khẩu không thành công.', error);
        return of(false);
      })
    );
  }

  isLoggedIn(): boolean {
    const token = localStorage.getItem('token');
    if (!token) return false;

    try {
      const decodedToken: any = jwtDecode(token);
      const currentTime = Math.floor(Date.now() / 1000);

      if (decodedToken.exp && decodedToken.exp < currentTime) {
        this.logout();
        return false;
      }

      return true;
    } catch (error) {
      this.logout();
      return false;
    }
  }


  logout(): void {
    this.loggedIn = false;
    localStorage.removeItem('token');
  }
}
