export type SocialShareTargetType =
  'Home'
  | 'Parks'
  | 'Park'
  | 'ParkItems'
  | 'ParkItem'
  | 'Videos'
  | 'Video'
  | 'Page';

export type SocialShareChannel =
  'Native'
  | 'Copy'
  | 'Email'
  | 'WhatsApp'
  | 'Telegram'
  | 'LinkedIn'
  | 'Facebook'
  | 'X'
  | 'Reddit'
  | 'QrCode';

export type SocialShareVisitorKind = 'Anonymous' | 'Authenticated';

export interface CaptureSocialShareEventRequest {
  targetType: SocialShareTargetType;
  targetId?: string | null;
  targetTitle?: string | null;
  url: string;
  languageCode?: string | null;
  channel: SocialShareChannel;
}

export interface CaptureSocialShareEventResponse {
  accepted: boolean;
  occurredAtUtc: string;
}

export interface SocialShareStatsQuery {
  fromUtc?: string | null;
  toUtc?: string | null;
}

export interface SocialShareDailyStatsPoint {
  date: string;
  count: number;
}

export interface SocialShareDimensionCount {
  key: string;
  count: number;
}

export interface SocialShareTopTarget {
  targetType: SocialShareTargetType;
  targetId: string | null;
  targetTitle: string | null;
  url: string;
  count: number;
}

export interface SocialShareStatsResult {
  fromUtc: string;
  toUtc: string;
  totalEvents: number;
  anonymousEvents: number;
  authenticatedEvents: number;
  daily: SocialShareDailyStatsPoint[];
  channels: SocialShareDimensionCount[];
  targetTypes: SocialShareDimensionCount[];
  visitorKinds: SocialShareDimensionCount[];
  topTargets: SocialShareTopTarget[];
}
