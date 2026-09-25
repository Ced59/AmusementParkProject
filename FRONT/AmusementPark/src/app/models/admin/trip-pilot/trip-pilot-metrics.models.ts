export interface TripPilotMetricsResult {
  generatedAtUtc: string;
  totalPlans: number;
  collaborativePlans: number;
  plansWithPreferences: number;
  plansWithDecisions: number;
  expiredInvitations: number;
  enabledNotificationSubscriptions: number;
  auditEvents: number;
  pendingAuditMarkers: number;
  activityCounts: Readonly<Record<string, number>>;
}
