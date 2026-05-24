import { TestBed } from '@angular/core/testing';

import { ThemeService } from './themes.service';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';

describe('ThemeService', () => {
  let service: ThemeService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: provideCommonTestDependencies(),
    });

    service = TestBed.inject(ThemeService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
