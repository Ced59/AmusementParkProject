// Diagnostic subset only. The full campaign remains a separate mandatory step.
export function isolatedLayoutPreflightPlan(environment, platform) {
  if (environment.V6_ISOLATED_PREFLIGHT === undefined) {
    if (environment.V6_PREFLIGHT_PHASE !== undefined) throw new Error('A preflight phase requires the explicit isolated preflight flag.');
    return null;
  }
  if (environment.V6_ISOLATED_PREFLIGHT !== 'true') {
    throw new Error('V6_ISOLATED_PREFLIGHT must be omitted or exactly true.');
  }
  if (environment.CI !== 'true' || platform !== 'linux') {
    throw new Error('The isolated UI preflight is restricted to Linux CI.');
  }
  if (!environment.V6_BASELINE_DIST_DIR || !/^[a-f0-9]{40}$/.test(environment.V6_BASELINE_SHA ?? '')) {
    throw new Error('The isolated UI preflight requires the exact compiled master baseline.');
  }
  const phase = environment.V6_PREFLIGHT_PHASE ?? 'all';
  if (!['all', 'admin', 'core'].includes(phase)) throw new Error('Unknown bounded preflight phase.');
  const cases = [
    { kind: 'admin-shell', language: 'fr', widths: [320, 390, 1366] },
    ...['standard', 'incomplete'].map(kind => ({ kind, language: 'fr', widths: [390, 1366] })),
  ];
  return {
    fullCampaignRequired: true,
    phase,
    cases: cases.filter(entry => phase === 'all' || (phase === 'admin') === (entry.kind === 'admin-shell')),
  };
}
