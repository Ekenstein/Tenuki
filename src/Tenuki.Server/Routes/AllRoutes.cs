using Tenuki.Server.SignalR;

namespace Tenuki.Server.Routes;

public static class AllRoutes
{
    public static void Register(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api");
        group.MapHub<TenukiHub>("hub");
        GameRoutes.Register(group);
    }
}
