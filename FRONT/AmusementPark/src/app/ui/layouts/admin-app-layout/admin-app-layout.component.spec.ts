import { ViewEncapsulation } from '@angular/core';

import { AdminAppLayoutComponent } from './admin-app-layout.component';

describe('AdminAppLayoutComponent', () => {
  it('uses unscoped component styles so admin CSS stays out of the public initial bundle', () => {
    expect((AdminAppLayoutComponent as unknown as { ɵcmp: { encapsulation: ViewEncapsulation } }).ɵcmp.encapsulation)
      .toBe(ViewEncapsulation.None);
  });
});
