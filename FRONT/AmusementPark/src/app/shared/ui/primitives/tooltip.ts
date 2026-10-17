import {
  AfterContentInit,
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ContentChildren,
  Directive,
  ElementRef,
  EventEmitter,
  forwardRef,
  HostBinding,
  HostListener,
  Input,
  NgModule,
  OnChanges,
  OnDestroy,
  Output,
  QueryList,
  Renderer2,
  TemplateRef,
} from '@angular/core';

import { FormsModule, NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';

import { NgClass, NgFor, NgIf, NgStyle, NgTemplateOutlet } from '@angular/common';

import { TranslateService } from '@ngx-translate/core';

import { UiTemplate } from './ui-template';

type TooltipPosition = 'top' | 'right' | 'bottom' | 'left';

let nextTooltipId = 0;

@Directive({
  selector: '[appUiTooltip]',
  standalone: true
})
export class Tooltip implements OnChanges, OnDestroy {
  @Input('appUiTooltip') text: string | null = null;
  @Input() tooltipPosition: TooltipPosition = 'top';

  private tooltipElement: HTMLElement | null = null;
  private readonly tooltipId = `app-ui-tooltip-${++nextTooltipId}`;

  constructor(
    private readonly elementRef: ElementRef<HTMLElement>,
    private readonly renderer: Renderer2
  ) {
  }

  ngOnChanges(): void {
    if (!this.hasText()) {
      this.hideTooltip();
      return;
    }

    this.renderer.removeAttribute(this.elementRef.nativeElement, 'title');
    if (this.tooltipElement !== null) {
      this.tooltipElement.textContent = this.text?.trim() ?? '';
      this.positionTooltip();
    }
  }

  ngOnDestroy(): void {
    this.hideTooltip();
  }

  @HostListener('mouseenter')
  @HostListener('focusin')
  protected showTooltip(): void {
    if (!this.hasText() || typeof document === 'undefined') {
      return;
    }

    this.renderer.removeAttribute(this.elementRef.nativeElement, 'title');

    if (this.tooltipElement === null) {
      const tooltip = this.renderer.createElement('div') as HTMLElement;
      tooltip.id = this.tooltipId;
      tooltip.setAttribute('role', 'tooltip');
      tooltip.className = 'app-ui-tooltip';
      tooltip.textContent = this.text?.trim() ?? '';
      document.body.appendChild(tooltip);
      this.tooltipElement = tooltip;
      this.renderer.setAttribute(this.elementRef.nativeElement, 'aria-describedby', this.tooltipId);
    }

    this.positionTooltip();
  }

  @HostListener('mouseleave')
  @HostListener('focusout')
  @HostListener('keydown.escape')
  protected hideTooltip(): void {
    if (this.tooltipElement !== null) {
      this.tooltipElement.remove();
      this.tooltipElement = null;
    }

    this.renderer.removeAttribute(this.elementRef.nativeElement, 'aria-describedby');
  }

  private hasText(): boolean {
    return (this.text?.trim().length ?? 0) > 0;
  }

  private positionTooltip(): void {
    if (this.tooltipElement === null || typeof window === 'undefined') {
      return;
    }

    const hostRect = this.elementRef.nativeElement.getBoundingClientRect();
    const tooltipRect = this.tooltipElement.getBoundingClientRect();
    const gap = 8;
    const viewportPadding = 8;
    let top = hostRect.top - tooltipRect.height - gap;
    let left = hostRect.left + (hostRect.width - tooltipRect.width) / 2;

    switch (this.tooltipPosition) {
      case 'bottom':
        top = hostRect.bottom + gap;
        break;
      case 'left':
        top = hostRect.top + (hostRect.height - tooltipRect.height) / 2;
        left = hostRect.left - tooltipRect.width - gap;
        break;
      case 'right':
        top = hostRect.top + (hostRect.height - tooltipRect.height) / 2;
        left = hostRect.right + gap;
        break;
      default:
        break;
    }

    const maxTop = window.innerHeight - tooltipRect.height - viewportPadding;
    const maxLeft = window.innerWidth - tooltipRect.width - viewportPadding;
    const clampedTop = Math.min(Math.max(viewportPadding, top), Math.max(viewportPadding, maxTop));
    const clampedLeft = Math.min(Math.max(viewportPadding, left), Math.max(viewportPadding, maxLeft));

    this.renderer.setStyle(this.tooltipElement, 'top', `${Math.round(clampedTop)}px`);
    this.renderer.setStyle(this.tooltipElement, 'left', `${Math.round(clampedLeft)}px`);
  }
}

export { TooltipModule } from './tooltip-module';
