import { DestroyRef, Directive, ElementRef, HostListener, Input, OnDestroy, Renderer2, effect, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';

import { AdminContextualBlockInstance } from '../../models/admin-contextual-block.model';
import { AdminContextualBlockSelectionFacade } from '../../state/admin-contextual-block-selection.facade';
import { AdminPublicViewModeFacade } from '../../state/admin-public-view-mode.facade';

@Directive({
  selector: '[appAdminContextualBlock]',
  standalone: true
})
export class AdminContextualBlockDirective implements OnDestroy {
  private readonly elementRef: ElementRef<HTMLElement> = inject(ElementRef<HTMLElement>);
  private readonly renderer: Renderer2 = inject(Renderer2);
  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private readonly translateService: TranslateService = inject(TranslateService);
  private readonly adminPublicViewModeFacade: AdminPublicViewModeFacade = inject(AdminPublicViewModeFacade);
  private readonly selectionFacade: AdminContextualBlockSelectionFacade = inject(AdminContextualBlockSelectionFacade);

  private block: AdminContextualBlockInstance | null = null;
  private actionButton: HTMLButtonElement | null = null;
  private actionLabel: HTMLSpanElement | null = null;
  private removeActionClickListener: (() => void) | null = null;

  @Input()
  set appAdminContextualBlock(block: AdminContextualBlockInstance | null) {
    this.block = block;
    this.refreshView();
  }

  constructor() {
    effect((): void => {
      this.adminPublicViewModeFacade.editionModeEnabled();
      this.selectionFacade.selectedBlock();
      this.refreshView();
    });

    this.translateService.onLangChange
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((): void => this.refreshActionLabel());
  }

  ngOnDestroy(): void {
    this.removeActionButton();
    this.removeActiveAttributes();
  }

  @HostListener('click', ['$event'])
  onHostClick(event: MouseEvent): void {
    if (!this.canSelectCurrentBlock() || this.isInteractiveTarget(event.target)) {
      return;
    }

    this.selectionFacade.selectBlock(this.block);
  }

  @HostListener('keydown', ['$event'])
  onHostKeydown(event: KeyboardEvent): void {
    if (!this.canSelectCurrentBlock()
      || (event.key !== 'Enter' && event.key !== ' ')
      || this.isInteractiveTarget(event.target)) {
      return;
    }

    event.preventDefault();
    this.selectionFacade.selectBlock(this.block);
  }

  private refreshView(): void {
    if (!this.canSelectCurrentBlock()) {
      this.removeActionButton();
      this.removeActiveAttributes();
      return;
    }

    this.applyActiveAttributes();
    this.ensureActionButton();
    this.refreshActionLabel();
    this.refreshSelectedState();
  }

  private canSelectCurrentBlock(): boolean {
    return this.adminPublicViewModeFacade.editionModeEnabled() && this.block !== null;
  }

  private applyActiveAttributes(): void {
    const host: HTMLElement = this.elementRef.nativeElement;

    this.renderer.addClass(host, 'admin-contextual-block');
    this.renderer.setAttribute(host, 'tabindex', '0');
    this.renderer.setAttribute(host, 'data-admin-contextual-block-type', this.block?.type ?? '');
  }

  private removeActiveAttributes(): void {
    const host: HTMLElement = this.elementRef.nativeElement;

    this.renderer.removeClass(host, 'admin-contextual-block');
    this.renderer.removeClass(host, 'admin-contextual-block--selected');
    this.renderer.removeAttribute(host, 'tabindex');
    this.renderer.removeAttribute(host, 'data-admin-contextual-block-type');
  }

  private refreshSelectedState(): void {
    const selectedBlock: AdminContextualBlockInstance | null = this.selectionFacade.selectedBlock();
    const isSelected: boolean = !!this.block && selectedBlock?.id === this.block.id;

    if (isSelected) {
      this.renderer.addClass(this.elementRef.nativeElement, 'admin-contextual-block--selected');
      return;
    }

    this.renderer.removeClass(this.elementRef.nativeElement, 'admin-contextual-block--selected');
  }

  private ensureActionButton(): void {
    if (this.actionButton) {
      return;
    }

    const button: HTMLButtonElement = this.renderer.createElement('button') as HTMLButtonElement;
    const icon: HTMLElement = this.renderer.createElement('i') as HTMLElement;
    const label: HTMLSpanElement = this.renderer.createElement('span') as HTMLSpanElement;

    this.renderer.setAttribute(button, 'type', 'button');
    this.renderer.addClass(button, 'admin-contextual-block__edit-button');
    this.renderer.addClass(icon, 'pi');
    this.renderer.addClass(icon, 'pi-pencil');
    this.renderer.setAttribute(icon, 'aria-hidden', 'true');
    this.renderer.appendChild(button, icon);
    this.renderer.appendChild(button, label);
    this.renderer.appendChild(this.elementRef.nativeElement, button);

    this.removeActionClickListener = this.renderer.listen(button, 'click', (event: MouseEvent): void => {
      event.preventDefault();
      event.stopPropagation();
      this.selectionFacade.selectBlock(this.block);
    });
    this.actionButton = button;
    this.actionLabel = label;
  }

  private refreshActionLabel(): void {
    if (!this.actionButton || !this.actionLabel) {
      return;
    }

    const translatedLabel: string = this.translateService.instant('admin.contextualBlocks.editAction') as string;
    const label: string = translatedLabel && translatedLabel !== 'admin.contextualBlocks.editAction'
      ? translatedLabel
      : 'Editer';

    this.actionLabel.textContent = label;
    this.renderer.setAttribute(this.actionButton, 'aria-label', label);
  }

  private removeActionButton(): void {
    if (this.removeActionClickListener) {
      this.removeActionClickListener();
      this.removeActionClickListener = null;
    }

    if (this.actionButton) {
      this.renderer.removeChild(this.elementRef.nativeElement, this.actionButton);
      this.actionButton = null;
      this.actionLabel = null;
    }
  }

  private isInteractiveTarget(target: EventTarget | null): boolean {
    if (!(target instanceof HTMLElement)) {
      return false;
    }

    return !!target.closest('a, button, input, textarea, select, summary, [role="button"], [contenteditable="true"]');
  }
}
