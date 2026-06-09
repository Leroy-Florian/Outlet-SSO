using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace Outlet.ArchitectureTests;

public abstract class ArchitectureTestBase
{
    // ==========================================================================
    // Outlet SSO is the identity / access bounded context (Outlet.Identity.*):
    // users + personal access tokens. It is a self-contained hexagon layered
    // Domain → Application → Infrastructure on top of the shared kernel.
    // The convention gates below run across the context's Domain assembly via
    // AllDomainAssemblies; BoundedContextIsolationTests keeps the context from
    // leaking into sibling Outlet contexts (Core / Cloud) that live elsewhere.
    // ==========================================================================

    protected static readonly Assembly KernelAssembly =
        typeof(Outlet.Kernel.Shared.AssemblyReference).Assembly;

    protected static readonly Assembly IdentityDomainAssembly =
        typeof(Outlet.Identity.Domain.AssemblyReference).Assembly;

    protected static readonly Assembly IdentityApplicationAssembly =
        typeof(Outlet.Identity.Application.AssemblyReference).Assembly;

    protected static readonly Assembly IdentityInfrastructureAssembly =
        typeof(Outlet.Identity.Infrastructure.AssemblyReference).Assembly;

    protected static readonly Assembly[] AllDomainAssemblies = [IdentityDomainAssembly];
    protected static readonly Assembly[] AllApplicationAssemblies = [IdentityApplicationAssembly];
    protected static readonly Assembly[] AllInfrastructureAssemblies = [IdentityInfrastructureAssembly];

    #region Given - Assembly Selection

    protected static Types GivenTypesInDomain() => Types.InAssemblies(AllDomainAssemblies);

    protected static Types GivenTypesInApplication() => Types.InAssemblies(AllApplicationAssemblies);

    protected static Types GivenTypesInInfrastructure() => Types.InAssemblies(AllInfrastructureAssemblies);

    protected static IEnumerable<Type> GivenAllTypesInDomain() =>
        AllDomainAssemblies.SelectMany(a => a.GetTypes());

    protected static IEnumerable<Type> GivenAllTypesInApplication() =>
        AllApplicationAssemblies.SelectMany(a => a.GetTypes());

    protected static IEnumerable<Type> GivenAllTypesInInfrastructure() =>
        AllInfrastructureAssemblies.SelectMany(a => a.GetTypes());

    #endregion

    #region When - Dependency Checks

    protected static TestResult WhenCheckingNoDependencyOn(Types types, params string[] forbiddenDependencies) =>
        types.ShouldNot().HaveDependencyOnAny(forbiddenDependencies).GetResult();

    #endregion

    #region Then - Assertions

    protected static void ThenShouldHaveNoViolations(TestResult result, string layerName, string dependencyType)
    {
        result.IsSuccessful.Should().BeTrue(
            $"{layerName} should not have {dependencyType} dependencies but found violations in:{Environment.NewLine}" +
            FormatViolations(result.FailingTypeNames));
    }

    #endregion

    #region Property Analysis Helpers

    protected static bool HasPublicSetters(Type type) =>
        type.GetProperties().Any(p =>
            p.SetMethod?.IsPublic == true &&
            !IsInitOnlySetter(p));

    private static bool IsInitOnlySetter(PropertyInfo property) =>
        property.SetMethod?.ReturnParameter
            .GetRequiredCustomModifiers()
            .Any(x => x.Name == "IsExternalInit") == true;

    #endregion

    #region Formatting Helpers

    protected static string FormatViolations(IEnumerable<string>? violations) =>
        violations is null ? string.Empty : string.Join(Environment.NewLine, violations.Select(v => $"  - {v}"));

    protected static string FormatTypeList(IEnumerable<string> types) =>
        string.Join(Environment.NewLine, types.Select(t => $"  - {t}"));

    #endregion
}
