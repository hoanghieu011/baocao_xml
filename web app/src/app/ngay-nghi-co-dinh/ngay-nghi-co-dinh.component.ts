import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, Validators } from '@angular/forms';
import { NgayNghiCoDinhService } from '../services/ngay-nghi-co-dinh.service';
import { TranslateService, TranslateModule } from '@ngx-translate/core';
import { Select2Module, Select2Data, Select2UpdateEvent } from 'ng-select2-component';

@Component({
  selector: 'app-ngay-nghi-co-dinh',
  standalone: true,
  imports: [CommonModule, FormsModule, Select2Module, TranslateModule],
  templateUrl: './ngay-nghi-co-dinh.component.html',
  styleUrls: ['./ngay-nghi-co-dinh.component.css']
})
export class NgayNghiCoDinhComponent implements OnInit {
  day_selected: any = null;
  editForm: FormGroup;
  originalHolidayType: string = '';
  isSaveProcessing: boolean = false;
  error: string = '';
  isError: boolean = false;
  titleModal: string = 'THONG_TIN_NN';

  selectedYear: number = new Date().getFullYear();
  registerForm: FormGroup;
  years: any[] = [];
  ds_loai_nghi: any[] = []
  holidays: any[] = [];
  newHolidayDate: string = '';
  months: any[] = [];

  constructor(private holidayService: NgayNghiCoDinhService,
    private translate: TranslateService, private fb: FormBuilder,) {
    this.registerForm = this.fb.group({
      loai_nghi_selected: ['', Validators.required],
      newHolidayDate: ['', Validators.required],
    })
    this.editForm = this.fb.group({
      selectedDate: [{ value: '' }],
      loai_nghi_selected: ['', Validators.required]
    });
    this.translate.setDefaultLang('vi');
    const savedLang = localStorage.getItem('language') || 'vi';
    this.translate.use(savedLang);
    this.initLoaiNghi();
    this.translate.onLangChange.subscribe(() => {
      this.initLoaiNghi()
    })
  }

  ngOnInit(): void {
    this.loadYears()
  }
  dayKeys: string[] = ['DAY.0', 'DAY.1', 'DAY.2', 'DAY.3', 'DAY.4', 'DAY.5', 'DAY.6'];

  private initLoaiNghi(): void {
    const selectedValue = this.registerForm.get('loai_nghi_selected')?.value;
    this.ds_loai_nghi = [
      { value: 'NGHI_LE', label: this.translate.instant('NGHI_LE') },
      { value: 'NGHI_KIEM_KE_H', label: this.translate.instant('NGHI_KIEM_KE_H') },
      { value: 'NGHI_PHEP_CO_DINH', label: this.translate.instant('NGHI_PHEP_CO_DINH') },
      { value: 'KHAC', label: this.translate.instant('KHAC') },
    ];
    this.registerForm.patchValue({ loai_nghi_selected: null })
    setTimeout(() => {
      this.registerForm.patchValue({ loai_nghi_selected: selectedValue });
    }, 10);
  }

  loadYears(): void {
    const currentYear = new Date().getFullYear();
    this.holidayService.getDistinctYears().subscribe({
      next: (response) => {
        this.years = response.data.map((y: number) => ({ value: y, label: y.toString() }));
        const nextYear = currentYear + 1;
        const foundNext = this.years.find((year: any) => year.value === nextYear);
        const foundCur = this.years.find((year: any) => year.value === currentYear);
        if (!foundCur) {
          this.years.push({ value: currentYear, label: currentYear.toString() })
          this.years.sort((a, b) => a.value - b.value)
        }
        if (!foundNext) {
          this.years.push({ value: nextYear, label: nextYear.toString() });
          this.years.sort((a, b) => a.value - b.value);
        }
        const foundCurrent = this.years.find((year: any) => year.value === currentYear);
        this.selectedYear = foundCurrent ? currentYear : (this.years.length ? this.years[0].value : currentYear);

        this.loadHolidays();
        this.generateCalendar();
      },
      error: (err) => {
        // console.error("Error fetching distinct years", err);
        for (let i = currentYear - 1; i <= currentYear + 1; i++) {
          this.years.push({ value: i, label: i.toString() });
        }
        this.selectedYear = currentYear;
        alert(this.translate.instant('CO_LOI_XR'))
      }
    });
  }


  loadHolidays(): void {
    this.holidayService.getAllHolidaysByYear(this.selectedYear).subscribe({
      next: (response) => {
        this.holidays = response.data;
        this.generateCalendar();
      },
      error: (err) => {
        // console.error('Error fetching holidays', err);
      }
    });
  }

  update(event: Select2UpdateEvent<any>) {
    this.loadHolidays();
  }

  generateCalendar(): void {
    this.months = [];
    for (let month = 0; month < 12; month++) {
      this.months.push({
        month,
        weeks: this.generateMonthCalendar(this.selectedYear, month)
      });
    }
  }

  generateMonthCalendar(year: number, month: number): any[][] {
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    const weeks = [];
    let week = [];

    for (let i = 0; i < firstDay.getDay(); i++) {
      week.push(null);
    }
    for (let day = 1; day <= lastDay.getDate(); day++) {
      week.push(new Date(year, month, day));
      if (new Date(year, month, day).getDay() === 6) {
        weeks.push(week);
        week = [];
      }
    }
    if (week.length > 0) {
      while (week.length < 7) {
        week.push(null);
      }
      weeks.push(week);
    }
    return weeks;
  }

  isHoliday(date: Date): any {
    if (!date) return false;
    return this.holidays.find(h => {
      const holidayDate = new Date(h.ngay_nghi);
      return holidayDate.getFullYear() === date.getFullYear() &&
        holidayDate.getMonth() === date.getMonth() &&
        holidayDate.getDate() === date.getDate();
    });
  }

  getHolidayDescription(date: Date): string {
    const holiday = this.isHoliday(date);
    return holiday ? this.translate.instant(holiday.mo_ta) : ''
  }

  addHoliday(): void {
    if (!this.newHolidayDate || this.registerForm.invalid) {
      return;
    }
    const holiday = {
      year: new Date(this.newHolidayDate).getFullYear(),
      ngay_nghi: new Date(this.newHolidayDate),
      mo_ta: this.registerForm.get('loai_nghi_selected')?.value
    };
    this.holidayService.createHoliday(holiday).subscribe({
      next: () => {
        this.loadHolidays();
        this.newHolidayDate = '';
        this.registerForm.patchValue({ loai_nghi_selected: null });
      },
      error: (err) => {
        if (err.error.code === 1) {
          alert(this.translate.instant('NGAY_NGHI_DA_TON_TAI'))
        }
      }
    });
  }
  // Khi người dùng click vào ô có holiday trên calendar
  onCalendarDayClick(day: Date): void {
    const holiday = this.isHoliday(day);
    if (holiday) {
      this.openEditModal(holiday);
    }
  }
  // Mở modal chỉnh sửa và khởi tạo giá trị ban đầu
  openEditModal(holiday: any): void {
    this.day_selected = holiday;
    this.editForm.patchValue({
      selectedDate: new Date(holiday.ngay_nghi).toLocaleDateString(),
      loai_nghi_selected: holiday.mo_ta
    });
    this.originalHolidayType = holiday.mo_ta;
  }
  isHolidayTypeChanged(): boolean {
    return this.editForm.get('loai_nghi_selected')?.value !== this.originalHolidayType;
  }

  // Lưu cập nhật nếu có thay đổi
  onSubmitEdit(): void {
    if (!this.isHolidayTypeChanged()) {
      return;
    }
    const newType = this.editForm.get('loai_nghi_selected')?.value;
    this.isSaveProcessing = true;
    this.holidayService.updateHolidayDescription(this.day_selected.id, newType).subscribe({
      next: () => {
        this.isSaveProcessing = false;
        this.loadHolidays();
        this.closeEditModal();
      },
      error: (err) => {
        this.isSaveProcessing = false;
        this.error = 'CO_LOI_XR';
        this.isError = true;
      }
    });
  }
  deleteHoliday(holidayId: number): void {
    console.log(holidayId)
    if (!confirm(this.translate.instant('XAC_NHAN_XOA_NN'))) return;
    this.holidayService.deleteHoliday(holidayId).subscribe({
      next: () => {
        this.loadHolidays();
      },
      error: (err) => {
        alert(this.translate.instant(''))
      }
    });
  }
  deleteSelectedHoliday(): void {
    if (!confirm(this.translate.instant('XAC_NHAN_XOA_NN'))) return;
    this.holidayService.deleteHoliday(this.day_selected.id).subscribe({
      next: () => {
        this.loadHolidays();
        this.closeEditModal();
      },
      error: (err) => {
        this.error = 'CO_LOI_XR';
        this.isError = true;
      }
    });
  }
  // Đóng modal chỉnh sửa
  closeEditModal(): void {
    this.day_selected = null;
    this.editForm.reset();
    this.isError = false;
    this.error = '';
  }
}