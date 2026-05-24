import { Injectable } from '@angular/core';

export interface NaturalTextTruncationOptions {
  maxLength: number;
  ellipsis?: string;
}

@Injectable({ providedIn: 'root' })
export class NaturalTextTruncatorService {
  truncate(value: string | null | undefined, options: NaturalTextTruncationOptions): string | null {
    const normalizedValue: string = (value ?? '').replace(/\s+/g, ' ').trim();
    const maxLength: number = Math.max(0, Math.floor(options.maxLength));
    const ellipsis: string = options.ellipsis ?? '…';

    if (normalizedValue.length === 0) {
      return null;
    }

    if (maxLength === 0) {
      return ellipsis;
    }

    if (normalizedValue.length <= maxLength) {
      return normalizedValue;
    }

    const hardLimit: number = Math.max(0, maxLength - ellipsis.length);
    const candidate: string = normalizedValue.slice(0, hardLimit).trimEnd();
    const naturalBreakpoint: number = Math.max(
      candidate.lastIndexOf(' '),
      candidate.lastIndexOf(','),
      candidate.lastIndexOf(';'),
      candidate.lastIndexOf(':'),
      candidate.lastIndexOf('.'),
      candidate.lastIndexOf('!'),
      candidate.lastIndexOf('?')
    );

    if (naturalBreakpoint >= Math.floor(hardLimit * 0.62)) {
      return `${candidate.slice(0, naturalBreakpoint).trimEnd()}${ellipsis}`;
    }

    return `${candidate}${ellipsis}`;
  }
}
