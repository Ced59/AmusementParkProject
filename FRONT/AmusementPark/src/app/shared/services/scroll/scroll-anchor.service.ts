import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Inject, Injectable, PLATFORM_ID } from '@angular/core';

export interface ScrollAnchorOptions {
  targetSelector?: string | null;
  behavior?: ScrollBehavior;
  block?: ScrollLogicalPosition;
}

@Injectable({
  providedIn: 'root'
})
export class ScrollAnchorService {
  private static readonly paginationTargetSelector: string = '[data-pagination-scroll-target]';
  private static readonly paginationContextSelector: string = '[data-pagination-scroll-context]';

  constructor(
    @Inject(DOCUMENT) private readonly document: Document,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {
  }

  scrollToSelector(selector: string, options: ScrollAnchorOptions = {}): void {
    if (!this.canScroll()) {
      return;
    }

    const target: Element | null = this.document.querySelector(selector);
    if (!(target instanceof HTMLElement)) {
      return;
    }

    this.scrollToElement(target, options);
  }

  scrollToPaginationTarget(hostElement: HTMLElement, options: ScrollAnchorOptions = {}): void {
    if (!this.canScroll()) {
      return;
    }

    const targetElement: HTMLElement = this.resolvePaginationTarget(hostElement, options.targetSelector) ?? hostElement;
    this.scrollToElement(targetElement, options);
  }

  private resolvePaginationTarget(hostElement: HTMLElement, targetSelector?: string | null): HTMLElement | null {
    if (targetSelector) {
      const explicitTarget: Element | null = this.document.querySelector(targetSelector);
      if (explicitTarget instanceof HTMLElement) {
        return explicitTarget;
      }
    }

    const closestTarget: Element | null = hostElement.closest(ScrollAnchorService.paginationTargetSelector);
    if (closestTarget instanceof HTMLElement) {
      return closestTarget;
    }

    const closestContext: Element | null = hostElement.closest(ScrollAnchorService.paginationContextSelector);
    const contextualTarget: Element | null =
      closestContext?.querySelector(ScrollAnchorService.paginationTargetSelector) ?? null;

    return contextualTarget instanceof HTMLElement ? contextualTarget : null;
  }

  private scrollToElement(element: HTMLElement, options: ScrollAnchorOptions): void {
    if (typeof element.scrollIntoView !== 'function') {
      return;
    }

    element.scrollIntoView({
      behavior: options.behavior ?? 'smooth',
      block: options.block ?? 'start',
      inline: 'nearest'
    });
  }

  private canScroll(): boolean {
    return isPlatformBrowser(this.platformId);
  }
}
