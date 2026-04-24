namespace KnowHub.Domain.Enums;

public enum SurveyInvitationStatus
{
    Pending   = 0, // Invitation record created, email not yet sent (queued in background job)
    Sent      = 1, // Email successfully delivered; awaiting response
    Submitted = 2, // Employee has submitted their response; token is permanently invalidated
    Expired   = 3, // Token expiry date has passed without a submission
    Failed    = 4, // Email delivery failed (SMTP / SES error); admin can resend
}
