import { inject, InjectionToken } from '@angular/core';

import { ParkFitPilotTelemetryApiService } from '@data-access/park-fit/park-fit-pilot-telemetry-api.service';
import { ParkFitPilotObservation } from '@app/models/park-fit/park-fit-pilot-observation.model';

export interface ParkFitPilotTelemetryPort {
  track(observation: ParkFitPilotObservation): void;
}

export const PARK_FIT_PILOT_TELEMETRY_PORT = new InjectionToken<ParkFitPilotTelemetryPort>(
  'PARK_FIT_PILOT_TELEMETRY_PORT',
  {
    providedIn: 'root',
    factory: () => inject(ParkFitPilotTelemetryApiService)
  }
);
