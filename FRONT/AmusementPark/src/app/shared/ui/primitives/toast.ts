import { ChangeDetectionStrategy, Component, HostBinding, Input, OnDestroy } from '@angular/core';
import { NgFor } from '@angular/common';
import { MessageService, ToastMessage } from './api';

@Component({
  selector: 'app-ui-toast',
  standalone: true,
  imports: [NgFor],
  template: `
    <div *ngFor="let message of messages" class="p-toast-message" [class.p-toast-message-success]="message.severity === 'success'" [class.p-toast-message-info]="message.severity === 'info'" [class.p-toast-message-warn]="message.severity === 'warn'" [class.p-toast-message-error]="message.severity === 'error'">
      <strong>{{ message.summary }}</strong>
      <span>{{ message.detail }}</span>
      <button type="button" (click)="dismiss(message)" aria-label="Close"><span class="pi pi-times" aria-hidden="true"></span></button>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Toast implements OnDestroy {
  @Input() position: string = 'bottom-right';
  messages: ToastMessage[] = [];
  private readonly timers: number[] = [];
  private readonly subscription = this.messageService.messages$.subscribe((messages: ToastMessage[]): void => {
    this.messages = messages;
    const latestMessage: ToastMessage | undefined = messages[messages.length - 1];
    if (latestMessage?.id) {
      const timeoutId: number = window.setTimeout((): void => this.messageService.remove(latestMessage.id ?? 0), 5000);
      this.timers.push(timeoutId);
    }
  });

  @HostBinding('class') protected get hostClasses(): string {
    return `p-toast p-toast-${this.position}`;
  }

  constructor(private readonly messageService: MessageService) {
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
    for (const timer of this.timers) {
      window.clearTimeout(timer);
    }
  }

  dismiss(message: ToastMessage): void {
    if (message.id) {
      this.messageService.remove(message.id);
    }
  }
}
