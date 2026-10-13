export interface DecodeIssue { index: number; type: string; maLk: string; message: string; }
export interface DecodeSummary {
  at: string; success: number; skipped: number; errors: number;
  hoso: number; xml1: number; xml2: number; xml3: number; hasOutput: boolean;
}
export interface DecodeReport extends DecodeSummary { issues: DecodeIssue[]; }
const LIMIT = 100 * 1024 * 1024;

function parseXml(value: string): Document {
  if (/<!DOCTYPE|<!ENTITY/i.test(value)) throw new Error('Không chấp nhận DTD hoặc khai báo thực thể XML.');
  const document = new DOMParser().parseFromString(value.replace(/^\uFEFF/, ''), 'application/xml');
  if (document.getElementsByTagName('parsererror').length || !document.documentElement) throw new Error('XML không đúng cấu trúc.');
  return document;
}

function indentXml(document: Document): void {
  const stack: { element: Element; depth: number }[] = [{ element: document.documentElement, depth: 0 }];
  while (stack.length) {
    const { element, depth } = stack.pop()!;
    if (depth > 256) throw new Error('XML vượt quá 256 cấp lồng nhau.');
    const children = Array.from(element.children);
    children.forEach(child => stack.push({ element: child, depth: depth + 1 }));
    // Không thay đổi khoảng trắng của giá trị/mixed content hoặc CDATA.
    if (!children.length || Array.from(element.childNodes).some(node =>
      node.nodeType === Node.CDATA_SECTION_NODE || node.nodeType === Node.TEXT_NODE && !!node.textContent?.trim())) continue;
    for (const node of Array.from(element.childNodes)) {
      if (node.nodeType === Node.TEXT_NODE && !node.textContent?.trim()) element.removeChild(node);
    }
    for (const node of Array.from(element.childNodes)) element.insertBefore(document.createTextNode('\n' + '  '.repeat(depth + 1)), node);
    element.appendChild(document.createTextNode('\n' + '  '.repeat(depth)));
  }
}

export async function decodeHisXml(raw: Blob): Promise<{ decoded: Blob | null; report: DecodeReport }> {
  const report: DecodeReport = { at: new Date().toISOString(), success: 0, skipped: 0, errors: 0,
    hoso: 0, xml1: 0, xml2: 0, xml3: 0, hasOutput: false, issues: [] };
  const issue = (index: number, type: string, maLk: string, message: string): void => {
    report.errors++; report.issues.push({ index, type, maLk, message });
  };
  try {
    if (!raw.size || raw.size > 50 * 1024 * 1024) throw new Error('XML gốc rỗng hoặc vượt quá 50 MB.');
    const document = parseXml(new TextDecoder('utf-8', { fatal: true }).decode(await raw.arrayBuffer()));
    const contents = Array.from(document.getElementsByTagName('NOIDUNGFILE'));
    if (!contents.length) issue(0, '', '', 'Không tìm thấy thẻ NOIDUNGFILE.');
    let expanded = 0;
    for (let index = 0; index < contents.length; index++) {
      if (index % 20 === 0) await new Promise(resolve => setTimeout(resolve, 0));
      const content = contents[index];
      const type = content.parentElement?.getElementsByTagName('LOAIHOSO')[0]?.textContent || '';
      const maLk = content.closest('HOSO')?.getElementsByTagName('MA_LK')[0]?.textContent || '';
      if (content.children.length) { report.skipped++; continue; }
      const base64 = (content.textContent || '').replace(/\s/g, '');
      if (!base64) { issue(index + 1, type, maLk, 'NOIDUNGFILE rỗng; giữ nguyên.'); continue; }
      let bytes: Uint8Array;
      try {
        if (!/^[A-Za-z0-9+/]*={0,2}$/.test(base64) || base64.length % 4 === 1) throw new Error();
        const binary = atob(base64);
        bytes = Uint8Array.from(binary, value => value.charCodeAt(0));
      } catch { issue(index + 1, type, maLk, 'Base64 không hợp lệ; giữ nguyên nội dung gốc.'); continue; }
      expanded += bytes.byteLength;
      if (expanded > LIMIT) throw new Error('Nội dung giải mã vượt quá 100 MB.');
      let text: string;
      try { text = new TextDecoder('utf-8', { fatal: true }).decode(bytes); }
      catch {
        content.textContent = new TextDecoder().decode(bytes).replace(/[\u0000-\u0008\u000B\u000C\u000E-\u001F]/g, '\uFFFD');
        issue(index + 1, type, maLk, 'Nội dung không phải UTF-8 hợp lệ; giữ bản text thay thế để kiểm tra.');
        continue;
      }
      try {
        const inner = parseXml(text);
        content.textContent = '';
        content.appendChild(document.importNode(inner.documentElement, true));
        report.success++;
      } catch {
        // XMLSerializer tự escape text lỗi, không đưa text vào innerHTML.
        content.textContent = text.replace(/[\u0000-\u0008\u000B\u000C\u000E-\u001F]/g, '\uFFFD');
        issue(index + 1, type, maLk, 'Giải mã được nhưng XML con không hợp lệ; giữ văn bản để kiểm tra.');
      }
    }
    report.hoso = document.getElementsByTagName('HOSO').length;
    report.xml1 = Array.from(document.getElementsByTagName('FILEHOSO')).filter(file =>
      file.getElementsByTagName('LOAIHOSO')[0]?.textContent === 'XML1').length;
    report.xml2 = document.getElementsByTagName('CHI_TIET_THUOC').length;
    report.xml3 = document.getElementsByTagName('CHI_TIET_DVKT').length;
    indentXml(document);
    const decoded = new Blob(['<?xml version="1.0" encoding="utf-8"?>\n',
      new XMLSerializer().serializeToString(document.documentElement)], { type: 'application/xml;charset=utf-8' });
    if (decoded.size > LIMIT) throw new Error('XML sau định dạng vượt quá 100 MB.');
    report.hasOutput = true;
    return { decoded, report };
  } catch (error) {
    issue(0, '', '', error instanceof Error ? error.message : 'Không thể decode file XML.');
    return { decoded: null, report };
  }
}

export function decodeCsv(reports: { fileName: string; report: DecodeReport }[]): Blob {
  const cell = (value: unknown): string => {
    let text = String(value ?? '');
    if (/^[=+\-@\t\r]/.test(text)) text = "'" + text;
    return '"' + text.replace(/"/g, '""') + '"';
  };
  const rows: unknown[][] = [['file', 'thanh_cong', 'bo_qua', 'loi', 'HOSO', 'XML1', 'XML2', 'XML3', 'thu_tu_the', 'LOAIHOSO', 'MA_LK', 'ghi_chu']];
  for (const { fileName, report } of reports) {
    const details = report.issues.length ? report.issues : [{ index: '', type: '', maLk: '', message: 'OK' }];
    for (const detail of details) rows.push([fileName, report.success, report.skipped, report.errors,
      report.hoso, report.xml1, report.xml2, report.xml3, detail.index, detail.type, detail.maLk, detail.message]);
  }
  return new Blob(['\uFEFF', rows.map(row => row.map(cell).join(',')).join('\r\n')], { type: 'text/csv;charset=utf-8' });
}

// ZIP không nén: dùng Blob, không thêm thư viện và không gửi XML ra server.
export async function createXmlZip(entries: { name: string; blob: Blob }[]): Promise<Blob> {
  if (entries.length > 65535 || entries.reduce((sum, entry) => sum + entry.blob.size, 0) > 200 * 1024 * 1024)
    throw new Error('ZIP vượt 200 MB hoặc quá nhiều file. Hãy chọn ít tháng hơn.');
  const table = new Uint32Array(256);
  for (let n = 0; n < 256; n++) { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; table[n] = c; }
  const local: BlobPart[] = [], central: BlobPart[] = [];
  let offset = 0, centralSize = 0;
  for (const entry of entries) {
    const name = new TextEncoder().encode(entry.name);
    const bytes = new Uint8Array(await entry.blob.arrayBuffer());
    let crc = 0xffffffff;
    for (const byte of bytes) crc = table[(crc ^ byte) & 255] ^ (crc >>> 8);
    crc = (crc ^ 0xffffffff) >>> 0;
    const header = new Uint8Array(30 + name.length);
    const h = new DataView(header.buffer);
    h.setUint32(0, 0x04034b50, true); h.setUint16(4, 20, true); h.setUint16(6, 0x800, true);
    h.setUint16(12, 0x21, true); h.setUint32(14, crc, true); h.setUint32(18, bytes.length, true);
    h.setUint32(22, bytes.length, true); h.setUint16(26, name.length, true); header.set(name, 30);
    local.push(header, entry.blob);
    const directory = new Uint8Array(46 + name.length);
    const d = new DataView(directory.buffer);
    d.setUint32(0, 0x02014b50, true); d.setUint16(4, 20, true); d.setUint16(6, 20, true);
    d.setUint16(8, 0x800, true); d.setUint16(14, 0x21, true); d.setUint32(16, crc, true);
    d.setUint32(20, bytes.length, true); d.setUint32(24, bytes.length, true);
    d.setUint16(28, name.length, true); d.setUint32(42, offset, true); directory.set(name, 46);
    central.push(directory); centralSize += directory.length; offset += header.length + bytes.length;
  }
  const end = new Uint8Array(22), e = new DataView(end.buffer);
  e.setUint32(0, 0x06054b50, true); e.setUint16(8, entries.length, true); e.setUint16(10, entries.length, true);
  e.setUint32(12, centralSize, true); e.setUint32(16, offset, true);
  return new Blob([...local, ...central, end], { type: 'application/zip' });
}
