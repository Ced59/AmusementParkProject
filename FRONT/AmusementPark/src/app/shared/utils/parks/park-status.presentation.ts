import { ParkStatus } from '@app/models/parks/park-status';
import { UiPrimitiveTone } from '@ui/primitives/models/ui-primitive-variant.model';

export interface ParkStatusPresentation {
  status: ParkStatus;
  labelKey: string;
  iconClass: string;
  tone: UiPrimitiveTone;
  showOnCard: boolean;
  isOpenToVisitors: boolean;
}

const PARK_STATUS_PRESENTATIONS: Record<ParkStatus, ParkStatusPresentation> = {
  Planned: {
    status: 'Planned',
    labelKey: 'parks.statuses.planned',
    iconClass: 'pi pi-calendar',
    tone: 'purple',
    showOnCard: true,
    isOpenToVisitors: false
  },
  UnderConstruction: {
    status: 'UnderConstruction',
    labelKey: 'parks.statuses.underConstruction',
    iconClass: 'pi pi-wrench',
    tone: 'gold',
    showOnCard: true,
    isOpenToVisitors: false
  },
  Operating: {
    status: 'Operating',
    labelKey: 'parks.statuses.operating',
    iconClass: 'pi pi-check-circle',
    tone: 'lime',
    showOnCard: true,
    isOpenToVisitors: true
  },
  TemporarilyClosed: {
    status: 'TemporarilyClosed',
    labelKey: 'parks.statuses.temporarilyClosed',
    iconClass: 'pi pi-pause-circle',
    tone: 'rose',
    showOnCard: true,
    isOpenToVisitors: false
  },
  ClosedDefinitively: {
    status: 'ClosedDefinitively',
    labelKey: 'parks.statuses.closedDefinitively',
    iconClass: 'pi pi-ban',
    tone: 'soft',
    showOnCard: true,
    isOpenToVisitors: false
  },
  Cancelled: {
    status: 'Cancelled',
    labelKey: 'parks.statuses.cancelled',
    iconClass: 'pi pi-times-circle',
    tone: 'soft',
    showOnCard: true,
    isOpenToVisitors: false
  }
};

export function resolveParkStatus(status: ParkStatus | null | undefined): ParkStatus {
  return status && PARK_STATUS_PRESENTATIONS[status] ? status : 'Operating';
}

export function getParkStatusPresentation(status: ParkStatus | null | undefined): ParkStatusPresentation {
  return PARK_STATUS_PRESENTATIONS[resolveParkStatus(status)];
}

export function isParkOpenToVisitors(status: ParkStatus | null | undefined): boolean {
  return getParkStatusPresentation(status).isOpenToVisitors;
}
