import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  Output,
  ViewChild,
} from '@angular/core';

// Replaces window.confirm(), which blocks the tab and cannot be styled or tested.
// It decides nothing itself: the parent renders it and acts on the answer.
@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.css',
})
export class ConfirmDialogComponent implements AfterViewInit {
  @Input() title = 'Are you sure?';
  @Input() message = '';
  @Input() confirmLabel = 'Confirm';
  @Input() busy = false;

  @Output() confirmed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  @ViewChild('confirmButton') private confirmButton?: ElementRef<HTMLButtonElement>;

  // The dialog is only in the DOM while open, so focusing once on init is enough.
  ngAfterViewInit(): void {
    this.confirmButton?.nativeElement.focus();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.cancelled.emit();
  }
}
