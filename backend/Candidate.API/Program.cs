using System.Reflection;
using System.Text.Json.Serialization;
using Candidate.API.Data;
using Candidate.API.Infrastructure;
using Candidate.API.Services;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddProvidersDatabase(builder.Configuration, builder.Environment);
builder.Services.AddScoped<ProviderQueryService>();
builder.Services.AddScoped<ProviderService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<DashboardService>();

builder.Services.AddControllers(options =>
        // Required fields are declared explicitly with [Required] on the request contracts.
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options =>
    {
        // Enums travel as names ("Active"), and numeric values are rejected as invalid input.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        // Malformed JSON yields a generic field error instead of exposing .NET type names.
        options.AllowInputFormatterExceptionMessages = false;
    });

// Consistent RFC 7807 error bodies for validation, business and unexpected errors.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DatabaseExceptionHandler>();

// The React dev server reaches the API through the Vite proxy (same origin), so CORS is only
// needed when the UI is served from another origin; allowed origins come from configuration.
builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml")));

var app = builder.Build();

await app.InitializeDatabaseAsync();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Not in Development: the Vite proxy calls the API over plain HTTP locally.
    app.UseHttpsRedirection();
}

app.UseCors(FrontendCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposes the entry point to WebApplicationFactory in the integration tests.
public partial class Program;
