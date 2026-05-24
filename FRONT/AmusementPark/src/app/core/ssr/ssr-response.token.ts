import { InjectionToken } from '@angular/core';

export interface SsrResponseLike {
  status(code: number): SsrResponseLike;
}

export const SSR_RESPONSE = new InjectionToken<SsrResponseLike | null>('SSR_RESPONSE');
