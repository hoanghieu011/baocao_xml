import { ChangeDetectorRef, Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconDirective} from '@coreui/icons-angular';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { BorderDirective, SpinnerModule, TableDirective } from '@coreui/angular';
import { ToastModule } from '@coreui/angular';
import { Subject, Subscription, takeUntil } from 'rxjs';
import { ExcelTypeData, ImportDataService } from '../services/import-data.service';
import { FILE_VALIDATE_ERRORS, fileTypeValidator, maxFileSizeValidator, maxFilesValidator } from '../custom/validators/multiple-file-validator';

type ImportDataResult = {
  success: boolean;
  message: string;
}

type FileUploadStatus = 'START' | 'BACKUP' | 'INSERT' | 'ROLLBACK' | 'INIT' | 'COMPLETE'

type FileStatus = {
  preName: string;
  extension: string;
  formattedFileSize: string;
  validateErrors: FILE_VALIDATE_ERRORS[]; 
  uploadStatus: FileUploadStatus;
  uploadResult: string;
  uploadError: string;
}

type UploadType = 'XML' | 'BNND' | 'BN15T' | 'BN_NHAPVIEN' | '';

const EXTENSIONS_BY_TYPE: Record<string, string[]> = {
  XML: ['.xml'],
  BNND: ['.xls', '.xlsx'],
  BN15T: ['.xls', '.xlsx'],
  BN_NHAPVIEN: ['.xls', '.xlsx'],
};

@Component({
  selector: 'app-import-data',
  standalone: true,
  imports: [IconDirective,CommonModule, FormsModule, TableDirective, BorderDirective, ToastModule, ReactiveFormsModule, SpinnerModule],
  templateUrl: './import-data.component.html',
  styleUrls: ['./import-data.component.css']
})
export class ImportDataComponent implements OnDestroy, OnInit {
  private destroy$ = new Subject<void>();
  @ViewChild('file') fileInput!: ElementRef;
  strListFile:string = '';
  listFileStatus: FileStatus[] = []
  fileAccept:string = '';
  isSubmitting: boolean = false;
  isBackuping: boolean = false;
  isInserting: boolean = false;
  formUpload = new FormGroup({
    importType: new FormControl<UploadType>('', Validators.required),
    file: new FormControl<File[]>([], [
      Validators.required,
      maxFileSizeValidator(this.maxSingleFileSizeInBytes),
      maxFilesValidator(this.maxFileCount),
      fileTypeValidator(()=> this.allowedExtensions)
    ])
  });
  constructor(private importDataService: ImportDataService, private cd: ChangeDetectorRef) {
    this.formUpload.controls.importType.valueChanges.subscribe(()=>{
      this.fileInput.nativeElement.value = '';
      this.listFileStatus = [];
      this.formUpload.controls.file.setValue([]);
      this.formUpload.controls.file.markAsPristine();
      this.formUpload.controls.file.markAsUntouched();
      this.formUpload.controls.file.setValidators([
      Validators.required,
      maxFileSizeValidator(this.maxSingleFileSizeInBytes),
      maxFilesValidator(this.maxFileCount),
      fileTypeValidator(()=> this.allowedExtensions)
    ]);
      this.formUpload.controls.file.updateValueAndValidity();
    })

    this.formUpload.statusChanges.subscribe((status)=>{
      console.log('Validation finished. New status:', status);
      if(!this.formUpload.controls.file.value) this.listFileStatus = [];
      else {
        
        this.listFileStatus = this.formUpload.controls.file.value.map( f=> {
          let nameTokens = f.name.split(".");
          let preName = nameTokens.length >= 2 ? nameTokens[0] : '';
          let extension = nameTokens.length >= 2 ? nameTokens[1] : '';
          let validateErrors: FILE_VALIDATE_ERRORS[] = [];
          if(this.formUpload.controls.file.errors?.['fileType'] &&
             this.formUpload.controls.file.errors?.['fileType'].files.filter((vF:string) => vF.includes(f.name)).length > 0) {
              validateErrors.push('fileType');
          }else if(this.formUpload.controls.file.errors?.['maxSize'] && 
             this.formUpload.controls.file.errors?.['maxSize'].files.filter((vF:string) => vF.includes(f.name)).length > 0){
              validateErrors.push('maxSize');
          }
          return {
            extension,
            preName,
            formattedFileSize: this.formatBytes(f.size),
            validateErrors,
            uploadStatus: 'INIT',
            uploadResult: '',
            uploadError: ''
          }
        })
      }
    })
   }
  ngOnInit(): void {
    
  }
  get maxFileCount(): number {
    if(!this.formUpload?.controls) return 0;
    if(this.formUpload.controls.importType.value === 'XML') return 5;
    else if(this.formUpload.controls.importType.value !== '') return 1;
    return 0; 
  }

  get maxTotalFileSizeInBytes(): number {
    if(!this.formUpload?.controls) return 0;
    if(this.formUpload.controls.importType.value === 'XML') return 10 * 1024 * 1024;
    else if(this.formUpload.controls.importType.value !== '') return 1 * 1024 * 1024;
    return 0; 
  }

  get maxSingleFileSizeInBytes(): number {
    if(!this.formUpload?.controls) return 0;
    if(this.formUpload.controls.importType.value === 'XML') return 50 * 1024 * 1024;
    else if(this.formUpload.controls.importType.value !== '') return 1 * 1024 * 1024;
    return 0; 
  }

  get multiple(): boolean {
    if(!this.formUpload?.controls) return false;
    if(this.formUpload.controls.importType.value === 'XML') return true;
    else if(this.formUpload.controls.importType.value !== '') return false;
    return false; 
  }

  get allowedExtensions(): string[] {
    return EXTENSIONS_BY_TYPE[this.formUpload?.controls?.importType.value ?? ''] ?? [];
  }

  get acceptAttr(): string {
    return this.allowedExtensions.join(',');
  }

  getFileExtensionCssClass(file: FileStatus) {
    return `file-extension ${file.validateErrors.includes('fileType')? 'errors' : ''}`
  }

  getFileSizeCssClass(file: FileStatus) {
    return `file-size ${file.validateErrors.includes('maxSize')? 'errors' : ''}`
  }

  onFileSelected(event: Event): void {
    console.log('on file change');
    // Cast the event target safely to access file data
    const input = event.target as HTMLInputElement;

    if (input.files && input.files.length > 0) {
      const files: File[] = Array.from(input.files);
      this.formUpload.controls.file.setValue(files);
    } else {
      this.fileInput.nativeElement.value = '';
      this.formUpload.controls.file.setValue([]);
    }
    this.formUpload.controls.file.markAsTouched();
  }

  formatBytes(bytes: number, decimals: number = 2): string {
    if (bytes === 0) return '0 Bytes';

    const k = 1024;
    const dm = decimals < 0 ? 0 : decimals;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));

    return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
  }

  random(min: number, max: number) {
    return Math.random() * (max - min) + min;
  }

  delay(ms: number) {
    return new Promise(resolve => setTimeout(resolve, ms))
  }

  async onSubmit() {
    console.log(this.formUpload.valid)
    if(this.formUpload.valid) {
      if(this.formUpload.controls.importType.value=='XML') {
        
        }
      else {
        
      }
    }else {
      this.addToast('Thiếu/sai thông tin! Vui lòng kiểm tra lại', 'danger')
    }
  }
  resetForm() {
    this.formUpload.reset();
    this.fileAccept = '';
  }

  

  ngOnDestroy(): void {
    this.destroy$.next(); 
    this.destroy$.complete(); 
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
