using BankDispatcher.Workers.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<AuditWorker>();
builder.Services.AddHostedService<AntiFraudWorker>();

var host = builder.Build();

await host.RunAsync();