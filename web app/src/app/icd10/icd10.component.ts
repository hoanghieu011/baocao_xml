import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Icd10Service } from '../services/icd10.service';
import { BorderDirective, TableDirective, ToastModule } from '@coreui/angular';

// Một node trong cây danh mục ICD-10 (kèm trạng thái hiển thị).
interface Icd10Node {
  ma_id: string;
  ma_icd?: string;
  ten_icd?: string;
  loai?: string;
  ma_cha?: string;
  is_leaf?: number;
  cap?: number;

  level: number;        // độ sâu để thụt lề (0 = chương gốc)
  expanded: boolean;    // đang mở rộng?
  loaded: boolean;      // đã tải con chưa?
  loading: boolean;     // đang tải con?
  isMatch: boolean;     // có khớp từ khóa tìm kiếm? (dùng để tô đậm)
  children: Icd10Node[];
}

@Component({
  selector: 'app-icd10',
  standalone: true,
  imports: [CommonModule, FormsModule, TableDirective, BorderDirective, ToastModule],
  templateUrl: './icd10.component.html',
  styleUrl: './icd10.component.css'
})
export class Icd10Component implements OnInit {
  Math = Math;

  searchTerm: string = '';

  // Cây danh mục (browse mode)
  roots: Icd10Node[] = [];
  loading = false;
  syncing = false;

  // Chế độ tìm kiếm: cây được lọc, mở tới các node khớp
  searchMode = false;
  searchRoots: Icd10Node[] = [];
  searchTruncated = false;
  searchMatchCount = 0;

  toasts: any[] = [];

  constructor(private icd10Service: Icd10Service) {}

  ngOnInit(): void {
    this.loadRoots();
  }

  addToast(message: string, color: string = 'danger') {
    this.toasts.push({ message, color, visible: true });
    setTimeout(() => this.toasts.shift(), 3000);
  }

  // ----- Cây danh mục (duyệt) -----

  private toNode(raw: any, level: number, opts: Partial<Icd10Node> = {}): Icd10Node {
    return {
      ma_id: raw.ma_id,
      ma_icd: raw.ma_icd,
      ten_icd: raw.ten_icd,
      loai: raw.loai,
      ma_cha: raw.ma_cha,
      is_leaf: raw.is_leaf,
      cap: raw.cap,
      level,
      expanded: false,
      loaded: false,
      loading: false,
      isMatch: false,
      children: [],
      ...opts
    };
  }

  loadRoots() {
    this.loading = true;
    this.icd10Service.getCayCon('').subscribe({
      next: (res) => {
        this.roots = (res?.dsIcd10 ?? []).map((r: any) => this.toNode(r, 0));
        this.loading = false;
      },
      error: () => {
        this.addToast('Có lỗi xảy ra, vui lòng thử lại sau!');
        this.loading = false;
      }
    });
  }

  toggleNode(node: Icd10Node) {
    if (node.is_leaf === 1) return; // node lá (mã bệnh) không có con

    if (node.expanded) {
      node.expanded = false;
      return;
    }

    node.expanded = true;
    if (node.loaded || node.loading) return;

    node.loading = true;
    this.icd10Service.getCayCon(node.ma_id).subscribe({
      next: (res) => {
        node.children = (res?.dsIcd10 ?? []).map((r: any) => this.toNode(r, node.level + 1));
        node.loaded = true;
        node.loading = false;
      },
      error: () => {
        node.loading = false;
        node.expanded = false;
        this.addToast('Không tải được danh mục con.');
      }
    });
  }

  // Làm phẳng cây thành danh sách các node đang hiển thị (để render bằng bảng).
  get visibleNodes(): Icd10Node[] {
    const out: Icd10Node[] = [];
    const walk = (nodes: Icd10Node[]) => {
      for (const n of nodes) {
        out.push(n);
        if (n.expanded && n.children.length) walk(n.children);
      }
    };
    walk(this.searchMode ? this.searchRoots : this.roots);
    return out;
  }

  // ----- Tìm kiếm: dựng cây từ node khớp + tổ tiên -----

  onSearch() {
    const term = this.searchTerm?.trim() ?? '';
    if (!term) {
      this.exitSearch();
      return;
    }

    this.loading = true;
    this.icd10Service.timCay(term).subscribe({
      next: (res) => {
        const raw: any[] = res?.dsIcd10 ?? [];
        this.searchTruncated = !!res?.truncated;
        this.buildSearchTree(raw, term);
        this.searchMode = true;
        this.loading = false;
        if (raw.length === 0) {
          this.addToast('Không tìm thấy kết quả phù hợp.', 'info');
        }
      },
      error: () => {
        this.addToast('Có lỗi xảy ra, vui lòng thử lại sau!');
        this.loading = false;
      }
    });
  }

  // Dựng cây từ tập phẳng (node khớp + toàn bộ tổ tiên), tự mở toàn bộ.
  private buildSearchTree(raw: any[], term: string) {
    const lower = term.toLowerCase();
    const map = new Map<string, Icd10Node>();

    for (const r of raw) {
      map.set(r.ma_id, this.toNode(r, 0, {
        expanded: true,
        loaded: true,
        isMatch:
          (r.ma_icd?.toLowerCase().includes(lower) ?? false) ||
          (r.ten_icd?.toLowerCase().includes(lower) ?? false)
      }));
    }

    const roots: Icd10Node[] = [];
    for (const node of map.values()) {
      const parent = node.ma_cha ? map.get(node.ma_cha) : undefined;
      if (parent) {
        parent.children.push(node);
      } else {
        roots.push(node);
      }
    }

    // Sắp xếp theo mã ICD và gán lại level theo độ sâu thực tế
    const sortRec = (nodes: Icd10Node[], level: number) => {
      nodes.sort((a, b) => (a.ma_icd ?? '').localeCompare(b.ma_icd ?? ''));
      for (const n of nodes) {
        n.level = level;
        sortRec(n.children, level + 1);
      }
    };
    sortRec(roots, 0);

    this.searchRoots = roots;
    this.searchMatchCount = raw.filter((r: any) =>
      (r.ma_icd?.toLowerCase().includes(lower) ?? false) ||
      (r.ten_icd?.toLowerCase().includes(lower) ?? false)
    ).length;
  }

  exitSearch() {
    this.searchMode = false;
    this.searchTerm = '';
    this.searchRoots = [];
    this.searchTruncated = false;
    this.searchMatchCount = 0;
  }

  // ----- Đồng bộ -----

  dongBo() {
    if (this.syncing) return;
    this.syncing = true;
    this.addToast('Đang đồng bộ danh mục ICD-10 từ nguồn, vui lòng chờ...', 'info');

    this.icd10Service.dongBoTuNguon().subscribe({
      next: (res) => {
        this.syncing = false;
        this.addToast(
          `Đồng bộ thành công: ${res?.tongSoNode ?? 0} mục (${res?.soMaBenh ?? 0} mã bệnh).`,
          'success'
        );
        this.exitSearch();
        this.loadRoots();
      },
      error: (err) => {
        this.syncing = false;
        this.addToast('Đồng bộ không thành công. ' + (err?.error?.detail ?? ''), 'danger');
      }
    });
  }
}
