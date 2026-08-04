import { getAttractionTechnicalValueKey } from './park-item-detail-presentation.mapper';

describe('park item detail presentation mapper', () => {
  it('maps imported technical values to localized presentation keys', () => {
    expect(getAttractionTechnicalValueKey('material', 'Steel')).toBe('parkItems.technicalValues.material.steel');
    expect(getAttractionTechnicalValueKey('seating', 'Sit Down')).toBe('parkItems.technicalValues.seating.sitDown');
    expect(getAttractionTechnicalValueKey('launch', 'Lift à pneus')).toBe('parkItems.technicalValues.launch.tireLift');
    expect(getAttractionTechnicalValueKey('restraint', 'Lap Bar commune')).toBe('parkItems.technicalValues.restraint.sharedLapBar');
  });

  it('keeps unknown technical values available as raw fallbacks', () => {
    expect(getAttractionTechnicalValueKey('material', 'Experimental alloy')).toBeNull();
    expect(getAttractionTechnicalValueKey('seating', null)).toBeNull();
  });
});
