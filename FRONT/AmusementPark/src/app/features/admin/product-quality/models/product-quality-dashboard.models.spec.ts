import {
  PRODUCT_QUALITY_DASHBOARDS,
  ProductQualityDashboardDefinition,
  ProductQualityProgramCode
} from './product-quality-dashboard.models';

describe('PRODUCT_QUALITY_DASHBOARDS', (): void => {
  it('covers each product program exactly once', (): void => {
    const expectedCodes: readonly ProductQualityProgramCode[] = [
      'RANK', 'PASS', 'SHARE', 'FIT', 'WATCH', 'TRIP', 'HIST', 'LIVE'
    ];
    const codes: readonly ProductQualityProgramCode[] = PRODUCT_QUALITY_DASHBOARDS.map(
      (dashboard: ProductQualityDashboardDefinition) => dashboard.code
    );

    expect(codes).toEqual(expectedCodes);
    expect(new Set(codes).size).toBe(expectedCodes.length);
  });

  it('contains navigation and classification only, never identifiers or raw measures', (): void => {
    const serializedDefinitions: string = JSON.stringify(PRODUCT_QUALITY_DASHBOARDS).toLowerCase();

    expect(serializedDefinitions).not.toContain('userid');
    expect(serializedDefinitions).not.toContain('visitid');
    expect(serializedDefinitions).not.toContain('parkid');
    expect(serializedDefinitions).not.toContain('count');
    expect(PRODUCT_QUALITY_DASHBOARDS.every(
      (dashboard: ProductQualityDashboardDefinition) => dashboard.routeSegments.length > 0
    )).toBe(true);
  });
});
