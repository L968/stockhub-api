using Microsoft.AspNetCore.Routing;

namespace Stockhub.Common.Presentation.Endpoints;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
