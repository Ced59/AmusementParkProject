import { createLeafletPopupLayoutOptions } from './leaflet-popup-layout.factory';

describe('createLeafletPopupLayoutOptions', () => {
  it('reserves native zoom-control space without changing pan enablement, zoom, close or marker options', () => {
    expect(createLeafletPopupLayoutOptions()).toEqual({ autoPanPaddingTopLeft: [5, 80] });
  });

  it('does not share mutable options between maps or markers', () => {
    const first = createLeafletPopupLayoutOptions();
    const second = createLeafletPopupLayoutOptions();
    expect(first).not.toBe(second);
    expect(first.autoPanPaddingTopLeft).not.toBe(second.autoPanPaddingTopLeft);
  });
});
