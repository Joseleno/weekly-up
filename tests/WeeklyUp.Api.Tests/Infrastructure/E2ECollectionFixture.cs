using WeeklyUp.Api.Tests.Infrastructure;

namespace WeeklyUp.Api.Tests.Infrastructure;

/// <summary>
/// Define a collection "E2E" que compartilha o DatabaseFixture
/// entre todos os testes E2E (um único banco criado/destruído por suite).
/// </summary>
[CollectionDefinition("E2E")]
public sealed class E2ETests : ICollectionFixture<DatabaseFixture>;
