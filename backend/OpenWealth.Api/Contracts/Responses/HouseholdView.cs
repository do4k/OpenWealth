using System.Text.Json.Serialization;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Responses;

/// <summary>
/// The caller's household state. When not in a household, only
/// <see cref="InHousehold"/> is present on the wire — the other fields are
/// genuinely absent (JsonIgnore on null), not just null, matching what the
/// endpoint sent before this was a named type.
/// </summary>
public record HouseholdView(
    bool InHousehold,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] HouseholdMemberStatus? MyStatus = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ShareVisibility? MyVisibility = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<HouseholdMemberView>? Members = null);
