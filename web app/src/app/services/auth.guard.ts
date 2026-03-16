import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root',
})
export class AuthGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
      return false;
    }

    const userInfo = this.authService.getUserInfo();
    const userRoles = userInfo?.roles?.split(',').map((r: string) => r.trim()) || [];

    const requiredRoles = route.data['roles'] as string[];
    const hasPermission = requiredRoles?.some(role => role === '' || userRoles.includes(role));

    if (!hasPermission) {
      this.router.navigate(['/404']);
      return false;
    }

    return true;
  }
}
