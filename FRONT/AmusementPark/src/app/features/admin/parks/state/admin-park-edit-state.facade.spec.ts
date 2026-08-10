import type { MockedObject } from 'vitest';
import { of, throwError } from 'rxjs';

import { ParkPricing } from '@app/models/parks/park-pricing';
import { AdminParkEditStateParksApiServicePort } from './admin-park-edit-state-data.ports';
import { AdminParkEditStateFacade } from './admin-park-edit-state.facade';

describe('AdminParkEditStateFacade pricing', () => {
  let port: MockedObject<AdminParkEditStateParksApiServicePort>;
  let facade: AdminParkEditStateFacade;
  let pricing: ParkPricing;

  beforeEach(() => {
    pricing = {
      parkId: 'park-1',
      currencyCode: 'EUR',
      notes: [],
      admissionOffers: [],
      annualPasses: [],
      parkingOffers: [],
    };
    port = {
      getAdminParkPricing: vi.fn().mockReturnValue(of(pricing)),
      upsertAdminParkPricing: vi.fn().mockReturnValue(of(pricing)),
    } as unknown as MockedObject<AdminParkEditStateParksApiServicePort>;
    facade = new AdminParkEditStateFacade(port);
  });

  it('loads pricing through the admin data port and resets loading state', async () => {
    const result: ParkPricing = await facade.loadPricing('park-1');

    expect(result).toEqual(pricing);
    expect(port.getAdminParkPricing).toHaveBeenCalledWith('park-1');
    expect(facade.pricingLoading()).toBe(false);
  });

  it('saves pricing through the admin data port and resets saving state', async () => {
    const result: ParkPricing = await facade.savePricing('park-1', pricing);

    expect(result).toEqual(pricing);
    expect(port.upsertAdminParkPricing).toHaveBeenCalledWith('park-1', pricing);
    expect(facade.pricingSaving()).toBe(false);
  });

  it('resets pricing loading state when the API fails', async () => {
    port.getAdminParkPricing.mockReturnValue(throwError(() => new Error('failure')));

    await expect(facade.loadPricing('park-1')).rejects.toThrow('failure');
    expect(facade.pricingLoading()).toBe(false);
  });
});
