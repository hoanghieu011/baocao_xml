import type { HisImportLog } from './his-browser-store';

export function importLogCsv(entries: { fileName: string; log: HisImportLog }[]): Blob {
  const cell = (value: unknown): string => {
    let text = String(value ?? '');
    if (/^[=+\-@\t\r]/.test(text)) text = "'" + text;
    return '"' + text.replace(/"/g, '""') + '"';
  };
  const rows: unknown[][] = [['file', 'thoi_gian', 'trang_thai', 'XML1', 'XML2', 'XML3', 'noi_dung']];
  for (const { fileName, log } of entries) {
    rows.push([fileName, log.at, log.status === 'success' ? 'Thành công' : 'Lỗi',
      log.countXML1, log.countXML2, log.countXML3, log.message]);
  }
  return new Blob(['\uFEFF', rows.map(row => row.map(cell).join(',')).join('\r\n')],
    { type: 'text/csv;charset=utf-8' });
}
