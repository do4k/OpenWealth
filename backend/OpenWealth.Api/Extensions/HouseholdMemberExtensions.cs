using OpenWealth.Api.Models;

namespace OpenWealth.Api.Extensions;

public static class HouseholdMemberExtensions
{
    /// <summary>Shapes a membership row and its user for the household member list.</summary>
    public static object ToMemberView(this HouseholdMember member, User user, Guid viewerUserId) => new
    {
        MembershipId = member.Id,
        UserId = user.Id,
        user.DisplayName,
        user.Email,
        member.Status,
        member.Visibility,
        IsMe = user.Id == viewerUserId,
    };
}
