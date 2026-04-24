// ── Speaker Marketplace types ──────────────────────────────────────────────────

export type BookingStatus = 'Pending' | 'Accepted' | 'Declined' | 'Completed' | 'Cancelled';

export interface SpeakerAvailabilityDto {
  id: string;
  userId: string;
  speakerName: string;
  speakerAvatarUrl?: string;
  speakerBio?: string;
  availableFrom: string;
  availableTo: string;
  isRecurring: boolean;
  recurrencePattern?: string;
  topics: string[];
  notes?: string;
  isBooked: boolean;
}

export interface SpeakerBookingDto {
  id: string;
  speakerAvailabilityId: string;
  speakerUserId: string;
  speakerName: string;
  requesterUserId: string;
  requesterName: string;
  topic: string;
  description?: string;
  status: BookingStatus;
  respondedAt?: string;
  responseNotes?: string;
  linkedSessionId?: string;
  createdDate: string;
}

export interface SetAvailabilityRequest {
  availableFrom: string;
  availableTo: string;
  isRecurring: boolean;
  recurrencePattern?: string;
  topics: string[];
  notes?: string;
}

export interface UpdateAvailabilityRequest extends SetAvailabilityRequest {}

export interface GetAvailableSpeakersRequest {
  topic?: string;
  from?: string;
  to?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface RequestBookingRequest {
  topic: string;
  description?: string;
  intendedSessionId?: string;
}

export interface RespondToBookingRequest {
  isAccepted: boolean;
  responseNotes?: string;
}

export interface GetMyBookingsRequest {
  asSpeaker?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

export interface AdminAssignRequest {
  speakerAvailabilityId: string;
  sessionId: string;
  topic: string;
  description?: string;
}
