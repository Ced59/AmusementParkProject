import { Injectable } from '@angular/core';
import { MessageService as PrimeNgMessageService } from 'primeng/api';

@Injectable({
  providedIn: 'root'
})
export class ToastMessageService {

  constructor(private primeNgMessageService: PrimeNgMessageService) {}

  add(severity: 'success' | 'info' | 'warn' | 'error', summary: string, detail: string) {
    this.primeNgMessageService.add({ severity, summary, detail });
  }
}
