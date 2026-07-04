export type DataQualityLevel = 'Critical' | 'Weak' | 'Partial' | 'Publishable' | 'Good' | 'Excellent';

export interface DataCompletenessScore {
  completenessScore: number;
  dataQualityLevel: DataQualityLevel;
  applicableMaxPoints: number;
  earnedPoints: number;
}

export type DataCompletenessSeverity = 'success' | 'info' | 'warn' | 'danger';

export function getDataCompletenessLabel(score: DataCompletenessScore | null | undefined): string {
  return score ? `${score.completenessScore}%` : '-';
}

export function getDataCompletenessSeverity(score: DataCompletenessScore | null | undefined): DataCompletenessSeverity {
  switch (score?.dataQualityLevel) {
    case 'Excellent':
    case 'Good':
      return 'success';
    case 'Publishable':
      return 'info';
    case 'Partial':
      return 'warn';
    case 'Critical':
    case 'Weak':
    default:
      return 'danger';
  }
}
