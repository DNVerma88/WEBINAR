import axiosClient from './axiosClient';
import type {
  SpeakerAvailabilityDto,
  SpeakerBookingDto,
  SetAvailabilityRequest,
  UpdateAvailabilityRequest,
  GetAvailableSpeakersRequest,
  RequestBookingRequest,
  RespondToBookingRequest,
  GetMyBookingsRequest,
  AdminAssignRequest,
  PagedList,
} from '../types';

export const speakerMarketplaceApi = {
  setAvailability: (data: SetAvailabilityRequest) =>
    axiosClient.post<SpeakerAvailabilityDto>('/speaker-marketplace/availability', data).then((r) => r.data),

  updateAvailability: (id: string, data: UpdateAvailabilityRequest) =>
    axiosClient.put<SpeakerAvailabilityDto>(`/speaker-marketplace/availability/${id}`, data).then((r) => r.data),

  deleteAvailability: (id: string) =>
    axiosClient.delete(`/speaker-marketplace/availability/${id}`).then((r) => r.data),

  getAvailableSpeakers: (params?: GetAvailableSpeakersRequest, signal?: AbortSignal) =>
    axiosClient.get<PagedList<SpeakerAvailabilityDto>>('/speaker-marketplace/available', { params, signal }).then((r) => r.data),

  getMyAvailability: (signal?: AbortSignal) =>
    axiosClient.get<SpeakerAvailabilityDto[]>('/speaker-marketplace/availability/my', { signal }).then((r) => r.data),

  requestBooking: (availabilityId: string, data: RequestBookingRequest) =>
    axiosClient.post<SpeakerBookingDto>('/speaker-marketplace/bookings', { speakerAvailabilityId: availabilityId, ...data }).then((r) => r.data),

  respondToBooking: (bookingId: string, data: RespondToBookingRequest) =>
    axiosClient.put<SpeakerBookingDto>(`/speaker-marketplace/bookings/${bookingId}/respond`, data).then((r) => r.data),

  getMyBookings: (params?: GetMyBookingsRequest, signal?: AbortSignal) =>
    axiosClient.get<PagedList<SpeakerBookingDto>>('/speaker-marketplace/bookings', { params, signal }).then((r) => r.data),

  linkToSession: (bookingId: string, sessionId: string) =>
    axiosClient.put<SpeakerBookingDto>(`/speaker-marketplace/bookings/${bookingId}/link-session`, { sessionId }).then((r) => r.data),

  adminAssign: (data: AdminAssignRequest) =>
    axiosClient.post<SpeakerBookingDto>('/speaker-marketplace/admin-assign', data).then((r) => r.data),
};
