import { inject, InjectionToken } from '@angular/core';
import { ContactApiService } from '@data-access/contact/contact-api.service';

export interface AdminContactGrievancesDataPort extends Pick<ContactApiService, 'searchAdminGrievances'> {
}

export const ADMIN_CONTACT_GRIEVANCES_DATA_PORT = new InjectionToken<AdminContactGrievancesDataPort>('ADMIN_CONTACT_GRIEVANCES_DATA_PORT', {
  providedIn: 'root',
  factory: () => inject(ContactApiService)
});
