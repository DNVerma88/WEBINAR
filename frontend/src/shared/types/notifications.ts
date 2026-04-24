export enum NotificationType {
  ProposalSubmitted = 'ProposalSubmitted',
  ProposalApproved = 'ProposalApproved',
  ProposalRejected = 'ProposalRejected',
  ProposalRevisionRequested = 'ProposalRevisionRequested',
  SessionScheduled = 'SessionScheduled',
  SessionReminder = 'SessionReminder',
  SessionStarted = 'SessionStarted',
  SessionCancelled = 'SessionCancelled',
  RegistrationConfirmed = 'RegistrationConfirmed',
  WaitlistPromoted = 'WaitlistPromoted',
  CommentAdded = 'CommentAdded',
  BadgeAwarded = 'BadgeAwarded',
  KnowledgeRequestUpvoted = 'KnowledgeRequestUpvoted',
  KnowledgeRequestClaimed = 'KnowledgeRequestClaimed',
  NewFollower = 'NewFollower',
  MaterialAdded = 'MaterialAdded',
  General = 'General',
  MentoringRequestReceived = 'MentoringRequestReceived',
  MentoringRequestAccepted = 'MentoringRequestAccepted',
  StreakMilestone = 'StreakMilestone',
  LearningPathCompleted = 'LearningPathCompleted',
}

export interface NotificationDto {
  id: string;
  notificationType: NotificationType;
  title: string;
  body: string;
  relatedEntityType?: string;
  relatedEntityId?: string;
  isRead: boolean;
  readAt?: string;
  createdDate: string;
}

export interface GetNotificationsRequest {
  isRead?: boolean;
  page?: number;
  pageSize?: number;
}
