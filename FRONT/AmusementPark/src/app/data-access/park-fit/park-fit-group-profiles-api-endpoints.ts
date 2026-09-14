export const PARK_FIT_GROUP_PROFILES_API_ENDPOINTS = {
  collection: 'me/park-fit/group-profiles',
  profile: (profileId: string): string =>
    `me/park-fit/group-profiles/${encodeURIComponent(profileId)}`,
  export: 'me/park-fit/group-profiles/export'
} as const;
