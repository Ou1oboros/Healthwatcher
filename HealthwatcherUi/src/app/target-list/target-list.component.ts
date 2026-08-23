import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ConnectionStatus, PreviewTarget } from '../models/target.model';
import { TargetService } from '../services/target.service';

const REFRESH_INTERVAL_MS = 15000;

@Component({
  selector: 'app-target-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './target-list.component.html',
  styleUrl: './target-list.component.css',
})
export class TargetListComponent implements OnInit, OnDestroy {
  readonly ConnectionStatus = ConnectionStatus;

  targets: PreviewTarget[] = [];
  totalCount = 0;
  pageIndex = 1;
  pageSize = 20;
  totalPages = 0;
  loading = false;
  errorMessage = '';

  // bound to the "add target" form via ngModel
  newTargetUrl = '';
  adding = false;

  // id of the row currently being renamed, and the ngModel-bound draft value
  renamingId: string | null = null;
  renameDraft = '';

  private refreshHandle?: ReturnType<typeof setInterval>;

  constructor(private targetService: TargetService) {}

  ngOnInit(): void {
    this.load();
    this.refreshHandle = setInterval(() => this.load(), REFRESH_INTERVAL_MS);
  }

  ngOnDestroy(): void {
    if (this.refreshHandle) {
      clearInterval(this.refreshHandle);
    }
  }

  load(): void {
    this.loading = true;
    this.targetService.getTargets(this.pageIndex, this.pageSize).subscribe({
      next: (result) => {
        this.targets = result.items;
        this.totalCount = result.totalCount;
        this.totalPages = result.totalPages;
        this.loading = false;
        this.errorMessage = '';
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = 'Could not load targets. Is the API running?';
        console.error(err);
      },
    });
  }

  addTarget(): void {
    if (!this.newTargetUrl.trim()) {
      return;
    }

    this.adding = true;
    this.targetService.addTarget(this.newTargetUrl.trim()).subscribe({
      next: () => {
        this.newTargetUrl = '';
        this.adding = false;
        this.load();
      },
      error: (err) => {
        this.adding = false;
        this.errorMessage = err?.error?.message ?? 'Could not add target.';
      },
    });
  }

  startRename(target: PreviewTarget): void {
    this.renamingId = target.id;
    this.renameDraft = target.name;
  }

  cancelRename(): void {
    this.renamingId = null;
    this.renameDraft = '';
  }

  confirmRename(target: PreviewTarget): void {
    if (!this.renameDraft.trim()) {
      return;
    }

    this.targetService.renameTarget(target.id, this.renameDraft.trim()).subscribe({
      next: () => {
        this.cancelRename();
        this.load();
      },
      error: (err) => {
        this.errorMessage = err?.error?.message ?? 'Could not rename target.';
      },
    });
  }

  deleteTarget(target: PreviewTarget): void {
    if (!confirm(`Delete "${target.name}"?`)) {
      return;
    }

    this.targetService.deleteTarget(target.id).subscribe({
      next: () => this.load(),
      error: (err) => {
        this.errorMessage = err?.error?.message ?? 'Could not delete target.';
      },
    });
  }

  goToPage(pageIndex: number): void {
    if (pageIndex < 1 || pageIndex > this.totalPages) {
      return;
    }
    this.pageIndex = pageIndex;
    this.load();
  }

  statusLabel(status: ConnectionStatus): string {
    return ConnectionStatus[status];
  }
}
