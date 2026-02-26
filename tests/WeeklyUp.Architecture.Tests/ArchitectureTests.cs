using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace WeeklyUp.Architecture.Tests;

public sealed class ArchitectureTests
{
    // Assemblies
    private static readonly Assembly DomainAssembly =
        typeof(WeeklyUp.Domain.Common.Entity).Assembly;

    private static readonly Assembly ApplicationAssembly =
        typeof(WeeklyUp.Application.ApplicationServiceExtensions).Assembly;

    private static readonly Assembly InfrastructureAssembly =
        typeof(WeeklyUp.Infrastructure.InfrastructureServiceExtensions).Assembly;

    // -----------------------------------------------
    // REGRAS DE DEPENDÊNCIA ENTRE CAMADAS
    // -----------------------------------------------

    [Fact]
    public void Domain_ShouldNot_DependOn_Application()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("WeeklyUp.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            result.FailingTypeNames is not null
                ? string.Join(", ", result.FailingTypeNames)
                : "Domain must not depend on Application");
    }

    [Fact]
    public void Domain_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("WeeklyUp.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            result.FailingTypeNames is not null
                ? string.Join(", ", result.FailingTypeNames)
                : "Domain must not depend on Infrastructure");
    }

    [Fact]
    public void Domain_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("WeeklyUp.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            result.FailingTypeNames is not null
                ? string.Join(", ", result.FailingTypeNames)
                : "Domain must not depend on Api");
    }

    [Fact]
    public void Application_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("WeeklyUp.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            result.FailingTypeNames is not null
                ? string.Join(", ", result.FailingTypeNames)
                : "Application must not depend on Infrastructure");
    }

    [Fact]
    public void Application_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("WeeklyUp.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            result.FailingTypeNames is not null
                ? string.Join(", ", result.FailingTypeNames)
                : "Application must not depend on Api");
    }

    [Fact]
    public void Infrastructure_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("WeeklyUp.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            result.FailingTypeNames is not null
                ? string.Join(", ", result.FailingTypeNames)
                : "Infrastructure must not depend on Api");
    }

    // -----------------------------------------------
    // REGRAS DE NAMING CONVENTIONS
    // -----------------------------------------------

    [Fact]
    public void CommandHandlers_ShouldEndWith_Handler()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(Mediator.ICommandHandler<,>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            result.FailingTypeNames is not null
                ? string.Join(", ", result.FailingTypeNames)
                : "All ICommandHandler implementations must end with 'Handler'");
    }

    [Fact]
    public void QueryHandlers_ShouldEndWith_Handler()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(Mediator.IQueryHandler<,>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            result.FailingTypeNames is not null
                ? string.Join(", ", result.FailingTypeNames)
                : "All IQueryHandler implementations must end with 'Handler'");
    }

    [Fact]
    public void Validators_ShouldEndWith_Validator()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .Inherit(typeof(FluentValidation.AbstractValidator<>))
            .Should()
            .HaveNameEndingWith("Validator")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            result.FailingTypeNames is not null
                ? string.Join(", ", result.FailingTypeNames)
                : "All AbstractValidator implementations must end with 'Validator'");
    }

    [Fact]
    public void Repositories_ShouldEndWith_Repository()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace("WeeklyUp.Infrastructure.Persistence.Repositories")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Repository")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            result.FailingTypeNames is not null
                ? string.Join(", ", result.FailingTypeNames)
                : "All repository classes must end with 'Repository'");
    }

    [Fact]
    public void Interfaces_ShouldStartWith_I()
    {
        var domainResult = Types.InAssembly(DomainAssembly)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        domainResult.IsSuccessful.Should().BeTrue(
            domainResult.FailingTypeNames is not null
                ? string.Join(", ", domainResult.FailingTypeNames)
                : "All interfaces in Domain must start with 'I'");

        var appResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        appResult.IsSuccessful.Should().BeTrue(
            appResult.FailingTypeNames is not null
                ? string.Join(", ", appResult.FailingTypeNames)
                : "All interfaces in Application must start with 'I'");
    }

    // -----------------------------------------------
    // REGRAS DE LOCALIZAÇÃO (onde vivem as classes)
    // -----------------------------------------------

    [Fact]
    public void Entities_ShouldResideIn_DomainEntitiesNamespace()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(WeeklyUp.Domain.Common.Entity))
            .And()
            .AreNotAbstract()
            .Should()
            .ResideInNamespace("WeeklyUp.Domain.Entities")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            result.FailingTypeNames is not null
                ? string.Join(", ", result.FailingTypeNames)
                : "All concrete entities must reside in WeeklyUp.Domain.Entities");
    }

    [Fact]
    public void ValueObjects_ShouldResideIn_DomainValueObjectsNamespace()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(WeeklyUp.Domain.Common.ValueObject))
            .Should()
            .ResideInNamespace("WeeklyUp.Domain.ValueObjects")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            result.FailingTypeNames is not null
                ? string.Join(", ", result.FailingTypeNames)
                : "All value objects must reside in WeeklyUp.Domain.ValueObjects");
    }

    [Fact]
    public void Repositories_ShouldResideIn_InfrastructurePersistenceNamespace()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .HaveNameEndingWith("Repository")
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespace("WeeklyUp.Infrastructure.Persistence.Repositories")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            result.FailingTypeNames is not null
                ? string.Join(", ", result.FailingTypeNames)
                : "All repository implementations must reside in WeeklyUp.Infrastructure.Persistence.Repositories");
    }

    // -----------------------------------------------
    // REGRAS DE IMUTABILIDADE E DESIGN
    // -----------------------------------------------

    [Fact]
    public void InfrastructureClasses_ShouldBeSealed()
    {
        // Excludes Refit source-generated types by filtering to WeeklyUp.Infrastructure namespace only
        var nonSealedTypes = InfrastructureAssembly.GetTypes()
            .Where(t => t.IsClass
                && !t.IsAbstract
                && t.Namespace is not null
                && t.Namespace.StartsWith("WeeklyUp.Infrastructure", StringComparison.Ordinal)
                && !t.Namespace.StartsWith("WeeklyUp.Infrastructure.Persistence.Migrations", StringComparison.Ordinal)
                && !t.IsSealed)
            .Select(t => t.FullName)
            .ToList();

        nonSealedTypes.Should().BeEmpty(
            $"All concrete Infrastructure classes must be sealed. Violations: {string.Join(", ", nonSealedTypes)}");
    }

    [Fact]
    public void DomainEntities_ShouldBeSealed()
    {
        // Domain entities are sealed by design — project convention
        var nonSealedEntities = DomainAssembly.GetTypes()
            .Where(t => t.IsClass
                && t.Namespace == "WeeklyUp.Domain.Entities"
                && !t.IsSealed)
            .Select(t => t.FullName)
            .ToList();

        nonSealedEntities.Should().BeEmpty(
            $"Domain entities must be sealed (project convention). Violations: {string.Join(", ", nonSealedEntities)}");
    }

    [Fact]
    public void ApplicationDtos_ShouldBeSealed()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace("WeeklyUp.Application.Common.DTOs")
            .And()
            .AreClasses()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            result.FailingTypeNames is not null
                ? string.Join(", ", result.FailingTypeNames)
                : "All DTOs in Application.Common.DTOs must be sealed");
    }

    // -----------------------------------------------
    // REGRAS DE DEPENDÊNCIA ESPECÍFICAS
    // -----------------------------------------------

    [Fact]
    public void Domain_ShouldNot_HaveEntityFrameworkDependency()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            result.FailingTypeNames is not null
                ? string.Join(", ", result.FailingTypeNames)
                : "Domain must not reference EntityFrameworkCore");
    }

    [Fact]
    public void Application_ShouldNot_HaveEntityFrameworkDependency()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            result.FailingTypeNames is not null
                ? string.Join(", ", result.FailingTypeNames)
                : "Application must not reference EntityFrameworkCore directly — use IUnitOfWork");
    }

    [Fact]
    public void Handlers_ShouldResideIn_ApplicationNamespace()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespaceStartingWith("WeeklyUp.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            result.FailingTypeNames is not null
                ? string.Join(", ", result.FailingTypeNames)
                : "All handler classes must reside in WeeklyUp.Application namespace");
    }
}
