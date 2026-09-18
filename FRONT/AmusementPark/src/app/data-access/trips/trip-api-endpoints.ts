const encodeSegment = (value: string): string => encodeURIComponent(value);

export const TRIP_API_ENDPOINTS = {
  plans: 'me/trips',
  plan: (tripPlanId: string): string => `me/trips/${encodeSegment(tripPlanId)}`,
  rename: (tripPlanId: string): string => `me/trips/${encodeSegment(tripPlanId)}/rename`,
  dates: (tripPlanId: string): string => `me/trips/${encodeSegment(tripPlanId)}/dates`,
  program: (tripPlanId: string): string => `me/trips/${encodeSegment(tripPlanId)}/program`,
  parks: (tripPlanId: string): string => `me/trips/${encodeSegment(tripPlanId)}/parks`,
  park: (tripPlanId: string, candidateId: string): string =>
    `me/trips/${encodeSegment(tripPlanId)}/parks/${encodeSegment(candidateId)}`,
  parkState: (tripPlanId: string, candidateId: string): string =>
    `me/trips/${encodeSegment(tripPlanId)}/parks/${encodeSegment(candidateId)}/state`,
  parkMove: (tripPlanId: string, candidateId: string): string =>
    `me/trips/${encodeSegment(tripPlanId)}/parks/${encodeSegment(candidateId)}/move`,
  day: (tripPlanId: string, localDate: string): string =>
    `me/trips/${encodeSegment(tripPlanId)}/days/${encodeSegment(localDate)}`
} as const;
