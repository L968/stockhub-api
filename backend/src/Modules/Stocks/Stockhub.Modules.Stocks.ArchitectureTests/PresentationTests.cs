using NetArchTest.Rules;
using Stockhub.Common.Presentation.Endpoints;
using Stockhub.Modules.Stocks.ArchitectureTests.Abstractions;

namespace Stockhub.Modules.Stocks.ArchitectureTests;

public class PresentationTests : BaseTest
{
    [Fact]
    public void Endpoint_Should_NotBePublic()
    {
        Types.InAssembly(PresentationAssembly)
            .That()
            .ImplementInterface(typeof(IEndpoint))
            .Should()
            .NotBePublic()
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void Endpoint_Should_BeSealed()
    {
        Types.InAssembly(PresentationAssembly)
            .That()
            .ImplementInterface(typeof(IEndpoint))
            .Should()
            .BeSealed()
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void Endpoint_ShouldHave_NameEndingWith_Endpoint()
    {
        Types.InAssembly(PresentationAssembly)
            .That()
            .ImplementInterface(typeof(IEndpoint))
            .Should()
            .HaveNameEndingWith("Endpoint")
            .GetResult()
            .ShouldBeSuccessful();
    }
}
