import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export type FILE_VALIDATE_ERRORS = 'maxFiles' | 'maxSize' | 'fileType'

export function maxFilesValidator(maxFiles: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const files: File[] | null = control.value;
    if (!files || !files.length) return null;
    return files.length <= maxFiles
      ? null
      : { maxFiles: { max: maxFiles, actual: files.length } };
  };
}


export function maxFileSizeValidator(maxSizeBytes: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const files: File[] | null = control.value;
    if (!files || !files.length) return null;
    const oversized = files.filter(f => f.size > maxSizeBytes);
    return oversized.length === 0
      ? null
      : { maxSize: { max: maxSizeBytes, files: oversized.map(f => {return `${f.name}( ${formatBytes(f.size)} )`}) } };
  };
}



export function fileTypeValidator(getAllowedExtensions: () => string[]): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const files: File[] | null = control.value;
    if (!files || !files.length) return null;

    const allowed = getAllowedExtensions();
    if (!allowed.length) return null;

    const invalid = files.filter(
      f => !allowed.some(ext => f.name.toLowerCase().endsWith(ext.toLowerCase()))
    );

    return invalid.length === 0
      ? null
      : { fileType: { allowed, files: invalid.map(f => f.name) } };
  };
}

 function formatBytes(bytes: number, decimals: number = 2): string {
    if (bytes === 0) return '0 Bytes';

    const k = 1024;
    const dm = decimals < 0 ? 0 : decimals;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));

    return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
  }
