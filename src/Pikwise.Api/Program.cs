using Pikwise.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
var app = builder.Build();

app.MapHealthChecks("/health");

app.Run();

public partial class Program;
