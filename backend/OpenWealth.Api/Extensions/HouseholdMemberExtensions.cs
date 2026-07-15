using OpenWealth.Api.Contracts.Responses;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Extensions;

public static class HouseholdMemberExtensions
{
    /// <summary>Shapes a membership row and its user for the household member list.</summary>
    public static HouseholdMemberView ToMemberView(this HouseholdMember member, User user, Guid viewerUserId) => new(
        MembershipId: member.Id,
        UserId: user.Id,
        DisplayName: user.DisplayName,
        Email: user.Email,
        Status: member.Status,
        Visibility: member.Visibility,
        IsMe: user.Id == viewerUserId);
}
