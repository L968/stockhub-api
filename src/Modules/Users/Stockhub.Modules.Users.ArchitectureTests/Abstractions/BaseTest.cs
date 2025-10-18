using System.Reflection;

namespace Stockhub.Modules.Users.ArchitectureTests.Abstractions;

public abstract class BaseTest
{
    protected static readonly Assembly DomainAssembly = typeof(Domain.Products.Product).Assembly;

    protected static readonly Assembly ApplicationAssembly = typeof(Application.AssemblyReference).Assembly;

    protected static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.UsersModule).Assembly;

    protected static readonly Assembly PresentationAssembly = typeof(Presentation.AssemblyReference).Assembly;
}
