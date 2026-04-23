import { NgStyle, NgTemplateOutlet } from '@angular/common';
import { Component, computed, inject, input, OnInit } from '@angular/core';
import { RouterLink, RouterLinkActive, Router, NavigationEnd, ActivatedRoute } from '@angular/router';

import {
  AvatarComponent,
  BadgeComponent,
  BreadcrumbRouterComponent,
  ColorModeService,
  ContainerComponent,
  DropdownComponent,
  DropdownDividerDirective,
  DropdownHeaderDirective,
  DropdownItemDirective,
  DropdownMenuDirective,
  DropdownToggleDirective,
  HeaderComponent,
  HeaderNavComponent,
  HeaderTogglerDirective,
  NavItemComponent,
  NavLinkDirective,
  ProgressBarDirective,
  ProgressComponent,
  SidebarToggleDirective,
  TextColorDirective,
  ThemeDirective
} from '@coreui/angular';

import { IconDirective } from '@coreui/icons-angular';
import { TranslateService, TranslateModule } from '@ngx-translate/core';
import {BreadcrumbWrapperComponent} from 'src/app/breadcrumb-wrapper/breadcrumb-wrapper.component';
import { BenhVienService} from '../../../services/benh-vien.service';

@Component({
  selector: 'app-default-header',
  templateUrl: './default-header.component.html',
  styleUrl: './default-header.component.scss',
  standalone: true,
  imports: [ContainerComponent, HeaderTogglerDirective, SidebarToggleDirective,
    IconDirective, HeaderNavComponent, NavItemComponent, NavLinkDirective,
    RouterLink, RouterLinkActive, NgTemplateOutlet, BreadcrumbRouterComponent,
    ThemeDirective, DropdownComponent, DropdownToggleDirective, TextColorDirective,
    AvatarComponent, DropdownMenuDirective, DropdownHeaderDirective, DropdownItemDirective,
    BadgeComponent, DropdownDividerDirective, ProgressBarDirective, ProgressComponent,
    NgStyle, TranslateModule, BreadcrumbWrapperComponent]
}) 
export class DefaultHeaderComponent extends HeaderComponent{

  readonly #colorModeService = inject(ColorModeService);
  readonly colorMode = this.#colorModeService.colorMode;
  pageTitle: string = '';

  tenbenhvien: string = '';

  readonly colorModes = [
    { name: 'light', text: 'Light', icon: 'cilSun' },
    { name: 'dark', text: 'Dark', icon: 'cilMoon' },
    { name: 'auto', text: 'Auto', icon: 'cilContrast' }
  ];

  readonly icons = computed(() => {
    const currentMode = this.colorMode();
    return this.colorModes.find(mode => mode.name === currentMode)?.icon ?? 'cilSun';
  });

  constructor(private translate: TranslateService, private benhVienService: BenhVienService) {
    super();
    this.translate.setDefaultLang('vi');
    const savedLang = localStorage.getItem('language') || 'vi';
    this.translate.use(savedLang);
    this.loadTenBenhVien();
  }
  switchLanguage(lang: string) {
    this.translate.use(lang);
    localStorage.setItem('language', lang);
  }
  sidebarId = input('sidebar1');

  private loadTenBenhVien() {
    this.benhVienService.getTtBenhVien().subscribe({
      next: (res) => {
        const data = res?.data || [];

        this.tenbenhvien = data[0].tenbenhvien;
        localStorage.setItem('TenBenhVien', this.tenbenhvien);
      },
      error: (err) => {
        console.error(err);
      }
    });
  }
}
