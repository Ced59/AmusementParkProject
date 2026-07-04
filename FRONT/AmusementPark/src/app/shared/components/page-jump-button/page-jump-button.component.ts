import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, HostListener, Inject, OnDestroy, PLATFORM_ID } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-page-jump-button',
  templateUrl: './page-jump-button.component.html',
  styleUrls: ['./page-jump-button.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule]
})
export class PageJumpButtonComponent implements AfterViewInit, OnDestroy {
  private static readonly scrollableThresholdPixels: number = 160;
  private static readonly nearBottomThresholdPixels: number = 140;

  protected isVisible: boolean = false;
  protected isNearPageBottom: boolean = false;

  private animationFrameId: number | null = null;
  private resizeObserver: ResizeObserver | null = null;

  constructor(
    private readonly router: Router,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly destroyRef: DestroyRef,
    @Inject(DOCUMENT) private readonly document: Document,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {
  }

  ngAfterViewInit(): void {
    if (!this.canUseDom()) {
      return;
    }

    this.installResizeObserver();
    this.router.events.pipe(
      filter((event: unknown): event is NavigationEnd => event instanceof NavigationEnd),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((): void => this.scheduleStateUpdate());

    this.scheduleStateUpdate();
  }

  ngOnDestroy(): void {
    const defaultView: Window | null = this.document.defaultView;
    if (defaultView && this.animationFrameId !== null) {
      defaultView.cancelAnimationFrame(this.animationFrameId);
      this.animationFrameId = null;
    }

    this.resizeObserver?.disconnect();
    this.resizeObserver = null;
  }

  @HostListener('window:scroll')
  @HostListener('window:resize')
  protected onViewportChanged(): void {
    this.scheduleStateUpdate();
  }

  protected get labelKey(): string {
    return this.isNearPageBottom ? 'actions.scrollTop' : 'actions.scrollBottom';
  }

  protected togglePageScroll(): void {
    if (!this.canUseDom()) {
      return;
    }

    const defaultView: Window | null = this.document.defaultView;
    if (!defaultView) {
      return;
    }

    const scrollingElement: HTMLElement = this.resolveScrollingElement();
    const targetTop: number = this.isNearPageBottom ? 0 : scrollingElement.scrollHeight;
    defaultView.scrollTo({
      top: targetTop,
      behavior: 'smooth'
    });
  }

  private installResizeObserver(): void {
    const defaultView: Window | null = this.document.defaultView;
    if (!defaultView || typeof ResizeObserver === 'undefined' || !this.document.body) {
      return;
    }

    const resizeObserver: ResizeObserver = new ResizeObserver((): void => this.scheduleStateUpdate());
    resizeObserver.observe(this.document.documentElement);
    resizeObserver.observe(this.document.body);
    this.resizeObserver = resizeObserver;
  }

  private scheduleStateUpdate(): void {
    if (!this.canUseDom()) {
      return;
    }

    const defaultView: Window | null = this.document.defaultView;
    if (!defaultView || this.animationFrameId !== null) {
      return;
    }

    this.animationFrameId = defaultView.requestAnimationFrame((): void => {
      this.animationFrameId = null;
      this.updatePageScrollState();
    });
  }

  private updatePageScrollState(): void {
    const defaultView: Window | null = this.document.defaultView;
    if (!defaultView) {
      return;
    }

    const scrollingElement: HTMLElement = this.resolveScrollingElement();
    const scrollHeight: number = Math.max(scrollingElement.scrollHeight, this.document.body?.scrollHeight ?? 0);
    const viewportHeight: number = defaultView.innerHeight;
    const viewportBottom: number = defaultView.scrollY + viewportHeight;
    const nextIsVisible: boolean = scrollHeight - viewportHeight > PageJumpButtonComponent.scrollableThresholdPixels;
    const nextIsNearPageBottom: boolean = scrollHeight - viewportBottom < PageJumpButtonComponent.nearBottomThresholdPixels;

    if (nextIsVisible === this.isVisible && nextIsNearPageBottom === this.isNearPageBottom) {
      return;
    }

    this.isVisible = nextIsVisible;
    this.isNearPageBottom = nextIsNearPageBottom;
    this.changeDetectorRef.markForCheck();
  }

  private resolveScrollingElement(): HTMLElement {
    const scrollingElement: Element | null = this.document.scrollingElement;
    if (scrollingElement instanceof HTMLElement) {
      return scrollingElement;
    }

    return this.document.documentElement;
  }

  private canUseDom(): boolean {
    return isPlatformBrowser(this.platformId);
  }
}
