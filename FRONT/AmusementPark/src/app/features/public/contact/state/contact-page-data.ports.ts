import { inject, InjectionToken } from '@angular/core';
import { ContactApiService } from '@data-access/contact/contact-api.service';

export interface ContactPageDataPort extends Pick<ContactApiService, 'submitGrievance'> {
}

export const CONTACT_PAGE_DATA_PORT = new InjectionToken<ContactPageDataPort>('CONTACT_PAGE_DATA_PORT', {
  providedIn: 'root',
  factory: () => inject(ContactApiService)
});
