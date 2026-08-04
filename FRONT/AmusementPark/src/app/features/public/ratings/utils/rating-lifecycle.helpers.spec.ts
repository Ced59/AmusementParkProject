import {
  resolveParkItemRatingContextHintKey,
  resolveParkRatingContextHintKey
} from './rating-lifecycle.helpers';

describe('rating lifecycle helpers', () => {
  it('uses past-visit wording for temporarily closed parks', () => {
    expect(resolveParkRatingContextHintKey('TemporarilyClosed')).toBe('ratings.stars.pastVisitHint');
  });

  it('uses historical wording for permanently closed parks and removed attractions', () => {
    expect(resolveParkRatingContextHintKey('ClosedDefinitively')).toBe('ratings.stars.historicalHint');
    expect(resolveParkItemRatingContextHintKey('Operating', 'Removed')).toBe('ratings.stars.historicalHint');
  });

  it('does not add context wording to current ratings', () => {
    expect(resolveParkRatingContextHintKey('Operating')).toBeNull();
    expect(resolveParkItemRatingContextHintKey('Operating', 'Operating')).toBeNull();
  });
});
