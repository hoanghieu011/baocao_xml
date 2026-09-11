import { AfterViewInit, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Chart, registerables } from 'chart.js';
import { DashboardService, DashboardStatItem, DashboardSummaryResponse } from '../services/dashboard.service';

const TS_PAGE_SIZE_OPTIONS = [10, 25, 50, 100];

Chart.register(...registerables);

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit, AfterViewInit, OnDestroy {
  Math = Math;

  readonly monthLabels: string[] = [
    'Th1', 'Th2', 'Th3', 'Th4', 'Th5', 'Th6',
    'Th7', 'Th8', 'Th9', 'Th10', 'Th11', 'Th12'
  ];

  readonly years: number[] = [];
  selectedYear: number = new Date().getFullYear();
  loading = false;

  totalVisits = 0;
  totalInsuredPatients = 0;
  totalRevenue = 0;
  bhytPaidRevenueTotal = 0;
  copayRevenueTotal = 0;
  insuredPatientRevenueTotal = 0;
  hospitalFeeRevenueTotal = 0;
  avgVisitsPerMonth = 0;

  visitsByMonth: number[] = [];
  bhytPaidRevenueByMonth: number[] = [];
  copayRevenueByMonth: number[] = [];
  insuredPatientRevenueByMonth: number[] = [];
  hospitalFeeRevenueByMonth: number[] = [];

  diseaseChapters: DashboardStatItem[] = [];

  // Bảng thống kê dịch vụ kỹ thuật (phân trang phía server)
  technicalServices: DashboardStatItem[] = [];
  tsPageNumber = 1;
  tsPageSize = 50;
  tsPageSizeOptions = TS_PAGE_SIZE_OPTIONS;
  tsTotalRecords = 0;
  tsLoading = false;

  private visitsLineChart?: Chart<'line', number[], string>;
  private revenueDonutChart?: Chart<'doughnut', number[], string>;
  private revenueBarChart?: Chart<'bar', number[], string>;
  private isViewInitialized = false;

  constructor(private dashboardService: DashboardService) {}

  ngOnInit(): void {
    this.buildYearOptions();
    this.loadDashboardData();
    this.loadTechnicalServices(true);
  }

  ngAfterViewInit(): void {
    this.isViewInitialized = true;
    this.renderCharts();
  }

  ngOnDestroy(): void {
    this.destroyCharts();
  }

  onYearChange(): void {
    this.loadDashboardData();
    this.loadTechnicalServices(true);
  }

  loadTechnicalServices(resetPage: boolean = false): void {
    if (resetPage) this.tsPageNumber = 1;

    this.tsLoading = true;
    this.dashboardService
      .getTechnicalServices(this.selectedYear, this.tsPageNumber, this.tsPageSize)
      .subscribe({
        next: (res) => {
          this.tsTotalRecords = res?.totalRecords ?? 0;
          this.tsPageNumber = res?.pageIndex ?? this.tsPageNumber;
          this.tsPageSize = res?.pageSize ?? this.tsPageSize;
          this.technicalServices = res?.items ?? [];
          this.tsLoading = false;
        },
        error: () => {
          this.technicalServices = [];
          this.tsTotalRecords = 0;
          this.tsLoading = false;
        }
      });
  }

  onTsPrev(): void {
    if (this.tsPageNumber > 1) {
      this.tsPageNumber--;
      this.loadTechnicalServices();
    }
  }

  onTsNext(): void {
    const maxPage = Math.max(1, Math.ceil(this.tsTotalRecords / this.tsPageSize));
    if (this.tsPageNumber < maxPage) {
      this.tsPageNumber++;
      this.loadTechnicalServices();
    }
  }

  onTsPageSizeChange(newSize: number): void {
    this.tsPageSize = Number(newSize);
    this.tsPageNumber = 1;
    this.loadTechnicalServices(true);
  }

  tsRowIndex(i: number): number {
    return (this.tsPageNumber - 1) * this.tsPageSize + i + 1;
  }

  get bhytPaidRevenueRate(): number {
    if (this.totalRevenue === 0) {
      return 0;
    }
    return (this.bhytPaidRevenueTotal / this.totalRevenue) * 100;
  }

  get copayRevenueRate(): number {
    if (this.totalRevenue === 0) {
      return 0;
    }
    return (this.copayRevenueTotal / this.totalRevenue) * 100;
  }

  get hospitalFeeRevenueRate(): number {
    if (this.totalRevenue === 0) {
      return 0;
    }
    return (this.hospitalFeeRevenueTotal / this.totalRevenue) * 100;
  }

  private buildYearOptions(): void {
    const currentYear = new Date().getFullYear();
    for (let year = currentYear - 4; year <= currentYear + 1; year++) {
      this.years.push(year);
    }
  }

  private loadDashboardData(): void {
    this.loading = true;
    this.dashboardService.getSummary(this.selectedYear).subscribe({
      next: (response) => {
        this.applyDashboardData(response);
        if (this.isViewInitialized) {
          this.renderCharts();
        }
        this.loading = false;
      },
      error: () => {
        this.resetData();
        if (this.isViewInitialized) {
          this.renderCharts();
        }
        this.loading = false;
      }
    });
  }

  private applyDashboardData(response: DashboardSummaryResponse): void {
    this.totalVisits = response.kpis.totalVisits ?? 0;
    this.totalInsuredPatients = response.kpis.insuredPatients ?? 0;
    this.totalRevenue = response.kpis.totalRevenue ?? 0;

    this.visitsByMonth = this.normalizeMonthlySeries(response.monthlyVisits);
    this.bhytPaidRevenueByMonth = this.normalizeMonthlySeries(response.monthlyRevenue.bhytPaid);
    this.copayRevenueByMonth = this.normalizeMonthlySeries(response.monthlyRevenue.copay);
    this.insuredPatientRevenueByMonth = this.normalizeMonthlySeries(response.monthlyRevenue.insuredPatientRevenue);
    this.hospitalFeeRevenueByMonth = this.normalizeMonthlySeries(response.monthlyRevenue.hospitalFee);

    this.bhytPaidRevenueTotal = response.revenueStructure.bhytPaid ?? this.sum(this.bhytPaidRevenueByMonth);
    this.copayRevenueTotal = response.revenueStructure.copay ?? this.sum(this.copayRevenueByMonth);
    this.hospitalFeeRevenueTotal = response.revenueStructure.hospitalFee ?? this.sum(this.hospitalFeeRevenueByMonth);
    this.insuredPatientRevenueTotal = this.bhytPaidRevenueTotal + this.copayRevenueTotal;
    this.avgVisitsPerMonth = Math.round(this.totalVisits / 12);

    this.diseaseChapters = response.diseaseChapters ?? [];
  }

  private resetData(): void {
    this.totalVisits = 0;
    this.totalInsuredPatients = 0;
    this.totalRevenue = 0;
    this.bhytPaidRevenueTotal = 0;
    this.copayRevenueTotal = 0;
    this.insuredPatientRevenueTotal = 0;
    this.hospitalFeeRevenueTotal = 0;
    this.avgVisitsPerMonth = 0;
    this.visitsByMonth = this.monthLabels.map(() => 0);
    this.bhytPaidRevenueByMonth = this.monthLabels.map(() => 0);
    this.copayRevenueByMonth = this.monthLabels.map(() => 0);
    this.insuredPatientRevenueByMonth = this.monthLabels.map(() => 0);
    this.hospitalFeeRevenueByMonth = this.monthLabels.map(() => 0);
    this.diseaseChapters = [];
  }

  private normalizeMonthlySeries(series: number[] | undefined): number[] {
    return this.monthLabels.map((_, index) => Number(series?.[index] ?? 0));
  }

  private renderCharts(): void {
    this.destroyCharts();
    this.renderVisitsLineChart();
    this.renderRevenueDonutChart();
    this.renderRevenueBarChart();
  }

  private renderVisitsLineChart(): void {
    const context = this.getCanvasContext('visitsLineChart');
    if (!context) {
      return;
    }

    this.visitsLineChart = new Chart(context, {
      type: 'line',
      data: {
        labels: this.monthLabels,
        datasets: [
          {
            label: 'Số lượt khám',
            data: this.visitsByMonth,
            borderColor: '#0d6efd',
            backgroundColor: 'rgba(13, 110, 253, 0.16)',
            fill: true,
            tension: 0.35,
            pointRadius: 3,
            pointHoverRadius: 5
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: true,
            position: 'top'
          }
        },
        scales: {
          y: {
            beginAtZero: true,
            ticks: {
              callback: (value) => Number(value).toLocaleString('vi-VN')
            }
          }
        }
      }
    });
  }

  private renderRevenueDonutChart(): void {
    const context = this.getCanvasContext('revenueDonutChart');
    if (!context) {
      return;
    }

    this.revenueDonutChart = new Chart(context, {
      type: 'doughnut',
      data: {
        labels: ['BHYT chi trả', 'Bệnh nhân cùng chi trả', 'Viện phí'],
        datasets: [
          {
            data: [this.bhytPaidRevenueTotal, this.copayRevenueTotal, this.hospitalFeeRevenueTotal],
            backgroundColor: ['#198754', '#0dcaf0', '#6f42c1'],
            borderColor: ['#ffffff', '#ffffff', '#ffffff'],
            borderWidth: 2,
            hoverOffset: 8
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: 'bottom'
          }
        }
      }
    });
  }

  private renderRevenueBarChart(): void {
    const context = this.getCanvasContext('revenueBarChart');
    if (!context) {
      return;
    }

    this.revenueBarChart = new Chart(context, {
      type: 'bar',
      data: {
        labels: this.monthLabels,
        datasets: [
          {
            label: 'Doanh thu bệnh nhân bảo hiểm',
            data: this.insuredPatientRevenueByMonth,
            backgroundColor: 'rgba(32, 201, 151, 0.75)',
            borderColor: '#20c997',
            borderWidth: 1,
            borderRadius: 6
          },
          {
            label: 'Doang thu bệnh nhân viện phí',
            data: this.hospitalFeeRevenueByMonth,
            backgroundColor: 'rgba(111, 66, 193, 0.75)',
            borderColor: '#6f42c1',
            borderWidth: 1,
            borderRadius: 6
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: 'top'
          }
        },
        scales: {
          y: {
            beginAtZero: true,
            ticks: {
              callback: (value) => `${(Number(value) / 1_000_000).toLocaleString('vi-VN')}tr`
            }
          }
        }
      }
    });
  }

  private destroyCharts(): void {
    this.visitsLineChart?.destroy();
    this.revenueDonutChart?.destroy();
    this.revenueBarChart?.destroy();
    this.visitsLineChart = undefined;
    this.revenueDonutChart = undefined;
    this.revenueBarChart = undefined;
  }

  private getCanvasContext(canvasId: string): CanvasRenderingContext2D | null {
    const canvas = document.getElementById(canvasId) as HTMLCanvasElement | null;
    if (!canvas) {
      return null;
    }
    return canvas.getContext('2d');
  }

  private sum(values: number[]): number {
    return values.reduce((total, value) => total + value, 0);
  }

}
