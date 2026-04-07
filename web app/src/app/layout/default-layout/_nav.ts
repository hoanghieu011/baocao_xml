import { INavData } from '@coreui/angular';

export interface INavDataExtended extends INavData {
  roles?: string[];
  notificationCount?: number; 
  translationKey?: string;
  children?: INavDataExtended[];
}

export const navItems: INavDataExtended[] = [
  {
    name: 'Báo cáo và tra cứu',
    iconComponent: { name: 'cilSearch' },
    url: '/baocao-tracuu',
    roles: ['all'],
    children: [
      {
        name: 'Danh sách bệnh nhân',
        roles: ['all'],
        url: '/baocao-tracuu/ds_benhnhan',
        iconComponent: { name: 'cil-people' }
      },
      {
        name: 'Báo cáo doanh thu theo bác sĩ chỉ định',
        roles: ['all'],
        url: '/baocao-tracuu/doanhthu_bscd',
        iconComponent: { name: 'cil-chart' }
      },
      {
        name: 'Báo cáo doanh thu theo bác sĩ thực hiện',
        roles: ['all'],
        url: '/baocao-tracuu/doanhthu_bsth',
        iconComponent: { name: 'cil-chart' }
      },
      {
        name: 'Báo cáo doanh thu theo khoa (chi tiết theo nhóm dịch vụ)',
        roles: ['all'],
        url: '/baocao-tracuu/doanhthu_khoa_ct',
        iconComponent: { name: 'cil-chart' }
      },
      {
        name: 'Báo cáo doanh thu theo khoa',
        roles: ['all'],
        url: '/baocao-tracuu/doanhthu_khoa',
        iconComponent: { name: 'cil-chart' }
      },
      {
        name: 'Báo cáo doanh thu toàn viện',
        roles: ['all'],
        url: '/baocao-tracuu/doanhthu_toanvien',
        iconComponent: { name: 'cil-chart' }
      }
    ]
  },
  {
    name: 'Danh mục',
    iconComponent: { name: 'cilFolder' },
    url: '/danhmuc',
    roles: ['all'],
    children: [
      {
        name: 'Danh mục dịch vụ',
        roles: ['all'],
        url: '/danhmuc/dich_vu',
        iconComponent: { name: 'cilMedicalCross' }
      },
      {
        name: 'Danh mục lấy mẫu',
        roles: ['all'],
        url: '/danhmuc/lay_mau',
        iconComponent: { name: 'cilMedicalCross' }
      }
    ]
  },
  {
    title: true,
    name: 'Nghỉ phép',
    translationKey: 'MENU.LEAVE',
    roles: ['tao_phieu']
  },
  {
    name: 'Tạo phiếu nghỉ phép',
    url: '/tao-phieu-nghi-phep',
    translationKey: 'MENU.CREATE_LEAVE_FORM',
    iconComponent: { name: 'cil-pencil' },
    roles: ['tao_phieu']
  },
  {
    name: 'Danh sách phiếu',
    url: '/danh-sach-phieu-nghi-phep',
    notificationCount: 123,
    translationKey: 'MENU.LEAVE_LIST',
    iconComponent: { name: 'cil-description' },
    roles: ['tao_phieu'],
    badge: {
      color: 'info',
      text: ''
    }
  },
  {
    title: true,
    name: 'Quản lý nghỉ phép',
    translationKey: 'MENU.QUAN_LY_NGHI_PHEP',
    roles: ['xu_ly']
  },
  {
    name: 'Xử lý phiếu nghỉ',
    translationKey: 'MENU.XU_LY_PHIEU_NGHI',
    notificationCount: 123,
    url: '/xu-ly-phieu-nghi',
    iconComponent: { name: 'cil-calendar' },
    roles: ['xu_ly'],
    badge: {
      color: 'danger',
      text: ''
    }
  },
  {
    title: true,
    name: 'Báo cáo',
    translationKey: 'MENU.BAO_CAO',
    roles: ['bao_cao', 'bao_bp_cao_bo_phan']
  },
  {
    name: 'Báo cáo theo bộ phận',
    translationKey: 'MENU.BAO_CAO_BO_PHAN',
    url: '/bao-cao-bo-phan',
    iconComponent: { name: 'cil-chart-pie' },
    roles: ['bao_bp_cao_bo_phan']
  },
  {
    name: 'Báo cáo nghỉ phép',
    translationKey: 'MENU.BAO_CAO_NGHI_PHEP',
    url: '/bao-cao-nghi-phep',
    iconComponent: { name: 'cil-chart' },
    roles: ['bao_cao']
  },
  {
    title: true,
    name: 'Quản trị hệ thống',
    translationKey: 'MENU.QUAN_TRI_HE_THONG',
    roles: ['admin', 'bao_bp_cao_bo_phan']
  },
  {
    name: 'Quản lý nhân viên',
    translationKey: 'MENU.QUAN_LY_NHAN_VIEN',
    url: '/quan-ly-nhan-vien',
    iconComponent: { name: 'cil-people' },
    roles: ['admin']
  },
  {
    name: 'Quản lý phiếu nghỉ',
    translationKey: 'MENU.QUAN_LY_PHIEU_NGHI',
    url: '/quan-ly-phieu-nghi',
    iconComponent: { name: 'cil-notes' },
    roles: ['admin', 'bao_bp_cao_bo_phan']
  },
  {
    name: 'Ngày nghỉ cố định',
    translationKey: 'MENU.NGAY_NGHI_CD',
    url: '/ngay-nghi-co-dinh',
    iconComponent: { name: 'cilFlagAlt' },
    roles: ['admin']
  },
  {
    name: 'Phép tồn',
    translationKey: 'PHEP_TON',
    url: '/phep-ton',
    iconComponent: { name: 'cil-calculator' },
    roles: ['admin']
  },
  {
    name: 'Phân quyền',
    translationKey: 'MENU.PHAN_QUYEN',
    url: '/phan-quyen',
    iconComponent: { name: 'cil-puzzle' },
    roles: ['admin']
  },
  {
    title: false,
    name: 'Thông tin tài khoản',
    url: '/tai-khoan',
    translationKey: 'MENU.THONG_TIN_TAI_KHOAN',
    roles: ['all'],
    iconComponent: { name: 'cibAboutMe' },
    children: [
      {
        name: 'Thông tin cá nhân',
        translationKey: 'MENU.THONG_TIN_CA_NHAN',
        url: '/tai-khoan/thong-tin-ca-nhan',
        iconComponent: { name: 'cil-user' },
        roles: ['all']
      },
      {
        name: 'Đổi mật khẩu',
        translationKey: 'MENU.DOI_MAT_KHAU',
        url: '/tai-khoan/doi-mat-khau',
        iconComponent: { name: 'cil-lock-locked' },
        roles: ['all']
      },
    ]
  },
  
];
export const navItems1: INavDataExtended[] = [
  {
    name: 'Đăng xuất',
    translationKey: 'MENU.DANG_XUAT',
    url: '#',
    iconComponent: { name: 'cil-account-logout' },
    roles: ['all']
  }
];
