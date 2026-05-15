import { Injectable } from '@angular/core';
import { MessageService as PrimeNgMessageService } from 'primeng/api';

import { sanitizeDisplayMessage } from '@shared/utils/security';

@Injectable({
  providedIn: 'root'
})
export class ToastMessageService {

  constructor(private primeNgMessageService: PrimeNgMessageService) {}

  add(severity: 'success' | 'info' | 'warn' | 'error', summary: string, detail: string): void {
    this.primeNgMessageService.add({
      severity,
      summary: sanitizeDisplayMessage(summary, 'Information'),
      detail: sanitizeDisplayMessage(detail, severity === 'error' ? 'Une erreur est survenue.' : '')
    });
  }
}
