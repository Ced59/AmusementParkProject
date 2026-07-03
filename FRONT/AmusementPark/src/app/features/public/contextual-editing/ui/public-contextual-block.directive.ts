import { Directive, ElementRef, Input, OnDestroy, Renderer2, inject } from '@angular/core';

import { PublicContextualBlockMarker } from '../models/public-contextual-block-marker.model';
import { PublicContextualBlockMarkerRegistry } from '../state/public-contextual-block-marker.registry';

let nextPublicContextualBlockId = 0;

@Directive({
  selector: '[appPublicContextualBlock]',
  standalone: true
})
export class PublicContextualBlockDirective implements OnDestroy {
  private readonly elementRef: ElementRef<HTMLElement> = inject(ElementRef<HTMLElement>);
  private readonly markerRegistry: PublicContextualBlockMarkerRegistry = inject(PublicContextualBlockMarkerRegistry);
  private readonly renderer: Renderer2 = inject(Renderer2);
  private readonly markerId: string = `public-contextual-block-${++nextPublicContextualBlockId}`;

  @Input()
  set appPublicContextualBlock(marker: PublicContextualBlockMarker | null) {
    if (!marker) {
      this.clearMarker();
      return;
    }

    this.markerRegistry.setMarker(this.markerId, marker);
    this.renderer.setAttribute(this.elementRef.nativeElement, 'data-admin-contextual-block-marker-id', this.markerId);
    this.renderer.setAttribute(this.elementRef.nativeElement, 'data-admin-contextual-block-type', marker.type);
  }

  ngOnDestroy(): void {
    this.clearMarker();
  }

  private clearMarker(): void {
    this.markerRegistry.deleteMarker(this.markerId);
    this.renderer.removeAttribute(this.elementRef.nativeElement, 'data-admin-contextual-block-marker-id');
    this.renderer.removeAttribute(this.elementRef.nativeElement, 'data-admin-contextual-block-type');
  }
}
