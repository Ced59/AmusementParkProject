import { resolveParkLifecycleNotice } from './park-lifecycle-notice.model';

describe('resolveParkLifecycleNotice', () => {
  it('does not display a lifecycle warning for operating parks', () => {
    expect(resolveParkLifecycleNotice('Operating')).toBeNull();
  });

  it.each([
    'Planned',
    'UnderConstruction',
    'TemporarilyClosed',
    'ClosedDefinitively',
    'Cancelled'
  ] as const)('returns dedicated content for %s parks', (status) => {
    expect(resolveParkLifecycleNotice(status)).toEqual(expect.objectContaining({
      titleKey: `parks.lifecycle.${status.charAt(0).toLowerCase()}${status.slice(1)}.title`
    }));
  });
});
