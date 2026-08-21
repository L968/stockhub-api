using System.Reflection;

namespace Stockhub.Modules.Stocks.ArchitectureTests.Abstractions;

public abstract class BaseTest
{
    protected static readonly Assembly DomainAssembly = typeof(Domain.Stock).Assembly;

    protected static readonly Assembly ApplicationAssembly = typeof(Application.AssemblyReference).Assembly;

    protected static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.StocksModule).Assembly;

    protected static readonly Assembly PresentationAssembly = typeof(Presentation.AssemblyReference).Assembly;
}
