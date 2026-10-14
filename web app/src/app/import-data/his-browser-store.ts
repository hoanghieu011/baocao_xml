import type { HisFetchRequest } from '../services/import-data.service';
import type { DecodeReport, DecodeSummary } from './his-xml-utils';

export interface HisImportLog {
  at: string;
  status: 'success' | 'error';
  message: string;
  countXML1?: number;
  countXML2?: number;
  countXML3?: number;
}

export interface HisLocalFile {
  id: string;
  year: number;
  month: number;
  cskcb: string;
  locTheo: string;
  recordCount: number;
  size: number;
  fetchedAt: string;
  importedAt: string | null;
  fileName: string;
  decode?: DecodeSummary;
  importLog?: HisImportLog;
}

// Tách metadata khỏi Blob để tải danh sách không đọc toàn bộ XML vào RAM.
export class HisBrowserStore {
  constructor(private readonly databaseName = 'his-xml-browser-v1') {}

  private open(): Promise<IDBDatabase> {
    return new Promise((resolve, reject) => {
      if (!globalThis.indexedDB) { reject(new Error('Trình duyệt không hỗ trợ kho XML cục bộ.')); return; }
      const request = indexedDB.open(this.databaseName, 1);
      request.onupgradeneeded = () => {
        const database = request.result;
        database.createObjectStore('files', { keyPath: ['scope', 'id'] }).createIndex('scope', 'scope');
        database.createObjectStore('blobs', { keyPath: ['scope', 'id', 'kind'] });
      };
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(new Error('Không mở được kho XML của trình duyệt.'));
      request.onblocked = () => reject(new Error('Kho XML đang bị khóa bởi tab khác. Hãy đóng tab cũ và thử lại.'));
    });
  }

  private async transaction<T>(mode: IDBTransactionMode, run: (tx: IDBTransaction, result: (value: T) => void) => void): Promise<T> {
    const database = await this.open();
    return new Promise<T>((resolve, reject) => {
      const tx = database.transaction(['files', 'blobs'], mode);
      let result: T;
      tx.oncomplete = () => { database.close(); resolve(result); };
      tx.onabort = () => {
        database.close();
        reject(new Error(tx.error?.name === 'QuotaExceededError'
          ? 'Bộ nhớ trình duyệt đã đầy. Tải ZIP để lưu lại rồi xóa các file không cần thiết.'
          : 'Không thể đọc/ghi kho XML trong trình duyệt. Dữ liệu chưa được xác nhận lưu thành công.'));
      };
      try { run(tx, value => { result = value; }); }
      catch (error) { tx.abort(); reject(error); }
    });
  }

  list(scope: string): Promise<HisLocalFile[]> {
    return this.transaction('readonly', (tx, done) => {
      const request = tx.objectStore('files').index('scope').getAll(scope);
      request.onsuccess = () => done(request.result.sort((a: HisLocalFile, b: HisLocalFile) =>
        a.year - b.year || a.month - b.month || a.fetchedAt.localeCompare(b.fetchedAt)));
    });
  }

  async save(scope: string, request: Pick<HisFetchRequest, 'year' | 'month' | 'cskcb' | 'locTheo'>, raw: Blob,
    recordCount: number, name?: string): Promise<HisLocalFile> {
    if (!scope) throw new Error('Chưa xác định được tài khoản cho kho XML.');
    if (!raw.size || raw.size > 50 * 1024 * 1024) throw new Error('File XML phải có dữ liệu và không quá 50 MB.');
    if (!crypto.subtle) throw new Error('Hãy mở ứng dụng bằng HTTPS hoặc localhost để sử dụng kho XML.');
    const digest = async (bytes: ArrayBuffer): Promise<string> => Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256', bytes)))
      .map(value => value.toString(16).padStart(2, '0')).join('');
    const contentHash = await digest(await raw.arrayBuffer());
    const id = await digest(new TextEncoder().encode(`${request.year}/${request.month}/${request.cskcb}/${request.locTheo}/${contentHash}`).buffer);
    return this.transaction('readwrite', (tx, done) => {
      const files = tx.objectStore('files');
      const existing = files.get([scope, id]);
      existing.onsuccess = () => {
        if (existing.result) { done(existing.result); return; }
        const file: HisLocalFile = { id, ...request, recordCount, size: raw.size, fetchedAt: new Date().toISOString(), importedAt: null,
          fileName: name || `XML_${request.year}${String(request.month).padStart(2, '0')}_${id.slice(0, 12)}.xml` };
        // Chỉ lưu các trường đã liệt kê; không đưa token/cookie từ request vào IndexedDB.
        const stored = { scope, id: file.id, year: file.year, month: file.month, cskcb: file.cskcb,
          locTheo: file.locTheo, recordCount, size: file.size, fetchedAt: file.fetchedAt, importedAt: null, fileName: file.fileName };
        files.add(stored);
        tx.objectStore('blobs').add({ scope, id, kind: 'raw', blob: raw });
        done(stored);
      };
    });
  }

  async read(scope: string, id: string, kind: 'raw' | 'decoded' | 'report' = 'raw'): Promise<Blob> {
    const value = await this.transaction<Blob | undefined>('readonly', (tx, done) => {
      const request = tx.objectStore('blobs').get([scope, id, kind]);
      request.onsuccess = () => done(request.result?.blob);
    });
    if (!value) throw new Error('Không tìm thấy file trong trình duyệt này. Hãy fetch hoặc chọn file lại.');
    return value;
  }

  markImported(scope: string, id: string): Promise<void> {
    return this.update(scope, id, (file) => { file.importedAt = new Date().toISOString(); });
  }

  saveImportLog(scope: string, id: string, log: HisImportLog): Promise<void> {
    return this.update(scope, id, file => {
      file.importLog = log;
      if (log.status === 'success') file.importedAt = log.at;
    });
  }

  saveDecoded(scope: string, id: string, decoded: Blob | null, report: DecodeReport): Promise<void> {
    return this.update(scope, id, (file, tx) => {
      const { issues, ...summary } = report;
      file.decode = summary;
      const blobs = tx.objectStore('blobs');
      if (decoded) blobs.put({ scope, id, kind: 'decoded', blob: decoded });
      else blobs.delete([scope, id, 'decoded']);
      blobs.put({ scope, id, kind: 'report', blob: new Blob([JSON.stringify(report)], { type: 'application/json' }) });
    });
  }

  private update(scope: string, id: string, change: (file: HisLocalFile, tx: IDBTransaction) => void): Promise<void> {
    return this.transaction('readwrite', (tx, done) => {
      const files = tx.objectStore('files');
      const request = files.get([scope, id]);
      request.onsuccess = () => {
        if (!request.result) { tx.abort(); return; }
        change(request.result, tx);
        files.put(request.result);
        done(undefined);
      };
    });
  }

  reset(): Promise<void> {
    return this.transaction('readwrite', (tx, done) => {
      tx.objectStore('files').clear();
      tx.objectStore('blobs').clear();
      done(undefined);
    });
  }

  remove(scope: string, id: string): Promise<void> {
    return this.transaction('readwrite', (tx, done) => {
      tx.objectStore('files').delete([scope, id]);
      for (const kind of ['raw', 'decoded', 'report']) tx.objectStore('blobs').delete([scope, id, kind]);
      done(undefined);
    });
  }
}
