using ArchitectAI.Business.Mappers;
using ArchitectAI.Business.Services;
using ArchitectAI.Business.Services.Interface;
using ArchitectAI.DomainObjects.DTOs;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();

// Configure Azure OpenAI options
builder.Services.Configure<AzureOpenAIOptions>(
    builder.Configuration.GetSection("AzureOpenAI"));

builder.Services.Configure<CosmosDbOptions>(
    builder.Configuration.GetSection(CosmosDbOptions.SectionName));

// AutoMapper configuration
builder.Services.AddAutoMapper(typeof(AssessmentProfile));

builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429;
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "POST:/api/Assessments",
            Period = "1m",
            Limit = 5  // max 5 requests per minute per IP
        },
        new RateLimitRule
        {
            Endpoint = "POST:/api/Assessments/answers",
            Period = "1m",
            Limit = 10
        }
    };
});

builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, 
    RateLimitConfiguration>();
    
var app = builder.Build();

// Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Apply a header key
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/Assessments"))
    {
        var key = context.Request.Headers["X-Api-Key"].FirstOrDefault();
        if (key != "architect-ai-2026-kb")
        {
            context.Response.StatusCode = 401;
            return;
        }
    }
    await next();
});

// Then in middleware section
app.UseIpRateLimiting();

app.Run();