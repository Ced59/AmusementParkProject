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

  it('recognizes French lift and restraint values with accents and spacing', () => {
    expect(getAttractionTechnicalValueKey('launch', '  Lift à câble  ')).toBe('parkItems.technicalValues.launch.cableLift');
    expect(getAttractionTechnicalValueKey('launch', 'Lift à chaîne')).toBe('parkItems.technicalValues.launch.chainLift');
    expect(getAttractionTechnicalValueKey('restraint', 'Ceinture')).toBe('parkItems.technicalValues.restraint.seatBelt');
    expect(getAttractionTechnicalValueKey('restraint', 'Ceinture de sécurité')).toBe('parkItems.technicalValues.restraint.seatBelt');
  });

  it('uses the same localized labels for equivalent English values', () => {
    expect(getAttractionTechnicalValueKey('launch', 'Cable lift')).toBe('parkItems.technicalValues.launch.cableLift');
    expect(getAttractionTechnicalValueKey('launch', 'ChainLift')).toBe('parkItems.technicalValues.launch.chainLift');
    expect(getAttractionTechnicalValueKey('restraint', 'Seat belt')).toBe('parkItems.technicalValues.restraint.seatBelt');
    expect(getAttractionTechnicalValueKey('seating', 'Alpine')).toBeNull();
  });
});
