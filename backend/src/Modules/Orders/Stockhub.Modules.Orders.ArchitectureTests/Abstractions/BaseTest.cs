using System.Reflection;

namespace Stockhub.Modules.Orders.ArchitectureTests.Abstractions;

public abstract class BaseTest
{
    protected static readonly Assembly DomainAssembly = typeof(Domain.Orders.Order).Assembly;

    protected static readonly Assembly ApplicationAssembly = typeof(Application.AssemblyReference).Assembly;

    protected static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.OrdersModule).Assembly;

    protected static readonly Assembly PresentationAssembly = typeof(Presentation.AssemblyReference).Assembly;
}
