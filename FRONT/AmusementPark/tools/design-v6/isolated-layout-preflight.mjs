// Diagnostic subset only. The full campaign remains a separate mandatory step.
export function isolatedLayoutPreflightPlan(environment, platform) {
  if (environment.V6_ISOLATED_PREFLIGHT === undefined) return null;
  if (environment.V6_ISOLATED_PREFLIGHT !== 'true') {
    throw new Error('V6_ISOLATED_PREFLIGHT must be omitted or exactly true.');
  }
  if (environment.CI !== 'true' || platform !== 'linux') {
    throw new Error('The isolated UI preflight is restricted to Linux CI.');
  }
  if (!environment.V6_BASELINE_DIST_DIR || !/^[a-f0-9]{40}$/.test(environment.V6_BASELINE_SHA ?? '')) {
    throw new Error('The isolated UI preflight requires the exact compiled master baseline.');
  }
  return {
    fullCampaignRequired: true,
    cases: ['standard', 'incomplete'].map(kind => ({ kind, language: 'fr', widths: [390, 1366] })),
  };
}
