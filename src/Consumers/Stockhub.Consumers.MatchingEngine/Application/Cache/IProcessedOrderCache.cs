namespace Stockhub.Consumers.MatchingEngine.Application.Cache;

public interface IProcessedOrderCache
{
    bool Exists(Guid orderId);
    void Add(Guid orderId);
    void Remove(Guid orderId);
}
