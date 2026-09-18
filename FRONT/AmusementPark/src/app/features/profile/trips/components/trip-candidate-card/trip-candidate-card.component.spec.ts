import { TripParkCandidate, TripParkCandidateState } from '@app/models/trips/trip.models';
import { TripCandidateCardComponent } from './trip-candidate-card.component';

describe('TripCandidateCardComponent', () => {
  it('keeps a scheduled park selected until its saved day is cleared', () => {
    const component: TripCandidateCardComponent = new TripCandidateCardComponent();
    component.candidate = createCandidate();
    component.scheduled = true;
    const state = component as unknown as {
      isStateDisabled: (candidateState: TripParkCandidateState) => boolean;
    };

    expect(state.isStateDisabled('Proposed')).toBe(true);
    expect(state.isStateDisabled('Shortlisted')).toBe(true);
    expect(state.isStateDisabled('Rejected')).toBe(true);

    component.scheduled = false;
    expect(state.isStateDisabled('Proposed')).toBe(false);
  });
});

function createCandidate(): TripParkCandidate {
  return {
    candidateId: 'candidate-1', parkId: 'park-1', parkName: 'Parc A', isParkAvailable: true,
    candidateDates: [], source: 'Manual', state: 'Selected', collectiveNote: null, fitSnapshot: null,
    sortPosition: 0, version: 1, createdAtUtc: '2026-09-18T10:00:00Z', updatedAtUtc: '2026-09-18T10:00:00Z'
  };
}
