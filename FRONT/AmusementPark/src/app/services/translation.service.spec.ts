import { TestBed } from '@angular/core/testing';

import { TranslationService } from './translation.service';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';

describe('TranslationService', () => {
  let service: TranslationService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: provideCommonTestDependencies(),
    });

    service = TestBed.inject(TranslationService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
