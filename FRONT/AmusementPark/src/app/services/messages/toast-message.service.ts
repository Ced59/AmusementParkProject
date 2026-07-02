import { Injectable } from '@angular/core';
import { MessageService } from '@shared/ui/primitives/api';

import { sanitizeDisplayMessage } from '@shared/utils/security';

@Injectable({
  providedIn: 'root'
})
export class ToastMessageService {

  constructor(private readonly messageService: MessageService) {}

  add(severity: 'success' | 'info' | 'warn' | 'error', summary: string, detail: string): void {
    this.messageService.add({
      severity,
      summary: sanitizeDisplayMessage(summary, 'Information'),
      detail: sanitizeDisplayMessage(detail, severity === 'error' ? 'Une erreur est survenue.' : '')
    });
  }
}
