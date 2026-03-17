import { Component, OnInit } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { NgScrollbar } from 'ngx-scrollbar';
import { CommonModule } from '@angular/common';
import { IconDirective } from '@coreui/icons-angular';
import {
  ContainerComponent,
  ShadowOnScrollDirective,
  SidebarBrandComponent,
  SidebarComponent,
  SidebarFooterComponent,
  SidebarHeaderComponent,
  SidebarNavComponent,
  SidebarToggleDirective,
  SidebarTogglerDirective
} from '@coreui/angular';

import { DefaultFooterComponent, DefaultHeaderComponent } from './';
import { navItems, navItems1, INavDataExtended } from './_nav';
import { AuthService } from 'src/app/services/auth.service';
import { NghiPhepSearchDto, NghiPhepService } from 'src/app/services/nghi-phep.service';
import { HttpErrorResponse } from '@angular/common/http';

import { TranslateService } from '@ngx-translate/core';
import { ItemsList } from '@ng-select/ng-select/lib/items-list';

@Component({
  selector: 'app-dashboard',
  templateUrl: './default-layout.component.html',
  styleUrls: ['./default-layout.component.scss'],
  standalone: true,
  imports: [
    SidebarComponent,
    SidebarHeaderComponent,
    SidebarBrandComponent,
    RouterLink,
    IconDirective,
    NgScrollbar,
    SidebarNavComponent,
    SidebarFooterComponent,
    SidebarToggleDirective,
    SidebarTogglerDirective,
    DefaultHeaderComponent,
    ShadowOnScrollDirective,
    ContainerComponent,
    RouterOutlet,
    DefaultFooterComponent,
    CommonModule
  ]
})
export class DefaultLayoutComponent implements OnInit {
  public navItems_g: INavDataExtended[] = []
  public navItems: INavDataExtended[] = [];
  public navItems1 = navItems1;
  private notificationCount: number = 0;

  constructor(private router: Router, private authService: AuthService,
    private nghiPhepService: NghiPhepService, private translate: TranslateService) {
    this.navItems_g = this.filterNavItemsByRole();
    this.translate.setDefaultLang('vi');
    const savedLang = localStorage.getItem('language') || 'vi';
    this.translate.use(savedLang);

    this.loadNavItems();

    this.translate.onLangChange.subscribe(() => {
      this.loadNavItems();
    });
  }
  loadNavItems() {
    this.navItems = this.navItems_g.map(item => ({
      ...item,
      name: item.translationKey ? this.translate.instant(item.translationKey) : item.name
    }));
    this.navItems1 = navItems1.map(item => ({
      ...item,
      name: item.translationKey ? this.translate.instant(item.translationKey) : item.name
    }));
  }

  private filterNavItemsByRole(): INavDataExtended[] {
    const userInfo = this.authService.getUserInfo();
    const userRoles = userInfo?.roles?.split(',').map((r: string) => r.trim()) || [];

    return navItems.filter(item => {
      return item.roles?.some(role => role === 'all' || userRoles.includes(role));
    });
  }

  ngOnInit() {
    
  }
  updateBadge_CN(notificationCount: number) {
    this.navItems = this.navItems.map(item => {
      if (item.translationKey === 'MENU.LEAVE_LIST') {
        return {
          ...item,
          badge: {
            text: notificationCount > 0 ? `${notificationCount}` : '',
            color: item.badge?.color ?? 'info',
            size: item.badge?.size,
            class: item.badge?.class
          }
        } as INavDataExtended;
      }
      return item;
    });
  }


  onScrollbarUpdate($event: any) {
  }

  logout() {
    this.authService.loggedIn = false;
    localStorage.removeItem('token');
    localStorage.removeItem('ma_nv')
    localStorage.removeItem('password')
    this.router.navigate(['/login']);
    // window.location.reload()
  }
}
