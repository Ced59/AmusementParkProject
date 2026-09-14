import { ParkFitGroupProfileExportItem } from './park-fit-group-profile-export-item.model';

export interface ParkFitGroupProfileExport {
  exportedAtUtc: string;
  profiles: ParkFitGroupProfileExportItem[];
}
