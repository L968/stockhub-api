namespace Stockhub.Consumers.MatchingEngine.Domain.Events;

internal sealed class OrderEventPayload
{
    public Guid Id { get; set; }
    public Guid User_Id { get; set; }
    public Guid Stock_Id { get; set; }
    public int Side { get; set; }
    public string Price { get; set; } = default!;
    public int Quantity { get; set; }
    public int Filled_Quantity { get; set; }
    public int Status { get; set; }
    public DateTime Created_At { get; set; }
    public DateTime Updated_At { get; set; }
}
