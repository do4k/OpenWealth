using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Requests;

public record ShareSettingsRequest(bool Enabled, string? Passphrase, ShareVisibility Visibility);
