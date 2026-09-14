import { ParkFitCriticalSource } from '@app/models/park-fit/park-fit-search.models';
import {
  parkFitComponentKindKey,
  parkFitScoreReasonKey,
  resolveParkFitSourceSummary,
  resolveParkFitSourceUrl
} from './park-fit-result-display.helpers';

describe('park fit result display helpers', () => {
  it('never exposes an unknown backend code as a translation key', () => {
    expect(parkFitComponentKindKey('InternalFutureCode')).toBe(
      'parkFit.results.componentKinds.Unknown'
    );
    expect(parkFitScoreReasonKey('InternalFutureCode')).toBe(
      'parkFit.results.scoreReasons.Unknown'
    );
  });

  it('chooses the requested localized proof before the English fallback', () => {
    const source: ParkFitCriticalSource = buildSource();

    expect(resolveParkFitSourceSummary(source, 'fr')).toBe('Condition officielle.');
    expect(resolveParkFitSourceSummary(source, 'nl')).toBe('Official condition.');
  });

  it('only turns secure public URLs into links', () => {
    expect(resolveParkFitSourceUrl(buildSource())).toBe('https://example.test/access');
    expect(resolveParkFitSourceUrl({ ...buildSource(), url: 'javascript:alert(1)' })).toBeNull();
    expect(resolveParkFitSourceUrl({ ...buildSource(), url: null })).toBeNull();
  });
});

function buildSource(): ParkFitCriticalSource {
  return {
    kind: 'Official',
    url: 'https://example.test/access',
    reference: 'internal-reference-never-rendered',
    languageCode: 'fr',
    collectedAtUtc: '2026-08-01T00:00:00Z',
    verifiedAtUtc: '2026-09-01T00:00:00Z',
    confidence: 'High',
    summaries: [
      { languageCode: 'en', value: 'Official condition.' },
      { languageCode: 'fr', value: 'Condition officielle.' }
    ]
  };
}
