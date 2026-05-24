import { TestBed } from '@angular/core/testing';

import { SharedService } from './shared.service';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';

describe('SharedService', () => {
  let service: SharedService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: provideCommonTestDependencies(),
    });

    service = TestBed.inject(SharedService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
