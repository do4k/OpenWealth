namespace OpenWealth.Api.Models;

/// <summary>
/// A group of linked users who've opted to share a combined view of their
/// wealth with each other. Joining a household changes nothing about who
/// owns or edits what — every member still owns only their own data; a
/// household just lets members see a combined summary, shaped by whatever
/// each member individually chooses to disclose to it.
/// </summary>
public class Household
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
