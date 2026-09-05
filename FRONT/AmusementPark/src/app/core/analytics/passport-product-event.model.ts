export type PassportProductSource = 'authenticated' | 'anonymous-local';
export type PassportProductDatePrecision = 'Year' | 'Month' | 'Day';
export type PassportProductExportFormat = 'Json' | 'Csv';

export type PassportProductEvent =
  | {
      readonly type: 'passport_opened';
      readonly source: PassportProductSource;
    }
  | {
      readonly type: 'visit_creation_started' | 'visit_created';
      readonly source: PassportProductSource;
      readonly datePrecision: PassportProductDatePrecision;
    }
  | {
      readonly type: 'visit_completed' | 'visit_reopened';
      readonly source: 'authenticated';
    }
  | {
      readonly type: 'second_visit_recorded';
      readonly source: 'anonymous-local';
    }
  | {
      readonly type: 'ride_occurrence_added';
      readonly source: PassportProductSource;
      readonly countBucket: 'one' | 'two-to-five' | 'six-plus';
    }
  | {
      readonly type: 'temporal_rating_added';
      readonly source: 'authenticated';
      readonly targetType: 'park-visit' | 'ride-occurrence';
    }
  | {
      readonly type: 'passport_statistics_opened';
      readonly source: 'authenticated';
      readonly scope: 'global' | 'park' | 'item' | 'year';
    }
  | {
      readonly type: 'passport_export_requested';
      readonly source: PassportProductSource;
      readonly format: PassportProductExportFormat;
    }
  | {
      readonly type: 'passport_deletion_started' | 'passport_deletion_completed';
      readonly source: PassportProductSource;
    };

export function passportRideCountBucket(count: number): 'one' | 'two-to-five' | 'six-plus' {
  if (count <= 1) {
    return 'one';
  }

  return count <= 5 ? 'two-to-five' : 'six-plus';
}
