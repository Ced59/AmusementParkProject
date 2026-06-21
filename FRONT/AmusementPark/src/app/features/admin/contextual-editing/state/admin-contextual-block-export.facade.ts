import { DOCUMENT } from '@angular/common';
import { HttpResponse } from '@angular/common/http';
import { Inject, Injectable, Signal, signal } from '@angular/core';
import { finalize } from 'rxjs';

import { AdminContextualBlockInstance } from '../models/admin-contextual-block.model';
import {
  ADMIN_CONTEXTUAL_BLOCK_EXPORT_DATA_PORT,
  AdminContextualBlockExportDataPort
} from './admin-contextual-block-export-data.ports';

@Injectable({
  providedIn: 'root'
})
export class AdminContextualBlockExportFacade {
  private readonly isExportingSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);

  public readonly isExporting: Signal<boolean> = this.isExportingSignal.asReadonly();
  public readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();

  constructor(
    @Inject(ADMIN_CONTEXTUAL_BLOCK_EXPORT_DATA_PORT) private readonly contextualBlocksApi: AdminContextualBlockExportDataPort,
    @Inject(DOCUMENT) private readonly document: Document
  ) {
  }

  canExport(block: AdminContextualBlockInstance | null): boolean {
    return Boolean(block?.capabilities.includes('boundedJsonExport'));
  }

  exportBlock(block: AdminContextualBlockInstance): void {
    this.errorKeySignal.set(null);

    if (!this.canExport(block)) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.downloadJsonUnavailable');
      return;
    }

    if (!block.entityId.trim()) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.downloadJsonError');
      return;
    }

    this.isExportingSignal.set(true);
    this.contextualBlocksApi.downloadBlockExport(block.type, block.entityId)
      .pipe(finalize((): void => this.isExportingSignal.set(false)))
      .subscribe({
        next: (response: HttpResponse<Blob>): void => {
          if (!response.body) {
            this.errorKeySignal.set('admin.contextualBlocks.drawer.downloadJsonError');
            return;
          }

          this.downloadBlob(response.body, this.resolveDownloadFileName(response, block));
        },
        error: (): void => {
          this.errorKeySignal.set('admin.contextualBlocks.drawer.downloadJsonError');
        }
      });
  }

  private downloadBlob(blob: Blob, fileName: string): void {
    const defaultView: Window | null = this.document.defaultView;
    if (!defaultView || typeof URL === 'undefined' || !URL.createObjectURL) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.downloadJsonError');
      return;
    }

    const objectUrl: string = URL.createObjectURL(blob);
    const anchor: HTMLAnchorElement = this.document.createElement('a');
    anchor.href = objectUrl;
    anchor.download = fileName;
    anchor.rel = 'noopener';
    this.document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(objectUrl);
  }

  private resolveDownloadFileName(response: HttpResponse<Blob>, block: AdminContextualBlockInstance): string {
    const contentDisposition: string = response.headers.get('content-disposition') ?? '';
    const utf8Match: RegExpMatchArray | null = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);
    if (utf8Match?.[1]) {
      return decodeURIComponent(utf8Match[1].replace(/"/g, ''));
    }

    const fallbackMatch: RegExpMatchArray | null = contentDisposition.match(/filename="?([^";]+)"?/i);
    if (fallbackMatch?.[1]) {
      return fallbackMatch[1];
    }

    return `${block.type.replace(/\./g, '-')}-${block.entityId}-contextual-block.json`;
  }
}
