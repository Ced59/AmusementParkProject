export type SocialNetwork = 'Facebook';
export type SocialPublicationStatus = 'Pending' | 'Published' | 'Failed' | 'Deleted';
export type SocialPublicationTrigger = 'Manual' | 'AutomaticParkPublication';

export interface SocialPublisher {
  readonly network: SocialNetwork;
  readonly displayName: string;
  readonly isEnabled: boolean;
  readonly isConfigured: boolean;
  readonly targetUrl: string | null;
  readonly supportsAutomaticParkAnnouncements: boolean;
}

export interface SocialPublication {
  readonly id: string;
  readonly network: SocialNetwork;
  readonly status: SocialPublicationStatus;
  readonly trigger: SocialPublicationTrigger;
  readonly message: string;
  readonly url: string;
  readonly sourceEntityType: string | null;
  readonly sourceEntityId: string | null;
  readonly requestedAtUtc: string;
  readonly attemptedAtUtc: string | null;
  readonly publishedAtUtc: string | null;
  readonly deletedAtUtc: string | null;
  readonly lastSynchronizedAtUtc: string | null;
  readonly externalPostId: string | null;
  readonly externalPostUrl: string | null;
  readonly failureCode: string | null;
  readonly failureMessage: string | null;
}

export interface SocialPublishingOverview {
  readonly publishers: SocialPublisher[];
  readonly recentPublications: SocialPublication[];
}

export interface PublishSocialLinkRequest {
  readonly network: SocialNetwork;
  readonly message: string;
  readonly url: string;
}

export interface UpdateSocialPublicationRequest {
  readonly message: string;
}

export interface SocialPublicationSynchronizationResult {
  readonly checkedCount: number;
  readonly updatedCount: number;
  readonly deletedCount: number;
  readonly failureCount: number;
}
