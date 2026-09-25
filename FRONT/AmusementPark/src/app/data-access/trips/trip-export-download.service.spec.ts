import { TripExport } from '@app/models/trips/trip-export.models';
import { TripExportDownloadService } from './trip-export-download.service';

describe('TripExportDownloadService', () => {
  it('downloads a readable file name without a technical identifier', () => {
    const click = vi.fn();
    const remove = vi.fn();
    const revokeObjectURL = vi.fn();
    const originalCreateObjectURL: typeof URL.createObjectURL = URL.createObjectURL;
    const originalRevokeObjectURL: typeof URL.revokeObjectURL = URL.revokeObjectURL;
    Object.defineProperty(URL, 'createObjectURL', {
      configurable: true,
      value: vi.fn().mockReturnValue('blob:export')
    });
    Object.defineProperty(URL, 'revokeObjectURL', {
      configurable: true,
      value: revokeObjectURL
    });
    const link = { click, remove } as unknown as HTMLAnchorElement;
    const document = {
      defaultView: {},
      body: { appendChild: vi.fn() },
      createElement: vi.fn().mockReturnValue(link)
    } as unknown as Document;
    const service: TripExportDownloadService = new TripExportDownloadService(document);

    try {
      const downloaded: boolean = service.downloadJson(createPlan());

      expect(downloaded).toBe(true);
      expect(link.download).toBe('week-end-a-cologne.json');
      expect(link.href).toBe('blob:export');
      expect(click).toHaveBeenCalledOnce();
      expect(remove).toHaveBeenCalledOnce();
      expect(revokeObjectURL).toHaveBeenCalledWith('blob:export');
    } finally {
      Object.defineProperty(URL, 'createObjectURL', {
        configurable: true,
        value: originalCreateObjectURL
      });
      Object.defineProperty(URL, 'revokeObjectURL', {
        configurable: true,
        value: originalRevokeObjectURL
      });
    }
  });
});

function createPlan(): TripExport {
  return {
    schemaVersion: 'trip-plan-export-v1',
    title: 'Week-end à Cologne',
    dateProposal: { kind: 'None', startDate: null, endDate: null, candidateDates: [] },
    destinationTimeZoneId: null,
    status: 'Planning',
    generatedAtUtc: '2027-08-12T09:30:00Z',
    candidateParks: [],
    days: [],
    collectiveDecisions: []
  };
}
