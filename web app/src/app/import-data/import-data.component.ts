import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import {
  FormControl,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  ValidatorFn,
  Validators
} from '@angular/forms';
import { BorderDirective, SpinnerModule, TableDirective, ToastModule } from '@coreui/angular';
import { IconDirective } from '@coreui/icons-angular';
import { saveAs } from 'file-saver';
import { firstValueFrom, Subject, takeUntil } from 'rxjs';
import {
  FILE_VALIDATE_ERRORS,
  fileTypeValidator,
  maxFileSizeValidator,
  maxFilesValidator
} from '../custom/validators/multiple-file-validator';
import {
  ExcelTypeData,
  ImportDataService,
  ImportExcelResponse,
  ImportExcelTableInfo,
  ImportXMLResponse
} from '../services/import-data.service';

type FileUploadStatus = 'BACKUP' | 'INSERT' | 'ROLLBACK' | 'INIT' | 'COMPLETE';

type FileStatus = {
  fileName: string;
  preName: string;
  extension: string;
  formattedFileSize: string;
  validateErrors: FILE_VALIDATE_ERRORS[];
  uploadStatus: FileUploadStatus;
  uploadResult: string;
  uploadError: string;
};

type UploadType = 'XML' | ExcelTypeData | '';

type ToastMessage = {
  message: string;
  color: string;
  visible: boolean;
};

const EXTENSIONS_BY_TYPE: Record<Exclude<UploadType, ''>, string[]> = {
  XML: ['.xml'],
  BNND: ['.xls', '.xlsx'],
  BN15T: ['.xls', '.xlsx'],
  BN_NHAPVIEN: ['.xls', '.xlsx']
};

const DEFAULT_EXCEL_TYPES: ImportExcelTableInfo[] = [
  { code: 'BNND', name: 'Bệnh nhân nhân dân' },
  { code: 'BN15T', name: 'Bệnh nhân 15T' },
  { code: 'BN_NHAPVIEN', name: 'Bệnh nhân nhập viện' }
];

@Component({
  selector: 'app-import-data',
  standalone: true,
  imports: [
    IconDirective,
    CommonModule,
    FormsModule,
    TableDirective,
    BorderDirective,
    ToastModule,
    ReactiveFormsModule,
    SpinnerModule
  ],
  templateUrl: './import-data.component.html',
  styleUrls: ['./import-data.component.css']
})
export class ImportDataComponent implements OnDestroy, OnInit {
  private destroy$ = new Subject<void>();

  @ViewChild('file') fileInput?: ElementRef<HTMLInputElement>;

  listFileStatus: FileStatus[] = [];
  excelTypes: ImportExcelTableInfo[] = DEFAULT_EXCEL_TYPES;
  isSubmitting = false;
  isDownloadingTemplate = false;
  isDeleting = false;
  toasts: ToastMessage[] = [];

  formUpload = new FormGroup({
    importType: new FormControl<UploadType>('', {
      nonNullable: true,
      validators: Validators.required
    }),
    file: new FormControl<File[]>([], {
      nonNullable: true,
      validators: this.createFileValidators()
    })
  });

  formDelete = new FormGroup({
    thang: new FormControl<number | null>(new Date().getMonth() + 1, [
      Validators.required,
      Validators.min(1),
      Validators.max(12)
    ]),
    nam: new FormControl<number | null>(new Date().getFullYear(), [
      Validators.required,
      Validators.min(2000),
      Validators.max(9999)
    ])
  });

  constructor(private importDataService: ImportDataService) {
    this.formUpload.controls.importType.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.clearSelectedFiles();
        this.formUpload.controls.file.setValidators(this.createFileValidators());
        this.formUpload.controls.file.updateValueAndValidity();
      });
  }

  ngOnInit(): void {
    this.importDataService.getImportExcelTablesInfo()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          if (data?.length) {
            this.excelTypes = data
              .filter((item) => this.isExcelType(item.code))
              .map((item) => ({
                ...item,
                name: DEFAULT_EXCEL_TYPES.find((type) => type.code === item.code)?.name || item.name
              }));
          }
        },
        error: () => {
          this.excelTypes = DEFAULT_EXCEL_TYPES;
        }
      });
  }

  get maxFileCount(): number {
    if (!this.formUpload?.controls) {
      return 0;
    }
    if (this.formUpload.controls.importType.value === 'XML') {
      return 1;
    }
    return this.formUpload.controls.importType.value ? 1 : 0;
  }

  get maxSingleFileSizeInBytes(): number {
    if (!this.formUpload?.controls) {
      return 0;
    }
    if (this.formUpload.controls.importType.value === 'XML') {
      return 50 * 1024 * 1024;
    }
    return this.formUpload.controls.importType.value ? 1024 * 1024 : 0;
  }

  get multiple(): boolean {
    //return this.formUpload.controls.importType.value === 'XML';
    return false;
  }

  get isExcelSelected(): boolean {
    return this.isExcelType(this.formUpload.controls.importType.value);
  }

  get allowedExtensions(): string[] {
    if (!this.formUpload?.controls) {
      return [];
    }
    const importType = this.formUpload.controls.importType.value;
    return importType ? EXTENSIONS_BY_TYPE[importType] : [];
  }

  get acceptAttr(): string {
    return this.allowedExtensions.join(',');
  }

  getFileExtensionCssClass(file: FileStatus): string {
    return `file-extension ${file.validateErrors.includes('fileType') ? 'errors' : ''}`;
  }

  getFileSizeCssClass(file: FileStatus): string {
    return `file-size ${file.validateErrors.includes('maxSize') ? 'errors' : ''}`;
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = input.files ? Array.from(input.files) : [];

    this.formUpload.controls.file.setValue(files);
    this.formUpload.controls.file.markAsTouched();
    this.formUpload.controls.file.updateValueAndValidity();
    this.listFileStatus = this.createFileStatuses(files);
  }

  async onSubmit(): Promise<void> {
    if (this.formUpload.invalid) {
      this.formUpload.markAllAsTouched();
      this.addToast('Thiếu hoặc sai thông tin. Vui lòng kiểm tra lại.', 'danger');
      return;
    }

    const importType = this.formUpload.controls.importType.value;
    const files = this.formUpload.controls.file.value;
    this.isSubmitting = true;

    try {
      if (importType === 'XML') {
        await this.submitXmlFiles(files);
      } else if (this.isExcelType(importType) && files[0]) {
        await this.submitExcelFile(files[0], importType);
      }
    } finally {
      this.isSubmitting = false;
    }
  }

  async downloadExcelTemplate(): Promise<void> {
    const importType = this.formUpload.controls.importType.value;
    if (!this.isExcelType(importType)) {
      this.addToast('Vui lòng chọn loại dữ liệu Excel.', 'warning');
      return;
    }

    this.isDownloadingTemplate = true;
    try {
      const blob = await firstValueFrom(
        this.importDataService.getImportExcelTemplate(importType)
      );
      saveAs(blob, `MAU_IMPORT_${importType}.xlsx`);
      this.addToast('Tải file mẫu thành công.', 'success');
    } catch (error: unknown) {
      this.addToast(this.getErrorMessage(error, 'Không thể tải file mẫu.'), 'danger');
    } finally {
      this.isDownloadingTemplate = false;
    }
  }

  deleteHospitalDataByMonth(): void {
    if (this.formDelete.invalid) {
      this.formDelete.markAllAsTouched();
      this.addToast('Tháng hoặc năm không hợp lệ.', 'danger');
      return;
    }

    const thang = this.formDelete.controls.thang.value;
    const nam = this.formDelete.controls.nam.value;
    if (thang === null || nam === null) {
      return;
    }

    const confirmed = window.confirm(
      `Bạn chắc chắn muốn xóa dữ liệu tháng ${thang}/${nam}? Thao tác này không thể hoàn tác.`
    );
    if (!confirmed) {
      return;
    }

    this.isDeleting = true;
    this.importDataService.deleteHospitalDataByMonth(thang, nam)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (message) => {
          this.addToast(message || `Đã xóa dữ liệu tháng ${thang}/${nam}.`, 'success');
          this.isDeleting = false;
        },
        error: (error: unknown) => {
          this.addToast(this.getErrorMessage(error, 'Xóa dữ liệu không thành công.'), 'danger');
          this.isDeleting = false;
        }
      });
  }

  resetForm(): void {
    this.formUpload.reset({ importType: '', file: [] });
    this.clearSelectedFiles();
  }

  formatBytes(bytes: number, decimals = 2): string {
    if (bytes === 0) {
      return '0 Bytes';
    }

    const unit = 1024;
    const precision = decimals < 0 ? 0 : decimals;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const index = Math.floor(Math.log(bytes) / Math.log(unit));
    return `${parseFloat((bytes / Math.pow(unit, index)).toFixed(precision))} ${sizes[index]}`;
  }

  addToast(message: string, color = 'danger'): void {
    const toast: ToastMessage = { message, color, visible: true };
    this.toasts.push(toast);
    setTimeout(() => {
      this.toasts = this.toasts.filter((item) => item !== toast);
    }, 3000);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private createFileValidators(): ValidatorFn[] {
    return [
      Validators.required,
      maxFileSizeValidator(this.maxSingleFileSizeInBytes),
      maxFilesValidator(this.maxFileCount),
      fileTypeValidator(() => this.allowedExtensions)
    ];
  }

  private createFileStatuses(files: File[]): FileStatus[] {
    const fileErrors = this.formUpload.controls.file.errors;
    const invalidTypes = (fileErrors?.['fileType']?.files ?? []) as string[];
    const invalidSizes = (fileErrors?.['maxSize']?.files ?? []) as string[];

    return files.map((file) => {
      const lastDotIndex = file.name.lastIndexOf('.');
      const validateErrors: FILE_VALIDATE_ERRORS[] = [];

      if (invalidTypes.some((item) => item.includes(file.name))) {
        validateErrors.push('fileType');
      }
      if (invalidSizes.some((item) => item.includes(file.name))) {
        validateErrors.push('maxSize');
      }

      return {
        fileName: file.name,
        preName: lastDotIndex > 0 ? file.name.slice(0, lastDotIndex) : file.name,
        extension: lastDotIndex > 0 ? file.name.slice(lastDotIndex + 1) : '',
        formattedFileSize: this.formatBytes(file.size),
        validateErrors,
        uploadStatus: 'INIT',
        uploadResult: '',
        uploadError: ''
      };
    });
  }

  private async submitXmlFiles(files: File[]): Promise<void> {
    let successCount = 0;

    for (let index = 0; index < files.length; index++) {
      this.updateFileStatus(index, { uploadStatus: 'INSERT', uploadError: '', uploadResult: '' });
      try {
        const response = await firstValueFrom(
          this.importDataService.importXMLData(files[index])
        );
        this.completeXmlUpload(index, response);
        if (!response.isError) {
          successCount++;
        }
      } catch (error: unknown) {
        this.updateFileStatus(index, {
          uploadStatus: 'COMPLETE',
          uploadError: this.getErrorMessage(error, 'Import XML không thành công.'),
          uploadResult: ''
        });
      }
    }

    const failedCount = files.length - successCount;
    const color = failedCount === 0 ? 'success' : successCount === 0 ? 'danger' : 'warning';
    this.addToast(
      `Hoàn tất ${files.length} file: ${successCount} thành công, ${failedCount} thất bại.`,
      color
    );
  }

  private async submitExcelFile(file: File, importType: ExcelTypeData): Promise<void> {
    this.updateFileStatus(0, { uploadStatus: 'BACKUP', uploadError: '', uploadResult: '' });

    try {
      const request = firstValueFrom(
        this.importDataService.importExcelData(file, importType)
      );
      await this.delay(300);
      this.updateFileStatus(0, { uploadStatus: 'INSERT' });
      const response = await request;
      this.completeExcelUpload(0, response);
    } catch (error: unknown) {
      const message = this.getErrorMessage(error, 'Import Excel không thành công.');
      this.updateFileStatus(0, {
        uploadStatus: 'COMPLETE',
        uploadError: message,
        uploadResult: ''
      });
      this.addToast(message, 'danger');
    }
  }

  private completeXmlUpload(index: number, response: ImportXMLResponse): void {
    if (response.isError) {
      this.updateFileStatus(index, {
        uploadStatus: 'COMPLETE',
        uploadError: response.message || 'Import XML không thành công.',
        uploadResult: ''
      });
      return;
    }

    const result = [
      response.message,
      `XML1: ${response.countXML1 ?? 0}`,
      `XML2: ${response.countXML2 ?? 0}`,
      `XML3: ${response.countXML3 ?? 0}`
    ].filter(Boolean).join(' | ');

    this.updateFileStatus(index, {
      uploadStatus: 'COMPLETE',
      uploadResult: result,
      uploadError: ''
    });
  }

  private completeExcelUpload(index: number, response: ImportExcelResponse): void {
    if (response.isError) {
      this.updateFileStatus(index, {
        uploadStatus: 'COMPLETE',
        uploadError: response.message || 'Import Excel không thành công.',
        uploadResult: ''
      });
      this.addToast(response.message || 'Import Excel không thành công.', 'danger');
      return;
    }

    const result = [
      response.message,
      response.table ? `Bảng: ${response.table}` : '',
      `Số dòng: ${response.affectedRows ?? 0}`
    ].filter(Boolean).join(' | ');

    this.updateFileStatus(index, {
      uploadStatus: 'COMPLETE',
      uploadResult: result,
      uploadError: ''
    });
    this.addToast('Import Excel thành công.', 'success');
  }

  private updateFileStatus(index: number, changes: Partial<FileStatus>): void {
    this.listFileStatus = this.listFileStatus.map((file, fileIndex) =>
      fileIndex === index ? { ...file, ...changes } : file
    );
  }

  private clearSelectedFiles(): void {
    if (this.fileInput) {
      this.fileInput.nativeElement.value = '';
    }
    this.listFileStatus = [];
    this.formUpload.controls.file.setValue([]);
    this.formUpload.controls.file.markAsPristine();
    this.formUpload.controls.file.markAsUntouched();
  }

  private isExcelType(importType: UploadType): importType is ExcelTypeData {
    return importType === 'BNND' || importType === 'BN15T' || importType === 'BN_NHAPVIEN';
  }

  private getErrorMessage(error: unknown, fallback: string): string {
    if (!(error instanceof HttpErrorResponse)) {
      return fallback;
    }

    if (typeof error.error === 'string' && error.error.trim()) {
      return error.error;
    }

    const body = error.error as { message?: string; detail?: string } | null;
    return body?.message || body?.detail || error.message || fallback;
  }

  private delay(ms: number): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }
}
