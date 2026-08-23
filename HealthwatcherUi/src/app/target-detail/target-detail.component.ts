import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ConnectionStatus, Target, TargetHistoryEntry, TargetUptime } from '../models/target.model';
import { TargetService } from '../services/target.service';

const REFRESH_INTERVAL_MS = 15000;

// SVG viewBox units, not pixels - the element is stretched to its CSS size.
const CHART_WIDTH = 600;
const CHART_HEIGHT = 160;
const CHART_PADDING = 10;

interface ChartPoint {
  x: number;
  y: number;
  up: boolean;
}

interface ChartData {
  points: string;
  dots: ChartPoint[];
  maxMs: number;
}

@Component({
  selector: 'app-target-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './target-detail.component.html',
  styleUrl: './target-detail.component.css',
})
export class TargetDetailComponent implements OnInit, OnDestroy {
  readonly ConnectionStatus = ConnectionStatus;

  targetId = '';
  target: Target | null = null;
  history: TargetHistoryEntry[] = [];
  uptime: TargetUptime | null = null;
  windowHours = 24;
  errorMessage = '';

  renaming = false;
  renameDraft = '';

  readonly chartWidth = CHART_WIDTH;
  readonly chartHeight = CHART_HEIGHT;

  private refreshHandle?: ReturnType<typeof setInterval>;

  constructor(
    private route: ActivatedRoute,
    private targetService: TargetService
  ) {}

  ngOnInit(): void {
    this.targetId = this.route.snapshot.paramMap.get('id') ?? '';
    this.load();
    this.refreshHandle = setInterval(() => this.load(), REFRESH_INTERVAL_MS);
  }

  ngOnDestroy(): void {
    if (this.refreshHandle) {
      clearInterval(this.refreshHandle);
    }
  }

  load(): void {
    this.targetService.getTargetById(this.targetId).subscribe({
      next: (target) => (this.target = target),
      error: (err) => {
        this.errorMessage = 'Could not load target.';
        console.error(err);
      },
    });

    this.targetService.getTargetHistory(this.targetId, 1, 20).subscribe({
      next: (result) => (this.history = result.items),
      error: (err) => console.error(err),
    });

    this.loadUptime();
  }

  loadUptime(): void {
    this.targetService.getTargetUptime(this.targetId, this.windowHours).subscribe({
      next: (uptime) => (this.uptime = uptime),
      error: (err) => console.error(err),
    });
  }

  startRename(): void {
    if (!this.target) return;
    this.renaming = true;
    this.renameDraft = this.target.name;
  }

  cancelRename(): void {
    this.renaming = false;
    this.renameDraft = '';
  }

  confirmRename(): void {
    if (!this.renameDraft.trim()) return;

    this.targetService.renameTarget(this.targetId, this.renameDraft.trim()).subscribe({
      next: () => {
        this.renaming = false;
        this.load();
      },
      error: (err) => {
        this.errorMessage = err?.error?.message ?? 'Could not rename target.';
      },
    });
  }

  statusLabel(status: ConnectionStatus): string {
    return ConnectionStatus[status];
  }

  // history comes back newest-first (for the table); the chart reads left-to-right
  // as oldest-to-newest, so it's reversed here rather than re-fetched.
  get chartData(): ChartData | null {
    const chronological = [...this.history]
      .reverse()
      .filter((entry) => entry.healthCheckRecord.responseTimeMs !== null);

    if (chronological.length < 2) {
      return null;
    }

    const maxMs = Math.max(...chronological.map((e) => e.healthCheckRecord.responseTimeMs!), 1);
    const plotWidth = CHART_WIDTH - CHART_PADDING * 2;
    const plotHeight = CHART_HEIGHT - CHART_PADDING * 2;
    const stepX = plotWidth / (chronological.length - 1);

    const dots: ChartPoint[] = chronological.map((entry, i) => ({
      x: CHART_PADDING + i * stepX,
      y: CHART_HEIGHT - CHART_PADDING - (entry.healthCheckRecord.responseTimeMs! / maxMs) * plotHeight,
      up: entry.healthCheckRecord.status === ConnectionStatus.Up,
    }));

    return {
      dots,
      maxMs,
      points: dots.map((d) => `${d.x.toFixed(1)},${d.y.toFixed(1)}`).join(' '),
    };
  }
}
