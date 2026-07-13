namespace OpenWealth.Api.Models;

public class Property
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public decimal EstimatedValue { get; set; }

    public List<Mortgage> Mortgages { get; set; } = [];
}
