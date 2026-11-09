using BankDispatcher.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<IRabbitMqPublisher, RabbitMqPublisher>();

var app = builder.Build();

app.MapControllers(); 

app.Run();