import { HisBrowserStore, HisImportLog, HisLocalFile } from './his-browser-store';
import { importLogCsv } from './his-import-log';
import { createXmlZip, decodeCsv, decodeHisXml, DecodeReport } from './his-xml-utils';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, EventEmitter, Input, OnDestroy, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { saveAs } from 'file-saver';
import { firstValueFrom } from 'rxjs';
import { HisFetchRequest, HisRecord, ImportDataService } from '../services/import-data.service';

type MonthChoice = { key: string; year: number; month: number; selected: boolean; status: string };

@Component({
  selector: 'app-his-import',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './his-import.component.html'
})
export class HisImportComponent implements OnDestroy {
  @Input() disabled = false;
  @Output() busyChange = new EventEmitter<boolean>();
  request: HisFetchRequest = {
    year: new Date().getFullYear(), month: new Date().getMonth() + 1,
    token: '', cookie: '', cskcb: '35046', locTheo: '2',
    qd3176: 'auto', page: 1, rows: 500, maLks: []
  };
  fromMonth = `${this.request.year}-${String(this.request.month).padStart(2, '0')}`;
  toMonth = this.fromMonth;
  months: MonthChoice[] = [];
  files: HisLocalFile[] = [];
  busy = false;
  stopRequested = false;
  sessionValid = false;
  sessionMessage = 'Nhập token, cookie và mã CSKCB, sau đó kiểm tra phiên để mở các chức năng.';
  message = '';
  error = '';
  rangeError = '';
  private destroyed = false;
  private store = new HisBrowserStore();
  private scope = '';
  previewText = '';
  previewName = '';
  previewPage = 0;
  readonly previewPageSize = 20000;
  get previewPages(): number { return Math.ceil(this.previewText.length / this.previewPageSize); }
  get previewContent(): string { return this.previewText.slice(this.previewPage * this.previewPageSize, (this.previewPage + 1) * this.previewPageSize); }

  constructor(private service: ImportDataService) { this.updateMonths(); }

  get selectedMonths(): MonthChoice[] { return this.months.filter(m => m.selected); }
  get selectedFiles(): HisLocalFile[] {
    return this.files.filter(file => this.selectedMonths.some(m => m.year === file.year && m.month === file.month)
      && file.cskcb === this.request.cskcb && file.locTheo === this.request.locTheo);
  }
  get pendingFiles(): HisLocalFile[] { return this.selectedFiles.filter(file => !file.importedAt && !file.decode?.errors); }
  get canOperate(): boolean { return this.sessionValid && !!this.scope && !this.busy && !this.disabled && !this.rangeError; }

  credentialsChanged(): void {
    this.sessionValid = false;
    this.sessionMessage = 'Thông tin phiên đã thay đổi. Vui lòng kiểm tra lại.';
    this.error = '';
  }

  updateMonths(): void {
    this.months = [];
    this.rangeError = '';
    const parse = (value: string): number | null => {
      if (!/^\d{4}-\d{2}$/.test(value)) return null;
      const [year, month] = value.split('-').map(Number);
      return year >= 2000 && year <= 9999 && month >= 1 && month <= 12 ? year * 12 + month - 1 : null;
    };
    const from = parse(this.fromMonth);
    const to = parse(this.toMonth);
    if (from === null || to === null || from > to || to - from >= 120) {
      this.rangeError = 'Chọn khoảng tháng hợp lệ, tối đa 120 tháng mỗi lượt.';
      return;
    }
    for (let value = from; value <= to; value++) {
      const year = Math.floor(value / 12);
      const month = value % 12 + 1;
      this.months.push({ key: `${year}-${String(month).padStart(2, '0')}`, year, month, selected: true, status: '' });
    }
  }

  filesForMonth(month: MonthChoice): HisLocalFile[] {
    return this.files.filter(f => f.year === month.year && f.month === month.month && f.cskcb === this.request.cskcb && f.locTheo === this.request.locTheo);
  }
  importedForMonth(month: MonthChoice): number { return this.filesForMonth(month).filter(f => !!f.importedAt).length; }
  selectAll(selected: boolean): void { this.months.forEach(m => m.selected = selected); }
  private setBusy(value: boolean): void {
    this.busy = value;
    if (!this.destroyed) this.busyChange.emit(value);
  }

  async checkSession(): Promise<void> {
    if (this.busy || this.disabled || !this.request.token.trim() || !this.request.cookie.trim()
      || !/^\d{1,10}$/.test(this.request.cskcb) || this.rangeError || !this.selectedMonths.length) return;
    this.setBusy(true);
    this.sessionValid = false;
    this.sessionMessage = 'Đang kiểm tra phiên đăng nhập HIS…';
    this.error = '';
    try {
      const status = await firstValueFrom(this.service.checkHisSession({ ...this.request,
        year: this.selectedMonths[0]?.year ?? this.request.year,
        month: this.selectedMonths[0]?.month ?? this.request.month }));
      if (this.destroyed) return;
      this.sessionValid = status.isValid;
      this.sessionMessage = status.message;
      if (status.isValid) {
        const { scope } = await firstValueFrom(this.service.getHisBrowserScope());
        if (this.destroyed) return;
        this.scope = scope;
        this.files = await this.store.list(scope);
      }
    } catch (error) {
      this.sessionValid = false;
      this.sessionMessage = 'Không thể hoàn tất kiểm tra phiên.';
      this.error = this.errorMessage(error);
    } finally { this.setBusy(false); }
  }

  async refreshFiles(): Promise<void> {
    if (!this.canOperate) return;
    this.setBusy(true);
    this.error = '';
    try { this.files = await this.store.list(this.scope); }
    catch (error) { this.error = this.errorMessage(error); }
    finally { this.setBusy(false); }
  }

  async fetchXml(): Promise<void> {
    if (!this.canOperate || !this.selectedMonths.length) return;
    this.setBusy(true);
    this.stopRequested = false;
    this.error = '';
    const months = this.selectedMonths;
    const base = { ...this.request, rows: 500 };
    try {
      for (const month of months) {
        if (this.stopRequested) break;
        const request = { ...base, year: month.year, month: month.month };
        const records = new Map<string, HisRecord>();
        let total: number | undefined;
        month.status = 'Đang lấy danh sách';
        for (let page = 1; page <= 100000; page++) {
          if (this.stopRequested) break;
          const result = await firstValueFrom(this.service.fetchHisRecords({ ...request, page }));
          if (total !== undefined && total !== result.total) throw new Error(`Tổng hồ sơ tháng ${month.key} thay đổi. Hãy fetch lại.`);
          total = result.total;
          const before = records.size;
          result.records.forEach(record => records.set(record.maLk, record));
          month.status = `Lấy danh sách ${records.size}/${total}`;
          if (page * 500 >= total) break;
          if (records.size === before) throw new Error(`HIS trả trang rỗng/trùng trong tháng ${month.key}.`);
          await this.delay(400);
        }
        if (this.stopRequested) { month.status = 'Đã dừng; chưa fetch hết'; break; }
        if (records.size !== total) throw new Error(`Tháng ${month.key}: chỉ lấy được ${records.size}/${total} mã duy nhất.`);
        if (!records.size) {
          month.status = 'Không có hồ sơ. Nếu bất thường, kiểm tra lại phiên HIS.';
          continue;
        }
        const groups = new Map<string, string[]>();
        for (const record of records.values()) {
          const ids = groups.get(record.qd3176) || [];
          ids.push(record.maLk);
          groups.set(record.qd3176, ids);
        }
        let fetched = 0;
        for (const [qd, ids] of groups) {
          for (let index = 0; index < ids.length && !this.stopRequested; index += 500) {
            const maLks = ids.slice(index, index + 500);
            month.status = `Đang fetch XML: ${fetched}/${records.size} hồ sơ`;
            const xml = await firstValueFrom(this.service.fetchHisXmlBatch({ ...request, maLks, qd3176: qd }));
            const file = await this.store.save(this.scope, request, xml, maLks.length);
            this.files = [...this.files.filter(f => f.id !== file.id), file];
            fetched += maLks.length;
            await this.delay(1000);
          }
        }
        month.status = `${fetched === records.size ? 'Đã fetch đủ' : 'Đã dừng, mới fetch'} ${fetched}/${records.size} hồ sơ.`;
      }
      this.message = `${this.stopRequested ? 'Đã dừng fetch.' : 'Hoàn tất lượt fetch.'} XML đã lưu trong trình duyệt này, chưa import vào database.`;
    } catch (error) {
      this.error = this.errorMessage(error);
      this.message = 'Fetch chưa hoàn tất. Các file đã lưu vẫn được giữ; có thể fetch lại để tiếp tục.';
    } finally { this.setBusy(false); }
  }

  async importXml(): Promise<void> {
    if (!this.canOperate || !this.pendingFiles.length) return;
    const files = [...this.pendingFiles];
    this.setBusy(true);
    this.stopRequested = false;
    this.error = '';
    let completed = 0;
    const logs: { fileName: string; log: HisImportLog }[] = [];
    try {
      for (const file of files) {
        if (this.stopRequested) break;
        this.message = `Đang import ${completed + 1}/${files.length}: ${file.fileName}`;
        let log: HisImportLog;
        try {
          const raw = await this.store.read(this.scope, file.id);
          const result = await firstValueFrom(this.service.importXMLData(new File([raw], file.fileName, { type: 'application/xml' })));
          log = { at: new Date().toISOString(), status: result.isError ? 'error' : 'success',
            message: result.message || (result.isError ? 'Import thất bại.' : 'Import thành công, không có lỗi được API báo về.'),
            countXML1: result.countXML1, countXML2: result.countXML2, countXML3: result.countXML3 };
        } catch (error) {
          log = { at: new Date().toISOString(), status: 'error', message: this.errorMessage(error) };
        }
        // Giữ log trong bộ nhớ để vẫn tải được nếu IndexedDB không ghi thành công.
        file.importLog = log;
        logs.push({ fileName: file.fileName, log });
        if (log.status === 'success') file.importedAt = log.at;
        try { await this.store.saveImportLog(this.scope, file.id, log); }
        catch (error) {
          log.message += ` Không lưu được kết quả vào trình duyệt: ${this.errorMessage(error)}`;
          throw new Error(log.message);
        }
        if (log.status === 'error') throw new Error(log.message);
        completed++;
        const month = this.months.find(m => m.year === file.year && m.month === file.month);
        if (month) month.status = log.message;
      }
      this.message = `${this.stopRequested ? 'Đã dừng.' : 'Hoàn tất.'} Import ${completed}/${files.length} file đã chọn.`;
    } catch (error) {
      this.error = this.errorMessage(error);
      this.message = `Đã import ${completed}/${files.length} file. Dừng tại file lỗi; lần tiếp theo bỏ qua các file đã thành công.`;
    } finally {
      this.setBusy(false);
      if (logs.length) {
        try { this.saveImportLogDownload(logs); }
        catch { this.error += ' Không tự tải được log. Bấm Log lỗi để tải lại.'; }
      }
    }
  }

  private saveImportLogDownload(logs: { fileName: string; log: HisImportLog }[]): void {
    saveAs(importLogCsv(logs), `HIS_import_log_${new Date().toISOString().replace(/[:.]/g, '-')}.csv`);
  }

  async download(file: HisLocalFile): Promise<void> {
    if (!this.canOperate) return;
    this.setBusy(true);
    try { saveAs(await this.store.read(this.scope, file.id), file.fileName); }
    catch (error) { this.error = this.errorMessage(error); }
    finally { this.setBusy(false); }
  }

  async addLocalFiles(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files || []);
    input.value = '';
    if (!this.canOperate || this.selectedMonths.length !== 1 || !files.length) return;
    this.setBusy(true);
    this.stopRequested = false;
    this.error = '';
    try {
      const month = this.selectedMonths[0];
      for (const file of files) {
        if (this.stopRequested) break;
        if (!file.name.toLowerCase().endsWith('.xml')) throw new Error('Chỉ chấp nhận file XML.');
        await this.store.save(this.scope, { year: month.year, month: month.month,
          cskcb: this.request.cskcb, locTheo: this.request.locTheo }, file, 0, file.name);
      }
      this.files = await this.store.list(this.scope);
      this.message = 'Đã thêm XML từ máy vào trình duyệt. Bấm Decode để kiểm tra số hồ sơ và lỗi; chưa gửi file lên server.';
    } catch (error) { this.error = this.errorMessage(error); }
    finally { this.setBusy(false); }
  }

  async decodeFiles(file?: HisLocalFile): Promise<void> {
    if (!this.canOperate) return;
    const files = file ? [file] : [...this.selectedFiles];
    this.setBusy(true);
    this.stopRequested = false;
    this.error = '';
    let done = 0, errors = 0;
    try {
      for (const item of files) {
        if (this.stopRequested) break;
        this.message = `Đang decode ${done + 1}/${files.length}: ${item.fileName}`;
        const result = await decodeHisXml(await this.store.read(this.scope, item.id));
        await this.store.saveDecoded(this.scope, item.id, result.decoded, result.report);
        errors += result.report.errors;
        done++;
        await this.delay(0);
      }
      this.files = await this.store.list(this.scope);
      this.message = `Đã decode ${done}/${files.length} file; ${errors} lỗi/cảnh báo. XML gốc được giữ nguyên; chưa import dữ liệu.`;
    } catch (error) { this.error = this.errorMessage(error); }
    finally { this.setBusy(false); }
  }

  private async reports(files: HisLocalFile[]): Promise<{ fileName: string; report: DecodeReport }[]> {
    const reports: { fileName: string; report: DecodeReport }[] = [];
    for (const file of files.filter(f => !!f.decode)) {
      reports.push({ fileName: file.fileName, report: JSON.parse(await (await this.store.read(this.scope, file.id, 'report')).text()) });
    }
    return reports;
  }

  async downloadDecoded(file: HisLocalFile): Promise<void> {
    if (!this.canOperate || !file.decode?.hasOutput) return;
    this.setBusy(true);
    try { saveAs(await this.store.read(this.scope, file.id, 'decoded'), file.fileName.replace(/\.xml$/i, '_decoded.xml')); }
    catch (error) { this.error = this.errorMessage(error); }
    finally { this.setBusy(false); }
  }

  async downloadLog(file?: HisLocalFile): Promise<void> {
    if (!this.canOperate) return;
    this.setBusy(true);
    try {
      if (file?.importLog) this.saveImportLogDownload([{ fileName: file.fileName, log: file.importLog }]);
      else saveAs(decodeCsv(await this.reports(file ? [file] : this.selectedFiles)), 'decode_log.csv');
    }
    catch (error) { this.error = this.errorMessage(error); }
    finally { this.setBusy(false); }
  }

  async downloadZip(): Promise<void> {
    if (!this.canOperate || !this.selectedFiles.length) return;
    this.setBusy(true);
    this.error = '';
    try {
      const entries: { name: string; blob: Blob }[] = [];
      let total = 0;
      for (const file of this.selectedFiles) {
        const folder = `${file.year}${String(file.month).padStart(2, '0')}`;
        const name = `${file.id.slice(0, 12)}_${file.fileName.replace(/[^\p{L}\p{N}._-]/gu, '_')}`;
        entries.push({ name: `raw/${folder}/${name}`, blob: await this.store.read(this.scope, file.id) });
        if (file.decode?.hasOutput) entries.push({ name: `decoded/${folder}/${name}`, blob: await this.store.read(this.scope, file.id, 'decoded') });
        total += file.size;
        if (total > 200 * 1024 * 1024) throw new Error('ZIP vượt 200 MB. Hãy chọn ít tháng hơn.');
      }
      entries.push({ name: 'decode_log.csv', blob: decodeCsv(await this.reports(this.selectedFiles)) });
      entries.push({ name: 'import_log.csv', blob: importLogCsv(this.selectedFiles
        .filter(file => !!file.importLog).map(file => ({ fileName: file.fileName, log: file.importLog! }))) });
      saveAs(await createXmlZip(entries), 'HIS_XML.zip');
    } catch (error) { this.error = this.errorMessage(error); }
    finally { this.setBusy(false); }
  }

  async preview(file: HisLocalFile): Promise<void> {
    if (!this.canOperate || !file.decode?.hasOutput) return;
    this.setBusy(true);
    try {
      const blob = await this.store.read(this.scope, file.id, 'decoded');
      this.previewText = await blob.slice(0, 200000).text();
      this.previewPage = 0;
      this.previewName = `${file.fileName}${blob.size > 200000 ? ' (xem trước 200 KB đầu; tải file để xem đầy đủ)' : ''}`;
    } catch (error) { this.error = this.errorMessage(error); }
    finally { this.setBusy(false); }
  }

  async resetStore(): Promise<void> {
    if (this.busy || this.disabled || !window.confirm('Reset kho dữ liệu HIS? Toàn bộ XML gốc, bản decode, log và trạng thái import của mọi tài khoản/bệnh viện trong trình duyệt này sẽ bị xóa, kể cả các tháng không được chọn. Không thể hoàn tác. Dữ liệu trên server không bị xóa.')) return;
    this.setBusy(true);
    this.error = '';
    this.message = '';
    try {
      await this.store.reset();
      this.files = [];
      this.previewText = '';
      this.previewName = '';
      this.previewPage = 0;
      this.stopRequested = false;
      this.months.forEach(month => { month.status = ''; });
      this.message = 'Đã reset toàn bộ kho dữ liệu HIS trong trình duyệt. Dữ liệu đã import trên server được giữ nguyên.';
    } catch (error) { this.error = this.errorMessage(error); }
    finally { this.setBusy(false); }
  }

  async removeFile(file: HisLocalFile): Promise<void> {
    if (!this.canOperate || !window.confirm(`Xóa ${file.fileName} và bản decode khỏi trình duyệt này? Dữ liệu đã import vào database không bị xóa.`)) return;
    this.setBusy(true);
    try {
      await this.store.remove(this.scope, file.id);
      this.files = this.files.filter(item => item.id !== file.id);
      this.previewText = '';
    } catch (error) { this.error = this.errorMessage(error); }
    finally { this.setBusy(false); }
  }

  private errorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) return (typeof error.error === 'string' ? error.error : error.error?.message) || (error.status === 0
      ? 'Mất kết nối API. Kiểm tra lại danh sách file trước khi thử tiếp.' : 'Yêu cầu thất bại. Kiểm tra phiên và thông tin HIS.');
    return error instanceof Error ? error.message : 'Không thể xử lý dữ liệu HIS.';
  }
  private delay(ms: number): Promise<void> { return new Promise(resolve => setTimeout(resolve, ms)); }
  ngOnDestroy(): void {
    this.destroyed = true;
    this.stopRequested = true;
    this.request.token = '';
    this.request.cookie = '';
  }
}
