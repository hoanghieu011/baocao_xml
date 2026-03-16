import { Component, OnInit, ViewChild, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Select2Module, Select2Data, Select2UpdateEvent } from 'ng-select2-component';
import { QuillModule } from 'ngx-quill';
import { MatDatepicker, MatDatepickerInputEvent } from '@angular/material/datepicker';
import { MatCalendarCellClassFunction } from '@angular/material/datepicker';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { Form, FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatNativeDateModule } from '@angular/material/core';
import { debounceTime, switchMap, tap, finalize, Subject, Observable } from 'rxjs';
import { ChangeDetectionStrategy, HostBinding } from '@angular/core';
import { UntypedFormGroup, UntypedFormControl } from '@angular/forms';
import { OverlayContainer } from '@angular/cdk/overlay';
import { DateClass, DateRemoveEvent } from 'ngx-multiple-dates';
import { NgModule } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { /*MAT_DATE_LOCALE,*/ MatRippleModule } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule, MatIconRegistry } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatToolbarModule } from '@angular/material/toolbar';
import { NgxMultipleDatesModule } from 'ngx-multiple-dates';
import { Router } from '@angular/router';
import { LoaiPhepService } from '../services/loai-phep.service';
import { LoaiPhep } from '../models/loai-phep.model';
import { AuthService } from '../services/auth.service';
import { LyDoNghiService } from '../services/ly-do-nghi.service'
import { NghiPhepService, NghiPhepDto } from '..//services/nghi-phep.service'
import { LyDoNghi } from '../models/ly-do-nghi.model';
import { NhanVienService } from '../services/nhan-vien.service';
import { NgSelectModule } from '@ng-select/ng-select';
import { TranslateService, TranslateModule } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { NgayNghiCoDinhService } from '../services/ngay-nghi-co-dinh.service'
import { PhepTonService } from '../services/phep-ton.service';

@Component({
  selector: 'app-tao-phieu-nghi-phep',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, Select2Module, QuillModule,
    MatDatepickerModule, MatNativeDateModule,
    CommonModule,
    HttpClientModule,
    FormsModule,
    MatNativeDateModule,
    MatRippleModule,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatIconModule,
    MatInputModule,
    MatToolbarModule,
    NgxMultipleDatesModule,
    NgSelectModule,
    TranslateModule
  ],
  templateUrl: './tao-phieu-nghi-phep.component.html',
  styleUrl: './tao-phieu-nghi-phep.component.css',
  encapsulation: ViewEncapsulation.None
})
export class TaoPhieuNghiPhepComponent implements OnInit {
  isSubmitting = false;
  ly_do_str: boolean = false;
  ly_do_touched: boolean = false;
  userInfo: any
  dsLyDo: any[] = [];
  minDate: string = '';
  ds_loai_phep: any[] = []
  er_loai_nghi_phep_str: string = ''
  er_loai_nghi_phep: boolean = false
  er_np_nam: boolean = false
  er_ly_do_nghi: boolean = true
  er_ly_do_nghi_str: string = ''
  today: string = new Date().toISOString().split('T')[0];
  nghiTuTouched = false;
  nghiDenTouched = false;
  selectedFile: File | null = null;
  constructor(private loaiPhepService: LoaiPhepService, private fb: FormBuilder,
    private nghiPhepService: NghiPhepService, private router: Router,
    private authService: AuthService, private lyDoNghiService: LyDoNghiService,
    private nhanVienService: NhanVienService, private translate: TranslateService,
    private ngayNghiCoDinhService: NgayNghiCoDinhService, private phepTonService: PhepTonService) {
    this.registerForm = this.fb.group({
      ho_ten: [''],
      bo_phan: [''],
      ma_nv: [''],
      phep_ton: [''],
      loai_nghi_phep: [''],
      xac_nhan_o: [false],
      ban_giao: [''],
      ly_do: ['', Validators.required],
      so_ngay_nghi: ['0'],
      nghi_tu: ['', Validators.required],
      nghi_den: ['', Validators.required],
      nghi_t7: [true]
    })
    this.translate.setDefaultLang('vi');
    const savedLang = localStorage.getItem('language') || 'vi';
    this.translate.use(savedLang);
    this.loadLyDoNghi();
    this.translate.onLangChange.subscribe(() => {
      this.loadLyDoNghi();
    });
  }
  selectedFileName: string | null = null;
  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      if (file.type.startsWith('image/')) {
        this.selectedFile = file;
        this.selectedFileName = file.name;
        this.registerForm.patchValue({ xac_nhan_o: false })
        this.er_xn_o = false
        this.additionalConfirmation = false;
      } else {
        alert(this.translate.instant('ER_FILE_NOT_IMAGE'));
        input.value = '';
        this.selectedFile = null;
      }
    }
  }
  quillEditorInstance: any;
  editorModules = {
    toolbar: {
      container: [
        ['bold', 'italic', 'underline'],
        // [{ 'list': 'ordered' }, { 'list': 'bullet' }],
        // [{ 'script': 'sub' }, { 'script': 'super' }],
        // [{ 'indent': '-1' }, { 'indent': '+1' }],
        // [{ 'direction': 'rtl' }],
        // [{ 'align': [] }],
      ],
    }
  };
  onEditorCreated(quillInstance: any) {
    this.quillEditorInstance = quillInstance;
  }
  ly_do_nghi: string = ''
  onLyDoChange() {
    this.ly_do_str = !this.ly_do_nghi || this.ly_do_nghi.trim() === '';
  }


  calculateSoNgayNghi() {
    const nghiTuValue = this.registerForm.get('nghi_tu')?.value;
    const nghiDenValue = this.registerForm.get('nghi_den')?.value;
    const lyDoValue = this.registerForm.get('ly_do')?.value;
    const nghiT7 = this.registerForm.get('nghi_t7')?.value;

    if (nghiTuValue && nghiDenValue) {
      const nghiTuDate = new Date(nghiTuValue);
      const nghiDenDate = new Date(nghiDenValue);

      let daysDiff = Math.ceil((nghiDenDate.getTime() - nghiTuDate.getTime()) / (1000 * 3600 * 24)) + 1;

      if (lyDoValue !== 'TS' && lyDoValue !== 'DS') {
        let countWeekendDays = 0;

        for (let d = new Date(nghiTuDate); d <= nghiDenDate; d.setDate(d.getDate() + 1)) {
          const dayOfWeek = d.getDay();

          if (!nghiT7 && (dayOfWeek === 6 || dayOfWeek === 0)) {
            countWeekendDays++;
          } else if (nghiT7 && dayOfWeek === 0) {
            countWeekendDays++;
          }
        }
        daysDiff -= countWeekendDays;

        const startYear = nghiTuDate.getFullYear();
        const endYear = nghiDenDate.getFullYear();
        let years: number[] = [];
        for (let y = startYear; y <= endYear; y++) {
          years.push(y);
        }
        // Gọi hàm getAllHolidaysByYear cho từng năm, dùng forkJoin để chờ tất cả hoàn thành
        const holidayObservables = years.map(year =>
          this.ngayNghiCoDinhService.getAllHolidaysByYear(year)
        );
        forkJoin(holidayObservables).subscribe({
          next: (responses: any[]) => {
            let holidayCount = 0;
            responses.forEach(response => {
              response.data.forEach((holiday: any) => {
                const holidayDate = new Date(holiday.ngay_nghi);
                const holidayDateOnly = new Date(holidayDate.getFullYear(), holidayDate.getMonth(), holidayDate.getDate());
                const nghiTuDateOnly = new Date(nghiTuDate.getFullYear(), nghiTuDate.getMonth(), nghiTuDate.getDate());
                const nghiDenDateOnly = new Date(nghiDenDate.getFullYear(), nghiDenDate.getMonth(), nghiDenDate.getDate());

                if (holidayDateOnly >= nghiTuDateOnly && holidayDateOnly <= nghiDenDateOnly) {
                  // Kiểm tra nếu ngày holiday không trùng với cuối tuần đã trừ
                  const dayOfWeek = holidayDate.getDay();
                  let isWeekend = false;
                  if (!nghiT7 && (dayOfWeek === 6 || dayOfWeek === 0)) {
                    isWeekend = true;
                  } else if (nghiT7 && dayOfWeek === 0) {
                    isWeekend = true;
                  }
                  if (!isWeekend) {
                    holidayCount++;
                  }
                }
              });
            });
            // Trừ thêm số ngày nghỉ lễ không trùng với cuối tuần
            daysDiff -= holidayCount;
            this.registerForm.get('so_ngay_nghi')?.setValue(daysDiff);
          },
          error: (err) => {
            console.error('Error fetching holidays for leave calculation', err);
            // Nếu có lỗi, vẫn hiển thị số ngày đã tính (không trừ ngày lễ)
            this.registerForm.get('so_ngay_nghi')?.setValue(daysDiff);
          }
        });
      } else {
        this.registerForm.get('so_ngay_nghi')?.setValue(daysDiff);
      }
    } else {
      this.registerForm.get('so_ngay_nghi')?.setValue(0);
    }
  }




  get nghiTuInvalid() {
    return this.registerForm.get('nghi_tu')?.invalid && this.registerForm.get('nghi_tu')?.touched;
  }

  get nghiDenInvalid() {
    return this.registerForm.get('nghi_den')?.invalid && this.registerForm.get('nghi_den')?.touched;
  }
  onNghiDenChange() {
    this.calculateSoNgayNghi();
  }

  onNghiTuChange() {
    const nghi_tu = this.registerForm.get('nghi_tu')?.value;
    const nghi_den = this.registerForm.get('nghi_den')?.value;

    if (nghi_tu && nghi_den && new Date(nghi_tu) > new Date(nghi_den)) {
      this.registerForm.get('nghi_den')?.setValue('');
    }

    this.calculateSoNgayNghi();
  }


  loadUserInfo() {
    const userInfo = this.authService.getUserInfo();

    if (userInfo) {
      this.userInfo = userInfo
      this.registerForm.patchValue({
        ho_ten: userInfo.full_name,
        bo_phan: userInfo.ten_bo_phan,
        ma_nv: userInfo.ma_nv,
      });
    }
  }
  er_xn_o: boolean = false
  ngOnInit(): void {
    this.loadLoaiPhep();
    this.loadUserInfo();
    this.loadLyDoNghi();
    this.loadPhepTon();
    const today = new Date();
    today.setDate(today.getDate() - 3);
    this.minDate = today.toISOString().split('T')[0];
  }
  loadLoaiPhep() {
    this.loaiPhepService.getAllLoaiPhep().subscribe(
      (data: LoaiPhep[]) => {
        this.ds_loai_phep = data.map((item: LoaiPhep) => ({
          value: item.id,
          label: item.ten_loai_phep,
          so_ngay_nghi: item.so_ngay_nghi
        }));
      },
      error => {
        if (error.status === 401) {
        } else {
        }
      }
    );
  }
  loadPhepTon(): void {
    const ma_nv = this.userInfo.ma_nv;
    const currentYear = new Date().getFullYear().toString();

    this.phepTonService.getPhepTon(ma_nv, currentYear).subscribe({
      next: (result) => {
        this.registerForm.patchValue({ phep_ton: result.phep_ton });
      },
      error: (err) => {
        // console.error("Lỗi khi tải dữ liệu phép tồn:", err);
      }
    });
  }
  loadLyDoNghi() {
    const selectedValue = this.registerForm.get('ly_do')?.value;
    this.lyDoNghiService.getAllLyDo().subscribe(
      (data: LyDoNghi[]) => {
        this.translate.get(data.map(item => item.dien_giai)).subscribe(translations => {
          this.dsLyDo = data.map(item => {
            let label = item.ky_hieu + ' - ' + (translations[item.dien_giai] || item.dien_giai);
            return {
              value: item.ky_hieu,
              label: label.replace(/^# - /, '')
            };
          });

        });
        this.registerForm.patchValue({ ly_do: null })
        setTimeout(() => {
          this.registerForm.patchValue({ ly_do: selectedValue });
        }, 10);
      },
      error => {
        if (error.status === 401) {
        } else {
        }
      }
    );
  }

  lyDoNghiValid(): boolean {
    return this.registerForm.controls['ly_do'].invalid;
  }

  registerForm: FormGroup;
  selectedDate: boolean = false;
  update(event: Select2UpdateEvent<any>) {
    this.er_loai_nghi_phep = false
    this.er_np_nam = false
    this.calculateSoNgayNghi()
    this.registerForm.patchValue({ xac_nhan_o: false })
    this.er_xn_o = false
  }
  hasChungTu: boolean = false;
  additionalConfirmation: boolean = false;

  onChungTuChange(event: any) {
    if (!this.hasChungTu) {
      this.selectedFile = null;
      this.additionalConfirmation = false;
    }
  }
  selectChungTu(flag: boolean) {
    this.hasChungTu = flag;
    if (!flag) {
      this.selectedFile = null;
      this.additionalConfirmation = false;
    }
  }
  onSubmit() {
    if (this.isSubmitting) return;
    this.isSubmitting = true;
    this.nghiDenTouched = true
    this.nghiTuTouched = true
    this.ly_do_touched = true;
    this.selectedDate = true;
    this.registerForm.markAllAsTouched();

    if (this.ly_do_nghi == '' || this.ly_do_nghi == null) {
      this.ly_do_str = true
    }
    const leaveType = this.registerForm.get('ly_do')?.value;
    const requiresAdditional = ['O', 'CO', 'TS', 'DS', 'AP', 'S'].includes(leaveType);

    if (requiresAdditional && this.hasChungTu) {
      if (!this.selectedFile && !this.additionalConfirmation) {
        this.hasChungTu = false;
      }
      if (!this.hasChungTu && !this.selectedFile) {
        this.er_xn_o = true;
        this.isSubmitting = false;
        return;
      }
    }
    if (['O', 'CO', 'TS', 'DS', 'AP', 'S'].includes(this.registerForm.get('ly_do')?.value) && !this.hasChungTu && !this.selectedFile) {
      this.isSubmitting = false;
      return
    }


    if (!this.lyDoNghiValid() && !this.nghiDenInvalid && !this.nghiTuInvalid && !this.ly_do_str) {
      var nghi_t7 = 0
      if (this.registerForm.get('nghi_t7')?.value === true) {
        nghi_t7 = 1
      }
      const newNghiPhep: NghiPhepDto = {
        loai_phep_id: 1,
        so_ngay_nghi: Number(this.registerForm.get('so_ngay_nghi')?.value) || 0,
        ky_hieu_ly_do: this.registerForm.get('ly_do')?.value,
        ly_do_nghi_str: this.ly_do_nghi,
        ban_giao: this.registerForm.get('ban_giao')?.value || 'defaut',
        nghi_tu: this.registerForm.get('nghi_tu')?.value,
        nghi_den: this.registerForm.get('nghi_den')?.value,
        nghi_t7: nghi_t7
      };
      const selectedNghiTu = new Date(newNghiPhep.nghi_tu);
      const threeDaysAgo = new Date();
      threeDaysAgo.setDate(threeDaysAgo.getDate() - 3);

      if (selectedNghiTu < threeDaysAgo) {
        alert(this.translate.instant('ER_NGHI_TU_3'));
        this.isSubmitting = false;
        return;
      }
      if (newNghiPhep.so_ngay_nghi <= 0) {
        alert(this.translate.instant('ER_SO_NGAY_NGHI'))
        this.isSubmitting = false;
        return
      }
      this.nghiPhepService.createNghiPhep(newNghiPhep, this.selectedFile ?? undefined).subscribe(
        response => {
          this.ly_do_nghi = '';
          alert(this.translate.instant('GUI_PHIEU_NGHI_TC'));
          this.nghiPhepService.getNotificationCount();
          this.registerForm.reset();
          this.er_np_nam = false;
          this.er_loai_nghi_phep = false;
          this.er_ly_do_nghi = false;
          this.ly_do_str = false;
          this.selectedDate = false;
          this.ly_do_touched = false;
          this.loadUserInfo();
          this.selectedFile = null;
          this.isSubmitting = false;
          this.selectedFile = null;
        },
        error => {
          this.isSubmitting = false;
          if (error.status === 401) {
            localStorage.removeItem('token');
            this.router.navigate(['/login']);
          } else if (error.error.code === 1) {
            this.er_np_nam = true;
            this.er_loai_nghi_phep_str = 'ER_NP_NAM';
          } else if (error.error.code === 9) {
            alert(this.translate.instant('NHAN_VIEN_DA_CO_NGAY_NGHI'));
          } else {
            console.error('Có lỗi xảy ra khi tạo nghỉ phép:', error);
            alert(this.translate.instant('GUI_PHIEU_NGHI_KO_TC'));
          }
        }
      );
      // end
    }
    else {
      this.isSubmitting = false;
      if (!this.registerForm.get('ly_do')?.value) {
        this.er_loai_nghi_phep = true;
        this.er_loai_nghi_phep_str = 'ER_LOAI_PHEP'
      }
      if (!this.registerForm.get('ly_do')?.value) {
        this.er_ly_do_nghi = true;
        this.er_ly_do_nghi_str = 'ER_LY_DO'
      }
    }
  }
}