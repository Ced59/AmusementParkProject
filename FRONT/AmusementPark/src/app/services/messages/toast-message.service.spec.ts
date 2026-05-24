import { TestBed } from '@angular/core/testing';

import { ToastMessageService } from './toast-message.service';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';

describe('ToastMessageService', () => {
  let service: ToastMessageService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: provideCommonTestDependencies(),
    });

    service = TestBed.inject(ToastMessageService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
