namespace OpenWealth.Api.Contracts.Requests;

public record RegisterRequest(string Email, string Password, string DisplayName);
