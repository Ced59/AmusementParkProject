import { ParkStatus } from '@app/models/parks/park-status';

export interface ParkLifecycleNoticeModel {
  titleKey: string;
  bodyKey: string;
  iconClass: string;
}

const NOTICE_BY_STATUS: Partial<Record<ParkStatus, ParkLifecycleNoticeModel>> = {
  Planned: {
    titleKey: 'parks.lifecycle.planned.title',
    bodyKey: 'parks.lifecycle.planned.body',
    iconClass: 'pi pi-calendar'
  },
  UnderConstruction: {
    titleKey: 'parks.lifecycle.underConstruction.title',
    bodyKey: 'parks.lifecycle.underConstruction.body',
    iconClass: 'pi pi-wrench'
  },
  TemporarilyClosed: {
    titleKey: 'parks.lifecycle.temporarilyClosed.title',
    bodyKey: 'parks.lifecycle.temporarilyClosed.body',
    iconClass: 'pi pi-pause-circle'
  },
  ClosedDefinitively: {
    titleKey: 'parks.lifecycle.closedDefinitively.title',
    bodyKey: 'parks.lifecycle.closedDefinitively.body',
    iconClass: 'pi pi-history'
  },
  Cancelled: {
    titleKey: 'parks.lifecycle.cancelled.title',
    bodyKey: 'parks.lifecycle.cancelled.body',
    iconClass: 'pi pi-times-circle'
  }
};

export function resolveParkLifecycleNotice(status: ParkStatus | null | undefined): ParkLifecycleNoticeModel | null {
  return status ? NOTICE_BY_STATUS[status] ?? null : null;
}
