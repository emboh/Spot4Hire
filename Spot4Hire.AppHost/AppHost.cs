var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);

var sql = builder.AddSqlServer("sql", password: sqlPassword, port: 14330)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);
var db = sql.AddDatabase("spot4hiredb");

var redis = builder.AddRedis("redis")
    .WithLifetime(ContainerLifetime.Persistent);

// Uses the Ollama app installed on this machine (not a container), so the
// downloaded models and the GPU are reused. The value lives in
// ConnectionStrings:chat (appsettings.Development.json). Ollama must be running.
var chat = builder.AddConnectionString("chat");

builder.AddProject<Projects.Spot4Hire_Backend>("backend")
    .WithReference(db)
    .WithReference(redis)
    .WithReference(chat)
    .WaitFor(db)
    .WaitFor(redis);

builder.Build().Run();
