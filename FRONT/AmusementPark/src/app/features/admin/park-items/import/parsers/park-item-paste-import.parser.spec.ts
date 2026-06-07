import { parseParkItemPasteImport } from './park-item-paste-import.parser';

describe('parseParkItemPasteImport', () => {
  it('maps one-name-per-line input to minimal drafts', () => {
    const result = parseParkItemPasteImport('Coaster One\n# comment\nDark Ride');

    expect(result).toEqual([
      jasmine.objectContaining({ rowNumber: 1, name: 'Coaster One', isVisible: false, adminReviewStatus: 'ToReview' }),
      jasmine.objectContaining({ rowNumber: 2, name: 'Dark Ride', isVisible: false, adminReviewStatus: 'ToReview' }),
    ]);
  });

  it('detects semicolon columns without a header', () => {
    const result = parseParkItemPasteImport('Blue Fire;Attraction;RollerCoaster;Iceland;Mack Rides');

    expect(result[0]).toEqual(jasmine.objectContaining({
      name: 'Blue Fire',
      category: 'Attraction',
      type: 'RollerCoaster',
      zoneName: 'Iceland',
      manufacturerName: 'Mack Rides',
    }));
  });

  it('uses header mapping for pasted spreadsheet tables', () => {
    const result = parseParkItemPasteImport('nom\tzone\tconstructeur\tvisible\nArthur\tMinimoys\tMack Rides\toui');

    expect(result[0]).toEqual(jasmine.objectContaining({
      name: 'Arthur',
      zoneName: 'Minimoys',
      manufacturerName: 'Mack Rides',
      isVisible: true,
    }));
  });
});
