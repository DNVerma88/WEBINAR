export enum MentorMenteeStatus {
  Pending = 'Pending',
  Active = 'Active',
  Completed = 'Completed',
  Declined = 'Declined',
}

export interface MentorMenteeDto {
  id: string;
  mentorId: string;
  mentorName: string;
  menteeId: string;
  menteeName: string;
  status: MentorMenteeStatus;
  startedAt?: string;
  endedAt?: string;
  goalsText?: string;
  matchReason?: string;
  createdDate: string;
}

export interface RequestMentorRequest {
  mentorId: string;
  goalsText?: string;
}
