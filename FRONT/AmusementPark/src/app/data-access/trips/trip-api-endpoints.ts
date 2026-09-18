const encodeSegment = (value: string): string => encodeURIComponent(value);

export const TRIP_API_ENDPOINTS = {
  plans: 'me/trips',
  plan: (tripPlanId: string): string => `me/trips/${encodeSegment(tripPlanId)}`,
  rename: (tripPlanId: string): string => `me/trips/${encodeSegment(tripPlanId)}/rename`,
  dates: (tripPlanId: string): string => `me/trips/${encodeSegment(tripPlanId)}/dates`,
  program: (tripPlanId: string): string => `me/trips/${encodeSegment(tripPlanId)}/program`,
  invitations: (tripPlanId: string): string => `me/trips/${encodeSegment(tripPlanId)}/invitations`,
  invitation: (tripPlanId: string, invitationId: string): string =>
    `me/trips/${encodeSegment(tripPlanId)}/invitations/${encodeSegment(invitationId)}`,
  invitationPreview: (token: string): string =>
    `public/trip-invitations/${encodeSegment(token)}/preview`,
  acceptInvitation: 'me/trip-invitations/accept',
  declineInvitation: 'me/trip-invitations/decline',
  participants: (tripPlanId: string): string =>
    `me/trips/${encodeSegment(tripPlanId)}/participants`,
  participantRole: (tripPlanId: string, memberId: string): string =>
    `me/trips/${encodeSegment(tripPlanId)}/participants/${encodeSegment(memberId)}/role`,
  transferOwnership: (tripPlanId: string, memberId: string): string =>
    `me/trips/${encodeSegment(tripPlanId)}/participants/${encodeSegment(memberId)}/transfer-ownership`,
  leaveTrip: (tripPlanId: string): string =>
    `me/trips/${encodeSegment(tripPlanId)}/participants/me`,
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
