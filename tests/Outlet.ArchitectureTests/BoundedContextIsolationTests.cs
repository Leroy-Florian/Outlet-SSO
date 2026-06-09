using System.Reflection;
using NetArchTest.Rules;

namespace Outlet.ArchitectureTests;

/// <summary>
/// The Identity context owns its own model. It communicates with sibling Outlet
/// contexts (the registry/install engine <c>Outlet.Core.*</c> and the cloud
/// <c>Outlet.Cloud.*</c>) by id and plain strings, never by importing their types —
/// in particular, issuing a scoped token must not turn into an Identity→Cloud
/// dependency: scopes cross the boundary as plain strings. Those contexts live in
/// other repositories, so the guard is expressed against their namespace prefixes
/// to stay honest even though their assemblies are absent here.
/// </summary>
public sealed class BoundedContextIsolationTests : ArchitectureTestBase
{
    private static readonly Assembly[] IdentityContext = [IdentityDomainAssembly, IdentityApplicationAssembly, IdentityInfrastructureAssembly];

    private static readonly string[] ForeignContextNamespaces = ["Outlet.Core", "Outlet.Cloud"];

    [Fact]
    public void Identity_ShouldNot_DependOn_OtherContexts()
    {
        var result = Types.InAssemblies(IdentityContext)
            .ShouldNot()
            .HaveDependencyOnAny(ForeignContextNamespaces)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"The Identity context must not depend on Core or Cloud:{Environment.NewLine}" +
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }
}
