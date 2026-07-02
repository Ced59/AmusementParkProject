import { ChangeDetectionStrategy, ChangeDetectorRef, Component, HostBinding, Input, OnDestroy } from '@angular/core';
import { NgFor } from '@angular/common';
import { MessageService, ToastMessage } from './api';

@Component({
  selector: 'app-ui-toast',
  standalone: true,
  imports: [NgFor],
  template: `
    <div
      *ngFor="let message of messages"
      class="p-toast-message"
      [class.p-toast-message-success]="message.severity === 'success'"
      [class.p-toast-message-info]="message.severity === 'info'"
      [class.p-toast-message-warn]="message.severity === 'warn'"
      [class.p-toast-message-error]="message.severity === 'error'"
      [attr.role]="resolveMessageRole(message)"
      [attr.aria-live]="resolveMessageLiveRegion(message)"
      aria-atomic="true">
      <strong class="p-toast-message__summary">{{ message.summary }}</strong>
      <span class="p-toast-message__detail">{{ message.detail }}</span>
      <button class="p-toast-message__close" type="button" (click)="dismiss(message)" aria-label="Close">
        <span aria-hidden="true">&times;</span>
      </button>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Toast implements OnDestroy {
  @Input() position: string = 'bottom-right';
  messages: ToastMessage[] = [];
  private readonly timers: Map<number, ReturnType<typeof setTimeout>> = new Map<number, ReturnType<typeof setTimeout>>();
  private readonly subscription = this.messageService.messages$.subscribe((messages: ToastMessage[]): void => {
    this.messages = messages;
    this.changeDetectorRef.markForCheck();

    const activeIds: Set<number> = new Set<number>();
    for (const message of messages) {
      if (message.id) {
        activeIds.add(message.id);
        this.scheduleDismiss(message.id);
      }
    }

    for (const [id, timer] of this.timers) {
      if (!activeIds.has(id)) {
        clearTimeout(timer);
        this.timers.delete(id);
      }
    }
  });

  @HostBinding('class') protected get hostClasses(): string {
    return `p-toast p-toast-${this.position}`;
  }

  constructor(
    private readonly messageService: MessageService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
    for (const timer of this.timers.values()) {
      clearTimeout(timer);
    }
    this.timers.clear();
  }

  dismiss(message: ToastMessage): void {
    if (message.id) {
      this.messageService.remove(message.id);
    }
  }

  protected resolveMessageRole(message: ToastMessage): 'alert' | 'status' {
    return message.severity === 'error' ? 'alert' : 'status';
  }

  protected resolveMessageLiveRegion(message: ToastMessage): 'assertive' | 'polite' {
    return message.severity === 'error' ? 'assertive' : 'polite';
  }

  private scheduleDismiss(id: number): void {
    if (this.timers.has(id) || typeof setTimeout !== 'function') {
      return;
    }

    const timeoutId: ReturnType<typeof setTimeout> = setTimeout((): void => {
      this.timers.delete(id);
      this.messageService.remove(id);
    }, 5000);
    this.timers.set(id, timeoutId);
  }
}
