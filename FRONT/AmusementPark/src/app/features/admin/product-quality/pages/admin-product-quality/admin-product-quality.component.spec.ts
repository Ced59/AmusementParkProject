import { ComponentFixture, TestBed } from '@angular/core/testing';

import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies
} from '@app/testing/common-test-providers';
import { PRODUCT_QUALITY_DASHBOARDS } from '../../models/product-quality-dashboard.models';
import { AdminProductQualityComponent } from './admin-product-quality.component';

describe('AdminProductQualityComponent', (): void => {
  it('renders one decision card per product program', async (): Promise<void> => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminProductQualityComponent],
      providers: provideCommonTestDependencies()
    }).compileComponents();
    const fixture: ComponentFixture<AdminProductQualityComponent> =
      TestBed.createComponent(AdminProductQualityComponent);

    fixture.detectChanges();

    const modules: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll(
      '.admin-product-quality__module'
    );
    const links: NodeListOf<HTMLAnchorElement> = fixture.nativeElement.querySelectorAll(
      '.admin-product-quality__link'
    );
    expect(modules.length).toBe(PRODUCT_QUALITY_DASHBOARDS.length);
    expect(links.length).toBe(PRODUCT_QUALITY_DASHBOARDS.length);
  });
});
