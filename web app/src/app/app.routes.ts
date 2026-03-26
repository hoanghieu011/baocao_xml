import { RouterModule, Routes } from '@angular/router';
import { DefaultLayoutComponent } from './layout';
import { AuthGuard } from './services/auth.guard';
import { NgModule } from '@angular/core';
import { RouteTitleResolver } from './services/route-title.resolver';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./login/login.component').then(m => m.LoginComponent),
  },
  {
    path: '',
    redirectTo: 'tai-khoan/thong-tin-ca-nhan',
    pathMatch: 'full'
  },
  {
    path: '',
    component: DefaultLayoutComponent,
    canActivate: [AuthGuard],
    data: {
      title: 'Quản lý báo cáo',
      roles: ['']
    },
    children: [
      {
        path: 'tao-phieu-nghi-phep',
        canActivate: [AuthGuard],
        loadChildren: () => import('../app/tao-phieu-nghi-phep/tao-phieu-nghi-phep.routes').then(m => m.TAO_PHIEU_NGHI_PHEP_ROUTES),
        data: {
          title: 'MENU.CREATE_LEAVE_FORM',
          roles: ['tao_phieu']
        },
      },
      {
        path: 'danh-sach-phieu-nghi-phep',
        canActivate: [AuthGuard],
        loadChildren: () => import('../app/danh-sach-phieu/danh-sach-phieu.routes').then(m => m.DANH_SACH_PHIEU),
        data: {
          title: 'MENU.LEAVE_LIST',
          roles: ['tao_phieu']
        }
      },
      {
        path: 'tai-khoan',
        children: [
          {
            path: 'thong-tin-ca-nhan',
            canActivate: [AuthGuard],
            loadChildren: () => import('../app/user-info/user-info.routes').then(m => m.USER_INFO_ROUTES),
            data: {
              title: 'MENU.THONG_TIN_CA_NHAN',
              roles: ['']
            }
          },
          {
            path: 'doi-mat-khau',
            canActivate: [AuthGuard],
            loadChildren: () => import('../app/change-password/change-password.routes').then(m => m.DOI_MAT_KHAU),
            data: {
              title: 'MENU.DOI_MAT_KHAU',
              roles: ['']
            }
          },
        ]
      },
      {
        path: 'xu-ly-phieu-nghi',
        canActivate: [AuthGuard],
        loadChildren: () => import('../app/xu-ly-phieu-nghi/xu-ly-phieu-nghi.routes').then(m => m.XU_LY_PHIEU),
        data: {
          title: 'MENU.XU_LY_PHIEU_NGHI',
          roles: ['xu_ly']
        }
      },
      {
        path: 'phep-ton',
        canActivate: [AuthGuard],
        loadChildren: () => import('../app/phep-ton/phep-ton.routes').then(m => m.PHEP_TON),
        data: {
          title: 'PHEP_TON',
          roles: ['admin']
        }
      },
      {
        path: 'bao-cao-nghi-phep',
        canActivate: [AuthGuard],
        loadChildren: () => import('./bao-cao/bao-cao.routes').then(m => m.BAO_CAO),
        data: {
          title: 'MENU.BAO_CAO',
          roles: ['bao_cao']
        }
      },
      {
        path: 'ngay-nghi-co-dinh',
        canActivate: [AuthGuard],
        loadChildren: () => import('./ngay-nghi-co-dinh/ngay-nghi-co-dinh.routes').then(m => m.NGAY_NGHI_CO_DINH),
        data: {
          title: 'MENU.NGAY_NGHI_CD',
          roles: ['admin']
        }
      },
      {
        path: 'bao-cao-bo-phan',
        canActivate: [AuthGuard],
        loadChildren: () => import('./bao-cao-bo-phan/bao-cao-bo-phan.routes').then(m => m.BAO_CAO_BO_PHAN),
        data: {
          title: 'MENU.BAO_CAO_BO_PHAN',
          roles: ['bao_bp_cao_bo_phan']
        }
      },
      {
        path: 'bao-cao-nhan-vien',
        canActivate: [AuthGuard],
        loadChildren: () => import('./bao-cao-nhan-vien/bao-cao-nhan-vien.routers').then(m => m.BAO_CAO_NHAN_VIEN),
        data: {
          title: 'MENU.BAO_CAO_NHAN_VIEN',
          roles: ['bao_cao']
        }
      },
      {
        path: 'quan-ly-nhan-vien',
        canActivate: [AuthGuard],
        loadChildren: () => import('../app/quan-ly-nhan-vien/quan-ly-nhan-vien.routes').then(m => m.QUAN_LY_NV),
        data: {
          title: 'MENU.QUAN_LY_NHAN_VIEN',
          roles: ['admin']
        }
      },
      {
        path: 'quan-ly-phieu-nghi',
        canActivate: [AuthGuard],
        loadChildren: () => import('../app/quan-ly-phieu-nghi/quan-ly-phieu-nghi.routes').then(m => m.QUAN_LY_PHIEU_NGHI),
        data: {
          title: 'MENU.QUAN_LY_PHIEU_NGHI',
          roles: ['admin', 'bao_bp_cao_bo_phan']
        }
      },
      {
        path: 'phan-quyen',
        canActivate: [AuthGuard],
        loadChildren: () => import('../app/phan-quyen/phan-quyen.routes').then(m => m.PHAN_QUYEN),
        data: {
          title: 'MENU.PHAN_QUYEN',
          roles: ['admin']
        }
      },
      {
        path: 'baocao-tracuu',
        children: [
          {
            path: 'ds_benhnhan',
            canActivate: [AuthGuard],
            loadChildren: () => import('../app/ds-benhnhan/ds-benhnhan.routes').then(m => m.DS_BENHNHAN),
            data: {
              title: 'Danh sách bệnh nhân',
              roles: ['']
            }
          }, 
          {
            path: 'doanhthu_bscd',
            canActivate: [AuthGuard],
            loadChildren: () => import('../app/bc-doanhthu-bscd/bc-doanhthu-bscd.routes').then(m => m.DOANHTHU_BSCD),
            data: {
              title: 'Báo cáo doanh thu theo bác sĩ chỉ định',
              roles: ['']
            }
          }, 
          {
            path: 'doanhthu_bsth',
            canActivate: [AuthGuard],
            loadChildren: () => import('../app/bc-doanhthu-bsth/bc-doanhthu-bsth.routes').then(m => m.DOANHTHU_BSTH),
            data: {
              title: 'Báo cáo doanh thu theo bác sĩ thực hiện',
              roles: ['']
            }
          }, 
          {
            path: 'doanhthu_khoa_ct',
            canActivate: [AuthGuard],
            loadChildren: () => import('../app/bc-doanhthu-khoa-chitiet/bc-doanhthu-khoa-chitiet.routes').then(m => m.DOANHTHU_KHOA_CT),
            data: {
              title: 'Báo cáo doanh thu theo khoa (chi tiết theo nhóm dịch vụ)',
              roles: ['']
            }
          },
          {
            path: 'doanhthu_khoa',
            canActivate: [AuthGuard],
            loadChildren: () => import('../app/bc-doanhthu-khoa/bc-doanhthu-khoa.routes').then(m => m.DOANHTHU_KHOA),
            data: {
              title: 'Báo cáo doanh thu theo khoa',
              roles: ['']
            }
          }, 
        ]
      },
      {
        path: 'danhmuc',
        children: [
          {
            path: 'dich_vu',
            canActivate: [AuthGuard],
            loadChildren: () => import('../app/dich-vu/dich-vu.routes').then(m => m.DS_DICHVU),
            data: {
              title: 'Danh mục dịch vụ',
              roles: ['']
            }
          },
          {
            path: 'lay_mau',
            canActivate: [AuthGuard],
            loadChildren: () => import('../app/lay-mau/lay-mau.routes').then(m => m.DS_LAYMAU),
            data: {
              title: 'Danh mục lấy mẫu',
              roles: ['']
            }
          }
        ]
      },
    ]
  },
  {
    path: '',
    loadChildren: () => import('../views/pages/routes').then(m => m.routes),
    data: {
      title: 'Page'
    }
  },
  { path: '**', redirectTo: 'login' }
];
@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {
  static routes = routes;
}
