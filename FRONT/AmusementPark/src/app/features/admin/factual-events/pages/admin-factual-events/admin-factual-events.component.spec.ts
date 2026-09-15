import { FactualChangeEventAdmin } from '@app/models/admin/factual-events/factual-event-administration.models';
import { AdminFactualEventsStateFacade } from '@features/admin/factual-events/state/admin-factual-events-state.facade';
import { AdminFactualEventsComponent } from './admin-factual-events.component';

describe('AdminFactualEventsComponent', (): void => {
  it('does not use a parent park name as the missing park item name', (): void => {
    const component = new AdminFactualEventsComponent({} as AdminFactualEventsStateFacade);
    const targetName = (component as unknown as {
      targetName(event: FactualChangeEventAdmin): string;
    }).targetName(createEventWithTarget(null, 'Parc exemple'));

    expect(targetName).toBe('');
  });

  it('keeps the resolved target name when it is available', (): void => {
    const component = new AdminFactualEventsComponent({} as AdminFactualEventsStateFacade);
    const targetName = (component as unknown as {
      targetName(event: FactualChangeEventAdmin): string;
    }).targetName(createEventWithTarget('Attraction exemple', 'Parc exemple'));

    expect(targetName).toBe('Attraction exemple');
  });

  it('does not offer verification for a low-confidence draft', (): void => {
    const component = new AdminFactualEventsComponent({} as AdminFactualEventsStateFacade);
    const canVerify = (component as unknown as {
      canVerify(event: FactualChangeEventAdmin): boolean;
    }).canVerify({ status: 'Draft', confidence: 'Low' } as FactualChangeEventAdmin);

    expect(canVerify).toBe(false);
  });

  it('offers verification for a draft with sufficient confidence', (): void => {
    const component = new AdminFactualEventsComponent({} as AdminFactualEventsStateFacade);
    const canVerify = (component as unknown as {
      canVerify(event: FactualChangeEventAdmin): boolean;
    }).canVerify({ status: 'Draft', confidence: 'Medium' } as FactualChangeEventAdmin);

    expect(canVerify).toBe(true);
  });
});

function createEventWithTarget(
  name: string | null,
  parentParkName: string | null,
): FactualChangeEventAdmin {
  return {
    target: { type: 'ParkItem', name, parentParkName },
  } as FactualChangeEventAdmin;
}
