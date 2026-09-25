import { DOCUMENT } from '@angular/common';
import { Inject, Injectable } from '@angular/core';

import { TripExport } from '@app/models/trips/trip-export.models';

@Injectable({ providedIn: 'root' })
export class TripExportDownloadService {
  constructor(@Inject(DOCUMENT) private readonly document: Document) {
  }

  downloadJson(plan: TripExport): boolean {
    const browserWindow: Window | null = this.document.defaultView;
    if (!browserWindow || typeof URL === 'undefined' || typeof URL.createObjectURL !== 'function') {
      return false;
    }

    const blob = new Blob([JSON.stringify(plan, null, 2)], { type: 'application/json;charset=utf-8' });
    const url: string = URL.createObjectURL(blob);
    const link: HTMLAnchorElement = this.document.createElement('a');
    link.href = url;
    link.download = `${this.toFileName(plan.title)}.json`;
    link.rel = 'noopener';
    this.document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
    return true;
  }

  print(): boolean {
    const browserWindow: Window | null = this.document.defaultView;
    if (!browserWindow) {
      return false;
    }

    browserWindow.print();
    return true;
  }

  private toFileName(title: string): string {
    const normalized: string = title
      .normalize('NFKD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '')
      .slice(0, 80);
    return normalized || 'programme-voyage';
  }
}
