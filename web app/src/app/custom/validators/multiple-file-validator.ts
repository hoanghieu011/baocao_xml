import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export class FileValidators {
  static validateMultipleFiles(config: { maxCount: number; maxTotalSizeBytes: number; maxSingleSizeBytes: number; allowedExtensions: string[] }): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const files: File[] = control.value;

      if (!files || files.length === 0) {
        return null; 
      }

      // 1. Validate số file tôi đa, nếu maxCount < 0 thì không giới hạn số lượng file
      if (config.maxCount >=0 && files.length > config.maxCount) {
        return { maxFileCountExceeded: { max: config.maxCount, actual: files.length } };
      }

      let totalSize = 0;
      let isSingleFileSizeExceeded = false;
      let invalidExtensionFiles ='';
      for (const file of files) {
        totalSize += file.size;
        if(file.size > config.maxSingleSizeBytes) {
          isSingleFileSizeExceeded = true;
          invalidExtensionFiles += file.name;
          invalidExtensionFiles += '\n';
        }
        const extension = file.name.split('.').pop()?.toLowerCase() || '';

        // 2. Validate Allowed Extensions
        if (!config.allowedExtensions.includes(extension)) {
          return { invalidFileExtension: { allowed: config.allowedExtensions, actual: extension, fileName: file.name } };
        }
      }

      // 3. Validate Tổng dung lượng file, nếu maxTotalSizeBytes < 0 thì không giới hạn tổng dung lượng 
       
      if (config.maxTotalSizeBytes >= 0 && totalSize > config.maxTotalSizeBytes) {
        return { totalFileSizeExceeded: { max: config.maxTotalSizeBytes, actual: totalSize } };
      }
      if (isSingleFileSizeExceeded) {
        return { singleFileSizeExceeded: { max: config.maxSingleSizeBytes, fileName: invalidExtensionFiles } };
      }
      return null; 
    };
  }
}
