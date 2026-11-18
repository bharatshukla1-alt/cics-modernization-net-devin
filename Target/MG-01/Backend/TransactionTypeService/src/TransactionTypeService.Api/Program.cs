using Microsoft.EntityFrameworkCore;
using TransactionTypeService.Api.Error;
using TransactionTypeService.Api.Repositories;
using TransactionTypeService.Api.Security;
using TransactionTypeService.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// PostgreSQL when ConnectionStrings:DefaultConnection is set; in-memory store otherwise (local/dev only)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TransactionTypeContext>(options =>
{
    if (string.IsNullOrWhiteSpace(connectionString))
        options.UseInMemoryDatabase("CICS_DB");
    else
        options.UseNpgsql(connectionString);
});

// Configure Authentication & Authorization
builder.Services.AddSecurityConfiguration(builder.Configuration);

// Register application services
builder.Services.AddScoped<ITransactionTypeService, TransactionTypeService.Api.Services.TransactionTypeService>();

// Add DB Health Check
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TransactionTypeContext>("Database");

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionHandlerMiddleware>();

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
