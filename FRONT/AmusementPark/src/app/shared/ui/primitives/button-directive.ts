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

@Directive({
  selector: 'button[appUiButton], a[appUiButton]',
  standalone: true
})
export class ButtonDirective implements OnChanges, AfterViewInit, OnDestroy {
  @Input() label: string | null = null;
  @Input() icon: string | null = null;
  @Input() iconPos: 'left' | 'right' | 'top' | 'bottom' = 'left';
  @Input() severity: string | null = null;
  @Input() styleClass: string | null = null;
  @Input() text: boolean | string = false;
  @Input() outlined: boolean | string = false;
  @Input() rounded: boolean | string = false;
  @Input() link: boolean | string = false;
  @Input() loading: boolean = false;

  @HostBinding('class.p-button') protected readonly buttonClass: boolean = true;
  @HostBinding('class.app-compatible-button') protected readonly appButtonClass: boolean = true;
  @HostBinding('class.p-button-loading') protected get isLoading(): boolean {
    return this.loading;
  }
  @HostBinding('class.p-button-text') protected get isText(): boolean {
    return this.asBoolean(this.text);
  }
  @HostBinding('class.p-button-outlined') protected get isOutlined(): boolean {
    return this.asBoolean(this.outlined);
  }
  @HostBinding('class.p-button-rounded') protected get isRounded(): boolean {
    return this.asBoolean(this.rounded);
  }
  @HostBinding('class.p-button-link') protected get isLink(): boolean {
    return this.asBoolean(this.link);
  }
  @HostBinding('class.p-button-secondary') protected get isSecondary(): boolean {
    return this.severity === 'secondary';
  }
  @HostBinding('class.p-button-success') protected get isSuccess(): boolean {
    return this.severity === 'success';
  }
  @HostBinding('class.p-button-info') protected get isInfo(): boolean {
    return this.severity === 'info';
  }
  @HostBinding('class.p-button-warning') protected get isWarning(): boolean {
    return this.severity === 'warn' || this.severity === 'warning';
  }
  @HostBinding('class.p-button-danger') protected get isDanger(): boolean {
    return this.severity === 'danger';
  }
  @HostBinding('class.p-button-help') protected get isHelp(): boolean {
    return this.severity === 'help';
  }

  private hasView: boolean = false;
  private isDestroyed: boolean = false;
  private renderScheduled: boolean = false;
  private readonly generatedNodes: Node[] = [];

  constructor(
    private readonly elementRef: ElementRef<HTMLElement>,
    private readonly renderer: Renderer2
  ) {
  }

  ngAfterViewInit(): void {
    this.hasView = true;
    this.renderContent();
  }

  ngOnChanges(): void {
    if (this.hasView) {
      this.scheduleRender();
    }
  }

  ngOnDestroy(): void {
    this.isDestroyed = true;
    this.renderScheduled = false;
    this.generatedNodes.length = 0;
  }

  private scheduleRender(): void {
    if (this.renderScheduled) {
      return;
    }

    this.renderScheduled = true;
    void Promise.resolve().then((): void => {
      this.renderScheduled = false;

      if (!this.isDestroyed) {
        this.renderContent();
      }
    });
  }

  private renderContent(): void {
    const host: HTMLElement = this.elementRef.nativeElement;

    this.clearGeneratedContent(host);

    if (!this.label && !this.icon && !this.loading) {
      return;
    }

    const resolvedIcon: string | null = this.loading ? 'pi pi-spin pi-spinner' : this.icon;
    if (resolvedIcon && (this.iconPos === 'left' || this.iconPos === 'top')) {
      this.appendIcon(host, resolvedIcon);
    }

    if (this.label) {
      const labelElement: HTMLElement = this.renderer.createElement('span');
      this.renderer.addClass(labelElement, 'p-button-label');
      this.renderer.appendChild(labelElement, this.renderer.createText(this.label));
      this.renderer.appendChild(host, labelElement);
      this.generatedNodes.push(labelElement);
    }

    if (resolvedIcon && (this.iconPos === 'right' || this.iconPos === 'bottom')) {
      this.appendIcon(host, resolvedIcon);
    }
  }

  private appendIcon(host: HTMLElement, icon: string): void {
    const iconElement: HTMLElement = this.renderer.createElement('span');
    this.renderer.addClass(iconElement, 'p-button-icon');
    this.renderer.addClass(iconElement, 'app-button-icon');
    for (const className of icon.split(' ').filter((value: string) => value.length > 0)) {
      this.renderer.addClass(iconElement, className);
    }
    this.renderer.setAttribute(iconElement, 'aria-hidden', 'true');
    this.renderer.appendChild(host, iconElement);
    this.generatedNodes.push(iconElement);
  }

  private clearGeneratedContent(host: HTMLElement): void {
    while (this.generatedNodes.length > 0) {
      const node: Node | undefined = this.generatedNodes.pop();
      if (node?.parentNode === host) {
        this.renderer.removeChild(host, node);
      }
    }
  }

  private asBoolean(value: boolean | string): boolean {
    return value === true || value === '';
  }
}
