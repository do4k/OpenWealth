using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Responses;

public record HouseholdMemberView(
    Guid MembershipId,
    Guid UserId,
    string DisplayName,
    string Email,
    HouseholdMemberStatus Status,
    ShareVisibility Visibility,
    bool IsMe);
