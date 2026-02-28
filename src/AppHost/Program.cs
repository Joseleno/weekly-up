var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("weeklyup-postgres-data")
    .WithPgAdmin();

var weeklyupDb = postgres.AddDatabase("weeklyup");

var redis = builder.AddRedis("redis")
    .WithDataVolume("weeklyup-redis-data");

var seq = builder.AddSeq("seq")
    .ExcludeFromManifest();

var api = builder.AddProject<Projects.Api>("api")
    .WithEnvironment("ASPNETCORE_URLS", "https://localhost:7001;http://localhost:5038")
    .WithEndpoint("http", e => { e.Port = 5038; e.TargetPort = 5038; e.IsProxied = false; })
    .WithEndpoint("https", e => { e.Port = 7001; e.TargetPort = 7001; e.IsProxied = false; })
    .WithEnvironment("ConnectionStrings__Database", weeklyupDb.Resource.ConnectionStringExpression)
    .WithEnvironment("ConnectionStrings__Redis", redis.Resource.ConnectionStringExpression)
    .WithReference(seq)
    .WaitFor(postgres)
    .WaitFor(redis);

builder.AddProject<Projects.WebApp>("webapp")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
