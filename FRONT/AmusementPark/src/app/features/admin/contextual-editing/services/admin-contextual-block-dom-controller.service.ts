import { DOCUMENT } from '@angular/common';
import { EffectRef, Injectable, Injector, Renderer2, RendererFactory2, effect, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';

import {
  PublicContextualBlockMarker,
  PublicContextualBlockType
} from '@features/public/contextual-editing/models/public-contextual-block-marker.model';
import { PublicContextualBlockMarkerRegistry } from '@features/public/contextual-editing/state/public-contextual-block-marker.registry';
import { AdminContextualBlockInstance, AdminContextualBlockType } from '../models/admin-contextual-block.model';
import { AdminContextualBlockSelectionFacade } from '../state/admin-contextual-block-selection.facade';
import { AdminPublicViewModeFacade } from '../state/admin-public-view-mode.facade';
import { AdminContextualBlockRegistryService } from './admin-contextual-block-registry.service';

interface AdminContextualDomMarkerState {
  block: AdminContextualBlockInstance;
  actionButton: HTMLButtonElement;
  actionLabel: HTMLSpanElement;
  hadTabIndex: boolean;
  previousTabIndex: string | null;
  removeActionClickListener: () => void;
}

@Injectable({
  providedIn: 'root'
})
export class AdminContextualBlockDomControllerService {
  private readonly document: Document = inject(DOCUMENT);
  private readonly injector: Injector = inject(Injector);
  private readonly renderer: Renderer2 = inject(RendererFactory2).createRenderer(null, null);
  private readonly translateService: TranslateService = inject(TranslateService);
  private readonly markerRegistry: PublicContextualBlockMarkerRegistry = inject(PublicContextualBlockMarkerRegistry);
  private readonly contextualBlockRegistry: AdminContextualBlockRegistryService = inject(AdminContextualBlockRegistryService);
  private readonly selectionFacade: AdminContextualBlockSelectionFacade = inject(AdminContextualBlockSelectionFacade);
  private readonly adminPublicViewModeFacade: AdminPublicViewModeFacade = inject(AdminPublicViewModeFacade);

  private readonly activeMarkers = new Map<HTMLElement, AdminContextualDomMarkerState>();

  private modeEffect: EffectRef | null = null;
  private mutationObserver: MutationObserver | null = null;
  private removeDocumentClickListener: (() => void) | null = null;
  private removeDocumentKeydownListener: (() => void) | null = null;
  private languageSubscription: Subscription | null = null;
  private isStarted: boolean = false;

  start(): void {
    if (this.isStarted) {
      return;
    }

    this.isStarted = true;
    this.removeDocumentClickListener = this.renderer.listen(this.document, 'click', (event: MouseEvent): void => this.onDocumentClick(event));
    this.removeDocumentKeydownListener = this.renderer.listen(this.document, 'keydown', (event: KeyboardEvent): void => this.onDocumentKeydown(event));
    this.languageSubscription = this.translateService.onLangChange.subscribe((): void => this.refreshActionLabels());
    this.startMutationObserver();
    this.modeEffect = effect((): void => {
      this.adminPublicViewModeFacade.editionModeEnabled();
      this.selectionFacade.selectedBlock();
      this.refreshMarkers();
    }, { injector: this.injector });
    this.refreshMarkers();
  }

  stop(): void {
    if (!this.isStarted) {
      return;
    }

    this.isStarted = false;
    this.modeEffect?.destroy();
    this.modeEffect = null;
    this.mutationObserver?.disconnect();
    this.mutationObserver = null;
    this.removeDocumentClickListener?.();
    this.removeDocumentClickListener = null;
    this.removeDocumentKeydownListener?.();
    this.removeDocumentKeydownListener = null;
    this.languageSubscription?.unsubscribe();
    this.languageSubscription = null;
    this.deactivateAllMarkers();
  }

  private startMutationObserver(): void {
    if (!this.document.body || typeof MutationObserver === 'undefined') {
      return;
    }

    const observer = new MutationObserver((): void => this.refreshMarkers());
    observer.observe(this.document.body, { childList: true, subtree: true });
    this.mutationObserver = observer;
  }

  private refreshMarkers(): void {
    if (!this.adminPublicViewModeFacade.editionModeEnabled()) {
      this.deactivateAllMarkers();
      return;
    }

    const markerElements = Array.from(
      this.document.querySelectorAll<HTMLElement>('[data-admin-contextual-block-marker-id]')
    );
    const currentElements = new Set<HTMLElement>(markerElements);

    markerElements.forEach((element: HTMLElement): void => {
      const block: AdminContextualBlockInstance | null = this.resolveBlockFromElement(element);

      if (!block) {
        this.deactivateMarker(element);
        return;
      }

      this.activateMarker(element, block);
    });

    Array.from(this.activeMarkers.keys())
      .filter((element: HTMLElement): boolean => !currentElements.has(element))
      .forEach((element: HTMLElement): void => this.deactivateMarker(element));
  }

  private activateMarker(element: HTMLElement, block: AdminContextualBlockInstance): void {
    const existingState: AdminContextualDomMarkerState | undefined = this.activeMarkers.get(element);

    if (existingState) {
      existingState.block = block;
      this.refreshActionLabel(existingState);
      this.refreshSelectedState(element, existingState);
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
    this.renderer.appendChild(element, button);

    const state: AdminContextualDomMarkerState = {
      block,
      actionButton: button,
      actionLabel: label,
      hadTabIndex: element.hasAttribute('tabindex'),
      previousTabIndex: element.getAttribute('tabindex'),
      removeActionClickListener: this.renderer.listen(button, 'click', (event: MouseEvent): void => {
        event.preventDefault();
        event.stopPropagation();
        this.selectionFacade.selectBlock(state.block);
      })
    };

    this.renderer.addClass(element, 'admin-contextual-block');
    this.renderer.setAttribute(element, 'tabindex', '0');
    this.activeMarkers.set(element, state);
    this.refreshActionLabel(state);
    this.refreshSelectedState(element, state);
  }

  private deactivateMarker(element: HTMLElement): void {
    const state: AdminContextualDomMarkerState | undefined = this.activeMarkers.get(element);

    if (!state) {
      return;
    }

    state.removeActionClickListener();
    this.renderer.removeChild(element, state.actionButton);
    this.renderer.removeClass(element, 'admin-contextual-block');
    this.renderer.removeClass(element, 'admin-contextual-block--selected');

    if (state.hadTabIndex && state.previousTabIndex !== null) {
      this.renderer.setAttribute(element, 'tabindex', state.previousTabIndex);
    } else {
      this.renderer.removeAttribute(element, 'tabindex');
    }

    this.activeMarkers.delete(element);
  }

  private deactivateAllMarkers(): void {
    Array.from(this.activeMarkers.keys()).forEach((element: HTMLElement): void => this.deactivateMarker(element));
  }

  private refreshSelectedState(element: HTMLElement, state: AdminContextualDomMarkerState): void {
    const selectedBlock: AdminContextualBlockInstance | null = this.selectionFacade.selectedBlock();

    if (selectedBlock?.id === state.block.id) {
      this.renderer.addClass(element, 'admin-contextual-block--selected');
      return;
    }

    this.renderer.removeClass(element, 'admin-contextual-block--selected');
  }

  private refreshActionLabels(): void {
    this.activeMarkers.forEach((state: AdminContextualDomMarkerState): void => this.refreshActionLabel(state));
  }

  private refreshActionLabel(state: AdminContextualDomMarkerState): void {
    const translatedLabel: string = this.translateService.instant('admin.contextualBlocks.editAction') as string;
    const label: string = translatedLabel && translatedLabel !== 'admin.contextualBlocks.editAction'
      ? translatedLabel
      : 'Éditer';

    state.actionLabel.textContent = label;
    this.renderer.setAttribute(state.actionButton, 'aria-label', label);
  }

  private onDocumentClick(event: MouseEvent): void {
    if (!this.adminPublicViewModeFacade.editionModeEnabled() || this.isInteractiveTarget(event.target)) {
      return;
    }

    const markerElement: HTMLElement | null = this.findMarkerElement(event.target);
    const state: AdminContextualDomMarkerState | undefined = markerElement
      ? this.activeMarkers.get(markerElement)
      : undefined;

    if (state) {
      this.selectionFacade.selectBlock(state.block);
    }
  }

  private onDocumentKeydown(event: KeyboardEvent): void {
    if (!this.adminPublicViewModeFacade.editionModeEnabled()
      || (event.key !== 'Enter' && event.key !== ' ')
      || this.isInteractiveTarget(event.target)) {
      return;
    }

    const markerElement: HTMLElement | null = this.findMarkerElement(event.target);
    const state: AdminContextualDomMarkerState | undefined = markerElement
      ? this.activeMarkers.get(markerElement)
      : undefined;

    if (!state) {
      return;
    }

    event.preventDefault();
    this.selectionFacade.selectBlock(state.block);
  }

  private findMarkerElement(target: EventTarget | null): HTMLElement | null {
    const element: HTMLElement | null = this.findNearestElement(target);

    return element?.closest<HTMLElement>('[data-admin-contextual-block-marker-id]') ?? null;
  }

  private isInteractiveTarget(target: EventTarget | null): boolean {
    const element: HTMLElement | null = this.findNearestElement(target);

    return !!element?.closest('a, button, input, textarea, select, summary, [role="button"], [contenteditable="true"]');
  }

  private findNearestElement(target: EventTarget | null): HTMLElement | null {
    if (typeof HTMLElement !== 'undefined' && target instanceof HTMLElement) {
      return target;
    }

    if (typeof Node !== 'undefined' && target instanceof Node) {
      return target.parentElement;
    }

    return null;
  }

  private resolveBlockFromElement(element: HTMLElement): AdminContextualBlockInstance | null {
    const markerId: string | null = element.getAttribute('data-admin-contextual-block-marker-id');
    const marker: PublicContextualBlockMarker | null = this.markerRegistry.getMarker(markerId);

    return marker ? this.createBlock(marker) : null;
  }

  private createBlock(marker: PublicContextualBlockMarker): AdminContextualBlockInstance | null {
    if (this.isParkBlockType(marker.type)) {
      return this.contextualBlockRegistry.createParkBlock(
        marker.type,
        marker.parkId,
        marker.contextLabel,
        marker.languageCode,
        marker.locationFallbackCenter ?? null
      );
    }

    if (this.isParkItemBlockType(marker.type)) {
      return this.contextualBlockRegistry.createParkItemBlock(
        marker.type,
        marker.parkItemId,
        marker.parkId,
        marker.contextLabel,
        marker.languageCode,
        marker.locationFallbackCenter ?? null
      );
    }

    if (marker.type === 'reference.manufacturer') {
      return this.contextualBlockRegistry.createManufacturerBlock(
        marker.manufacturerId,
        marker.contextLabel,
        marker.languageCode,
        marker.parkGraphUpsertDraftJson,
        marker.parkGraphUpsertFileName
      );
    }

    return null;
  }

  private isParkBlockType(type: PublicContextualBlockType): type is AdminContextualBlockType {
    return type === 'park.hero'
      || type === 'park.description'
      || type === 'park.images'
      || type === 'park.location'
      || type === 'park.practical';
  }

  private isParkItemBlockType(type: PublicContextualBlockType): type is AdminContextualBlockType {
    return type === 'parkItem.description'
      || type === 'parkItem.images'
      || type === 'parkItem.location';
  }
}
