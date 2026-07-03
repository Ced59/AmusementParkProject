import { Injectable } from '@angular/core';

import { PublicContextualBlockMarker } from '../models/public-contextual-block-marker.model';

@Injectable({
  providedIn: 'root'
})
export class PublicContextualBlockMarkerRegistry {
  private readonly markersById = new Map<string, PublicContextualBlockMarker>();

  setMarker(id: string, marker: PublicContextualBlockMarker): void {
    this.markersById.set(id, marker);
  }

  getMarker(id: string | null): PublicContextualBlockMarker | null {
    return id ? this.markersById.get(id) ?? null : null;
  }

  deleteMarker(id: string): void {
    this.markersById.delete(id);
  }
}
