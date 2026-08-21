using System.Reflection;
using NetArchTest.Rules;
using Stockhub.ArchitectureTests.Abstractions;
using Stockhub.Modules.Orders.Infrastructure;
using Stockhub.Modules.Stocks.Infrastructure;
using Stockhub.Modules.Users.Infrastructure;

namespace Stockhub.ArchitectureTests.Layers;

public class ModuleTests : BaseTest
{
    [Fact]
    public void UsersModule_ShouldNotHaveDependencyOn_AnyOtherModule()
    {
        string[] otherModules = [OrdersNamespace, StocksNamespace];

        List<Assembly> usersAssemblies =
        [
            typeof(Modules.Users.Domain.User).Assembly,
            Modules.Users.Application.AssemblyReference.Assembly,
            Modules.Users.Presentation.AssemblyReference.Assembly,
            typeof(UsersModule).Assembly
        ];

        Types.InAssemblies(usersAssemblies)
            .Should()
            .NotHaveDependencyOnAny(otherModules)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void OrdersModule_ShouldNotHaveDependencyOn_AnyOtherModule()
    {
        string[] otherModules = [UsersNamespace, StocksNamespace];
        string[] publicApiModules = [StocksPublicApiNamespace];

        List<Assembly> ordersAssemblies =
        [
            typeof(Modules.Orders.Domain.Orders.Order).Assembly,
            Modules.Orders.Application.AssemblyReference.Assembly,
            Modules.Orders.Presentation.AssemblyReference.Assembly,
            typeof(OrdersModule).Assembly
        ];

        Types.InAssemblies(ordersAssemblies)
            .That()
            .DoNotHaveDependencyOnAny(publicApiModules)
            .Should()
            .NotHaveDependencyOnAny(otherModules)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void StocksModule_ShouldNotHaveDependencyOn_AnyOtherModule()
    {
        string[] otherModules = [UsersNamespace, OrdersNamespace];

        List<Assembly> stocksAssemblies =
        [
            typeof(Modules.Stocks.Domain.Stock).Assembly,
            Modules.Stocks.Application.AssemblyReference.Assembly,
            Modules.Stocks.Presentation.AssemblyReference.Assembly,
            typeof(StocksModule).Assembly
        ];

        Types.InAssemblies(stocksAssemblies)
            .Should()
            .NotHaveDependencyOnAny(otherModules)
            .GetResult()
            .ShouldBeSuccessful();
    }
}
