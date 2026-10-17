import type { PopupOptions } from 'leaflet';

/** Keep the existing two native zoom controls clear of the popup frame. */
export function createLeafletPopupLayoutOptions(): PopupOptions {
  return { autoPanPaddingTopLeft: [5, 80] };
}
