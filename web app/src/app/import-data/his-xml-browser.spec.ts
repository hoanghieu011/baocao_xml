import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { importLogCsv } from './his-import-log';
import { TestBed } from '@angular/core/testing';
import { ImportDataService } from '../services/import-data.service';
import { HisBrowserStore } from './his-browser-store';
import { HisImportComponent } from './his-import.component';
import { createXmlZip, decodeCsv, decodeHisXml } from './his-xml-utils';

const b64 = (text: string): string => btoa(Array.from(new TextEncoder().encode(text)).map(b => String.fromCharCode(b)).join(''));
const envelope = (content: string): Blob => new Blob([
  `<GIAMDINHHS><HOSO><FILEHOSO><LOAIHOSO>XML1</LOAIHOSO><NOIDUNGFILE>${content}</NOIDUNGFILE></FILEHOSO></HOSO></GIAMDINHHS>`
], { type: 'application/xml' });
const rawXml = (): Blob => envelope(b64('<TONG_HOP><MA_LK>A</MA_LK><HO_TEN>Nguyễn Văn A</HO_TEN></TONG_HOP>'));

describe('Decode XML trong trình duyệt', () => {
  it('giải mã UTF-8, giữ nguyên Blob gốc và tạo XML con dễ đọc', async () => {
    const raw = rawXml(), before = await raw.text();
    const { decoded, report } = await decodeHisXml(raw);
    expect(report.success).toBe(1); expect(report.errors).toBe(0); expect(report.xml1).toBe(1);
    const text = await decoded!.text();
    expect(text).toContain('Nguyễn Văn A'); expect(text).toContain('\n  <HOSO>');
    expect(new DOMParser().parseFromString(text, 'application/xml').querySelector('NOIDUNGFILE > TONG_HOP')).not.toBeNull();
    expect(await raw.text()).toBe(before);
  });
  it('bỏ qua XML đã decode để chạy lại an toàn', async () => {
    const first = await decodeHisXml(rawXml());
    const second = await decodeHisXml(first.decoded!);
    expect(second.report.skipped).toBe(1); expect(second.report.success).toBe(0); expect(second.report.errors).toBe(0);
  });
  it('ghi lỗi Base64 và giữ nội dung', async () => {
    const result = await decodeHisXml(envelope('%%%'));
    expect(result.report.errors).toBe(1); expect(await result.decoded!.text()).toContain('%%%');
  });
  it('XML con lỗi được giữ dưới dạng text escape, không hỏng XML ngoài', async () => {
    const result = await decodeHisXml(envelope(b64('<BROKEN>&')));
    const text = await result.decoded!.text();
    expect(result.report.errors).toBe(1); expect(text).toContain('&lt;BROKEN&gt;&amp;');
    expect(new DOMParser().parseFromString(text, 'application/xml').getElementsByTagName('parsererror').length).toBe(0);
  });
  it('rỗng, XML ngoài sai và DTD không được coi là decode thành công', async () => {
    for (const value of ['', '<x>', '<!DOCTYPE x [<!ENTITY e "bad">]><x>&e;</x>']) {
      const result = await decodeHisXml(new Blob([value]));
      expect(result.decoded).toBeNull(); expect(result.report.errors).toBeGreaterThan(0);
    }
    expect((await decodeHisXml(envelope(''))).report.errors).toBe(1);
  });
  it('XML1/2/3 và HOSO được đếm sau decode', async () => {
    const raw = new Blob([`<ROOT><HOSO>
      <FILEHOSO><LOAIHOSO>XML1</LOAIHOSO><NOIDUNGFILE>${b64('<TONG_HOP><MA_LK>A</MA_LK></TONG_HOP>')}</NOIDUNGFILE></FILEHOSO>
      <FILEHOSO><LOAIHOSO>XML2</LOAIHOSO><NOIDUNGFILE>${b64('<DS><CHI_TIET_THUOC/><CHI_TIET_THUOC/></DS>')}</NOIDUNGFILE></FILEHOSO>
      <FILEHOSO><LOAIHOSO>XML3</LOAIHOSO><NOIDUNGFILE>${b64('<DS><CHI_TIET_DVKT/></DS>')}</NOIDUNGFILE></FILEHOSO>
      </HOSO></ROOT>`]);
    const { report } = await decodeHisXml(raw);
    expect([report.hoso, report.xml1, report.xml2, report.xml3]).toEqual([1, 1, 2, 1]);
  });
  it('log CSV có BOM và chặn công thức từ tên file', async () => {
    const result = await decodeHisXml(envelope('%%%'));
    const csv = decodeCsv([{ fileName: '=cmd.xml', report: result.report }]);
    expect(Array.from(new Uint8Array(await csv.slice(0, 3).arrayBuffer()))).toEqual([239, 187, 191]);
    expect(await csv.text()).toContain("'=cmd.xml");
    expect(await csv.text()).toContain('Base64');
  });
  it('ZIP chứa đường dẫn UTF-8, nội dung và central directory', async () => {
    const zip = await createXmlZip([{ name: 'decoded/202601/tiếng-việt.xml', blob: new Blob(['<x>Việt Nam</x>']) }]);
    const bytes = await zip.arrayBuffer(), view = new DataView(bytes);
    expect(view.getUint32(0, true)).toBe(0x04034b50);
    const nameLength = view.getUint16(26, true);
    const size = view.getUint32(18, true);
    expect(new TextDecoder().decode(bytes.slice(30, 30 + nameLength))).toBe('decoded/202601/tiếng-việt.xml');
    expect(new TextDecoder().decode(bytes.slice(30 + nameLength, 30 + nameLength + size))).toBe('<x>Việt Nam</x>');
    expect(view.getUint32(30 + nameLength + size, true)).toBe(0x02014b50);
    expect(view.getUint32(bytes.byteLength - 22, true)).toBe(0x06054b50);
  });
  it('ZIP tính CRC32 đúng chuẩn', async () => {
    const bytes = await (await createXmlZip([{ name: 'crc.txt', blob: new Blob(['123456789']) }])).arrayBuffer();
    expect(new DataView(bytes).getUint32(14, true)).toBe(0xcbf43926);
  });
  it('toggle mặc định đóng, cho chọn tháng trước kiểm tra và preview chỉ hiển thị text', async () => {
    await TestBed.configureTestingModule({ imports: [HisImportComponent], providers: [
      { provide: ImportDataService, useValue: {} }
    ] }).compileComponents();
    const fixture = TestBed.createComponent(HisImportComponent);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect((root.querySelector('details') as HTMLDetailsElement).open).toBeFalse();
    expect(root.querySelector('#hisFrom')!.matches(':disabled')).toBeFalse();
    fixture.componentInstance.sessionValid = true;
    (fixture.componentInstance as unknown as { scope: string }).scope = 'test';
    fixture.componentInstance.previewText = '<img src=x onerror=alert(1) />';
    fixture.detectChanges();
    expect(root.querySelector('#hisFrom')!.matches(':disabled')).toBeFalse();
    expect(root.querySelector('pre')!.textContent).toContain('<img');
    expect(root.querySelector('pre img')).toBeNull();
    fixture.destroy();
  });
});

describe('Kho IndexedDB và luồng import', () => {
  let name: string, store: HisBrowserStore;
  const request = { year: 2026, month: 1, cskcb: '35046', locTheo: '2' };
  beforeEach(() => { name = 'his-browser-test-' + crypto.randomUUID(); store = new HisBrowserStore(name); });
  afterEach(async () => {
    await new Promise<void>((resolve, reject) => { const remove = indexedDB.deleteDatabase(name); remove.onsuccess = () => resolve(); remove.onerror = () => reject(remove.error); });
  });
  it('lưu Blob qua lần khởi tạo lại và tách tài khoản', async () => {
    const file = await store.save('user-a', request, rawXml(), 1);
    const restarted = new HisBrowserStore(name);
    expect((await restarted.list('user-a')).length).toBe(1);
    expect((await restarted.list('user-b')).length).toBe(0);
    expect(await (await restarted.read('user-a', file.id)).text()).toBe(await rawXml().text());
    await expectAsync(restarted.read('user-b', file.id)).toBeRejected();
  });
  it('không lưu token/cookie; fetch lại giữ trạng thái import và decode', async () => {
    const credentials = { ...request, token: 'secret-token', cookie: 'secret-cookie' };
    const file = await store.save('a', credentials, rawXml(), 1);
    const decoded = await decodeHisXml(rawXml());
    await store.saveDecoded('a', file.id, decoded.decoded, decoded.report);
    await store.markImported('a', file.id);
    const again = await store.save('a', credentials, rawXml(), 1);
    expect(again.id).toBe(file.id); expect(again.importedAt).toBeTruthy(); expect(again.decode?.success).toBe(1);
    expect(JSON.stringify(await store.list('a'))).not.toContain('secret');
    expect((await store.list('a')).length).toBe(1);
    expect(await (await store.read('a', file.id)).text()).toBe(await rawXml().text());
  });
  it('ghi đồng thời decode và import không làm mất trạng thái', async () => {
    const file = await store.save('a', request, rawXml(), 1), result = await decodeHisXml(rawXml());
    await Promise.all([store.saveDecoded('a', file.id, result.decoded, result.report), store.markImported('a', file.id)]);
    const [saved] = await store.list('a'); expect(saved.importedAt).toBeTruthy(); expect(saved.decode?.hasOutput).toBeTrue();
  });
  it('xóa cục bộ xóa raw/decode/log, không tác động tài khoản khác', async () => {
    const a = await store.save('a', request, rawXml(), 1), b = await store.save('b', request, rawXml(), 1);
    const result = await decodeHisXml(rawXml()); await store.saveDecoded('a', a.id, result.decoded, result.report);
    await store.remove('a', a.id);
    expect((await store.list('a')).length).toBe(0);
    await expectAsync(store.read('a', a.id, 'decoded')).toBeRejected();
    await expectAsync(store.read('a', a.id, 'report')).toBeRejected();
    expect((await store.read('b', b.id)).size).toBeGreaterThan(0);
  });
  it('reset xóa mọi scope và Blob, kho vẫn lưu được file mới', async () => {
    const result = await decodeHisXml(rawXml());
    for (const scope of ['a', 'b']) {
      const file = await store.save(scope, request, rawXml(), 1);
      await store.saveDecoded(scope, file.id, result.decoded, result.report);
      await store.saveImportLog(scope, file.id, { at: new Date().toISOString(), status: 'success', message: 'OK' });
    }
    const [file] = await store.list('a');
    await store.reset();
    const reopened = new HisBrowserStore(name);
    for (const scope of ['a', 'b']) {
      expect(await reopened.list(scope)).toEqual([]);
      for (const kind of ['raw', 'decoded', 'report'] as const) {
        await expectAsync(reopened.read(scope, file.id, kind)).toBeRejected();
      }
    }
    const fresh = await reopened.save('a', request, rawXml(), 1);
    expect(fresh.importedAt).toBeNull(); expect(fresh.importLog).toBeUndefined();
    expect((await reopened.list('a')).length).toBe(1);
  });

  it('reset cần xác nhận, dùng được trước kiểm tra phiên và xóa trạng thái màn hình', async () => {
    const component = new HisImportComponent({} as ImportDataService);
    Object.assign(component, { store });
    component.files = [await store.save('a', request, rawXml(), 1)];
    component.previewText = '<x/>'; component.previewName = 'test.xml'; component.previewPage = 2;
    component.months[0].status = 'Đã import';
    const confirm = spyOn(window, 'confirm').and.returnValue(false);
    await component.resetStore();
    expect(component.files.length).toBe(1); expect((await store.list('a')).length).toBe(1);
    confirm.and.returnValue(true);
    component.busy = true; await component.resetStore();
    expect(confirm).toHaveBeenCalledTimes(1);
    component.busy = false;
    await component.resetStore();
    expect(component.files).toEqual([]); expect(await store.list('a')).toEqual([]);
    expect(component.previewText).toBe(''); expect(component.previewName).toBe(''); expect(component.previewPage).toBe(0);
    expect(component.months[0].status).toBe(''); expect(component.busy).toBeFalse();
  });

  it('reset lỗi giữ danh sách hiện tại và báo lỗi', async () => {
    const component = new HisImportComponent({} as ImportDataService);
    Object.assign(component, { store });
    component.files = [await store.save('a', request, rawXml(), 1)];
    spyOn(window, 'confirm').and.returnValue(true);
    spyOn(store, 'reset').and.rejectWith(new Error('Không thể xóa kho'));
    await component.resetStore();
    expect(component.files.length).toBe(1); expect(component.error).toBe('Không thể xóa kho');
    expect(component.busy).toBeFalse(); expect(component.message).toBe('');
  });

  it('fetch nhiều tháng chỉ lưu browser; decode không gọi HTTP; import mới upload raw', async () => {
    const upload = jasmine.createSpy('upload').and.returnValue(of({ isError: false, message: 'OK' }));
    const fetchXml = jasmine.createSpy('fetch').and.returnValue(of(rawXml()));
    const service = { checkHisSession: () => of({ isValid: true, message: 'OK' }), getHisBrowserScope: () => of({ scope: 'a' }),
      fetchHisRecords: () => of({ total: 1, records: [{ maLk: 'A', qd3176: 'auto' }] }), fetchHisXmlBatch: fetchXml, importXMLData: upload };
    const component = new HisImportComponent(service as unknown as ImportDataService);
    spyOn<any>(component, 'saveImportLogDownload');
    (component as unknown as { store: HisBrowserStore }).store = store;
    spyOn(component as unknown as { delay(ms: number): Promise<void> }, 'delay').and.resolveTo();
    component.request.token = 't'; component.request.cookie = 'c';
    component.fromMonth = '2026-01'; component.toMonth = '2026-02'; component.updateMonths();
    await component.fetchXml(); expect(fetchXml).not.toHaveBeenCalled();
    await component.checkSession(); await component.fetchXml();
    expect(fetchXml).toHaveBeenCalledTimes(2); expect(upload).not.toHaveBeenCalled(); expect(component.files.length).toBe(2);
    await component.decodeFiles(); expect(fetchXml).toHaveBeenCalledTimes(2); expect(upload).not.toHaveBeenCalled();
    component.months[1].selected = false;
    await component.importXml(); expect(upload).toHaveBeenCalledTimes(1);
    expect(await (upload.calls.mostRecent().args[0] as File).text()).toBe(await rawXml().text());
    await component.importXml(); expect(upload).toHaveBeenCalledTimes(1);
  });
  it('file decode lỗi bị loại khỏi import', async () => {
    const file = await store.save('a', request, envelope('%%%'), 1), result = await decodeHisXml(envelope('%%%'));
    await store.saveDecoded('a', file.id, result.decoded, result.report);
    const component = new HisImportComponent({} as ImportDataService);
    component.fromMonth = '2026-01'; component.toMonth = '2026-01'; component.updateMonths(); component.files = await store.list('a');
    expect(component.selectedFiles.length).toBe(1); expect(component.pendingFiles.length).toBe(0);
  });

  for (const fails of [false, true]) {
    it(`tự tải và lưu log import ${fails ? 'lỗi HTTP' : 'thành công'} khi chưa decode`, async () => {
      const file = await store.save('a', request, rawXml(), 1);
      const upload = jasmine.createSpy('upload').and.returnValue(fails
        ? throwError(() => new HttpErrorResponse({ status: 500, error: 'Lỗi SQL: MA_LK A' }))
        : of({ isError: false, message: 'Thêm mới thành công!', countXML1: 1, countXML2: 2, countXML3: 3 }));
      const component = new HisImportComponent({ importXMLData: upload } as unknown as ImportDataService);
      Object.assign(component, { store, scope: 'a', sessionValid: true });
      component.fromMonth = component.toMonth = '2026-01'; component.updateMonths();
      component.files = [file];
      const download = spyOn<any>(component, 'saveImportLogDownload');
      await component.importXml();
      expect(download).toHaveBeenCalledTimes(1);
      const [saved] = await new HisBrowserStore(name).list('a');
      expect(saved.decode).toBeUndefined();
      expect(saved.importLog?.status).toBe(fails ? 'error' : 'success');
      expect(!!saved.importedAt).toBe(!fails);
      expect(saved.importLog?.message).toContain(fails ? 'Lỗi SQL: MA_LK A' : 'Thêm mới thành công!');
      await component.downloadLog(saved);
      expect(download).toHaveBeenCalledTimes(2);
      const csv = importLogCsv([{ fileName: '=file.xml', log: saved.importLog! }]);
      expect(Array.from(new Uint8Array(await csv.slice(0, 3).arrayBuffer()))).toEqual([239, 187, 191]);
      expect(await csv.text()).toContain("'=file.xml");
      expect(await csv.text()).toContain(fails ? 'Lỗi SQL' : 'Thành công');
    });
  }

  it('bật Log lỗi sau import không cần decode', async () => {
    await TestBed.configureTestingModule({ imports: [HisImportComponent], providers: [
      { provide: ImportDataService, useValue: {} }
    ] }).compileComponents();
    const fixture = TestBed.createComponent(HisImportComponent), component = fixture.componentInstance;
    Object.assign(component, { scope: 'a', sessionValid: true });
    component.fromMonth = component.toMonth = '2026-01'; component.updateMonths();
    const file = await store.save('a', request, rawXml(), 1);
    component.files = [file]; fixture.detectChanges();
    const button = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button'))
      .find(button => button.textContent?.trim() === 'Log lỗi')!;
    expect(button.disabled).toBeTrue();
    file.importLog = { at: new Date().toISOString(), status: 'success', message: 'OK' };
    fixture.detectChanges(); expect(button.disabled).toBeFalse();
    fixture.destroy();
  });
});
