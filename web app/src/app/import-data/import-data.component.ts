import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { BorderDirective, TableDirective } from '@coreui/angular';
import { ToastModule } from '@coreui/angular';
import { Subscription } from 'rxjs';
import { ImportDataService } from '../services/import-data.service';
import { FileValidators } from '../custom/validators/multiple-file-validator';
type ImportDataResult = {
  success: boolean;
  message: string;
}
@Component({
  selector: 'app-import-data',
  standalone: true,
  imports: [CommonModule, FormsModule, TableDirective, BorderDirective, ToastModule, ReactiveFormsModule],
  templateUrl: './import-data.component.html',
  styleUrls: ['./import-data.component.css']
})
export class ImportDataComponent implements OnDestroy, OnInit {
  maxFileSizeInBytes: number = 10 * 1024 * 1024; // 10MB
  fileAccept:string = '';
  strListFile:string = '';
  multiple: boolean = false;
  isSubmitting: boolean = false;
  isBackuping: boolean = false;
  backUpStepResult: ImportDataResult | null = null;
  isImporting: boolean = false;
  importStepResult: ImportDataResult | null = null;
  //rollback/ commit step result
  isFinalizing: boolean = false;
  finalizingStepResult: ImportDataResult | null = null;
  selectedFiles: File[] = [];
  formUpload = new FormGroup({
    importType: new FormControl('', Validators.required),
    file: new FormControl<File[] | null>(null)
  });
  constructor(private importDataService: ImportDataService) { }
  ngOnInit(): void {
    
  }

  isFile(obj: any): obj is File {
    return obj instanceof File;
  }

  isFileArray(obj: any): obj is File[] {
    return Array.isArray(obj) && obj.every(item => item instanceof File);
  }

  onImportTypeChange(event: any) {
    this.fileAccept = this.getFileAccept();
    this.selectedFiles = [];
    if (this.formUpload.value.importType === 'XML') {
      this.multiple = true;
      this.formUpload.addControl('file', new FormControl(null, [Validators.required, FileValidators.validateMultipleFiles({
        maxCount: -1, // No limit on the number of files
        maxTotalSizeBytes: -1, // No limit on total size
        maxSingleSizeBytes: 10 * 1024 * 1024, // 10MB
        allowedExtensions: ['xml']
      })]));
    } else {
      this.multiple = false;
      this.formUpload.addControl('file', new FormControl(null, [Validators.required, FileValidators.validateMultipleFiles({
        maxCount: 1, // Only one file allowed
        maxTotalSizeBytes: -1, // No limit on total size
        maxSingleSizeBytes: 1 * 1024 * 1024, // 1MB
        allowedExtensions: ['xlsx', 'xls']
      })]));
    }
    this.formUpload.get('file')?.reset();
    
  }
  onFileSelected(event: Event): void {
    // Cast the event target safely to access file data
    const input = event.target as HTMLInputElement;

    if (input.files && input.files.length > 0) {
      const files: File[] = Array.from(input.files);
      this.selectedFiles = files;
      this.formUpload.patchValue({ file: files });
    } else {
      this.formUpload.patchValue({ file: null });
      this.selectedFiles = [];
    }
    let control = this.formUpload.get('file');
    control?.markAsDirty();
    control?.markAsTouched();
    control?.updateValueAndValidity({emitEvent: true});
    
  }

  formatBytes(bytes: number, decimals: number = 2): string {
    if (bytes === 0) return '0 Bytes';

    const k = 1024;
    const dm = decimals < 0 ? 0 : decimals;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));

    return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
  }
  getFileAccept(): string {
    switch (this.formUpload.value.importType) {
      case 'XML':
        return '.xml';
      case 'BNND':
        return '.xlsx,.xls';
      case 'BN15T':
        return '.xlsx,.xls';
      case 'BN_NHAPVIEN':
        return '.xlsx,.xls';
      default:
        return '';
    }
  }

  onSubmit() {
    // simulate the import process with a delay
    console.log('Form submitted with values:', this.formUpload.value);
    if(this.formUpload.get('file')?.errors) {
      const errors = this.formUpload.get('file')?.errors;
      console.log('Validation errors:', errors);
    }
    if (!this.formUpload.value.file || !this.formUpload.value.importType) {
      this.addToast('Vui lòng chọn loại dữ liệu và file để import.', 'danger');
      return;
    }
    this.isSubmitting = true;
    this.isBackuping = true;
    setTimeout(() => {
      
      this.backUpStepResult = { success: true, message: 'Backup dữ liệu thành công!' };
      this.isImporting = true;
    }, 2000);
    setTimeout(() => {
      this.isBackuping = false;
      this.importStepResult = { success: true, message: 'Import dữ liệu thành công!' };
      this.isFinalizing = true;
    }, 2000);
    setTimeout(() => {
      this.isImporting = false;
      this.finalizingStepResult = { success: true, message: 'Commit dữ liệu thành công!' };
      this.isFinalizing = false;
      this.isSubmitting = false;
    }, 2000);
  }
  resetForm() {
    this.formUpload.reset();
    this.selectedFiles = [];
    this.multiple = false;
    this.fileAccept = '';
    this.backUpStepResult = null;
    this.importStepResult = null;
    this.finalizingStepResult = null;
  }
  ngOnDestroy(): void {

  }

  toasts: any[] = [];

  addToast(message: string, color: string = 'danger') {
    this.toasts.push({
      message,
      color,
      visible: true
    });
    setTimeout(() => {
      this.toasts.shift();
    }, 3000);
  }
  
}
