import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ConfirmDialogComponent } from '../confirm-dialog/confirm-dialog.component';
import { ConnectionStatus, PreviewTarget } from '../models/target.model';
import { TargetService } from '../services/target.service';

const REFRESH_INTERVAL_MS = 15000;

@Component({
  selector: 'app-target-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, ConfirmDialogComponent],
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

  newTargetUrl = '';
  adding = false;

  // the row currently being renamed, and its draft value
  renamingId: string | null = null;
  renameDraft = '';

  // the row whose delete is waiting on the confirmation dialog
  pendingDelete: PreviewTarget | null = null;
  deleting = false;

  private refreshHandle?: ReturnType<typeof setInterval>;

  constructor(private targetService: TargetService) {}

  ngOnInit(): void {
    this.load();
    this.refreshHandle = setInterval(() => this.refresh(), REFRESH_INTERVAL_MS);
  }

  ngOnDestroy(): void {
    if (this.refreshHandle) {
      clearInterval(this.refreshHandle);
    }
  }

  // Skipped, not cancelled, while a row is being edited: a reload replaces the array under
  // the open editor. The next tick picks straight back up.
  get refreshPaused(): boolean {
    return this.renamingId !== null || this.pendingDelete !== null;
  }

  private refresh(): void {
    if (this.refreshPaused) {
      return;
    }

    this.load();
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

  askDelete(target: PreviewTarget): void {
    this.pendingDelete = target;
  }

  cancelDelete(): void {
    this.pendingDelete = null;
  }

  confirmDelete(): void {
    const target = this.pendingDelete;
    if (!target) {
      return;
    }

    this.deleting = true;
    this.targetService.deleteTarget(target.id).subscribe({
      next: () => {
        this.deleting = false;
        this.pendingDelete = null;
        this.load();
      },
      error: (err) => {
        this.deleting = false;
        this.pendingDelete = null;
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

  // Drives the alert banner, scoped to the loaded page; a bigger deployment would ask the API.
  get downTargets(): PreviewTarget[] {
    return this.targets.filter((target) => target.status === ConnectionStatus.Down);
  }

  get downTargetNames(): string {
    return this.downTargets.map((target) => target.name).join(', ');
  }

  statusLabel(status: ConnectionStatus): string {
    return ConnectionStatus[status];
  }
}
