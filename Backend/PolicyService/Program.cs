using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PolicyService.Config;
using PolicyService.Data;
using PolicyService.Repositories;
using PolicyService.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5003");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Policy Service API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter: Bearer {your JWT token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",};

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer",doc),new List<string>()
        }
    });
});
builder.Services.AddDbContext<PolicyDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("PolicyDb")));
builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
builder.Services.AddScoped<IPolicyWorkflowService, PolicyWorkflowService>();
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddHostedService<PolicyPaymentConsumer>();
builder.Services.AddHostedService<PolicyExpiryReminderPublisher>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        context.Response.StatusCode = error switch
        {
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };
        await context.Response.WriteAsJsonAsync(new { message = error?.Message ?? "An unexpected error occurred." });
    });
});

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PolicyDbContext>();
    await dbContext.Database.MigrateAsync();
}

//await PolicySeeder.SeedAsync(app);

// Re-publish PolicyCreated for all existing policies so the AdminService audit log stays current.
// This is idempotent — the AdminService deduplicates by PolicyId in its projection.
using (var scope = app.Services.CreateScope())
{
    var repository = scope.ServiceProvider.GetRequiredService<IPolicyRepository>();
    var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
    try
    {
        var policies = await repository.GetPoliciesAsync(CancellationToken.None);
        foreach (var policy in policies)
        {
            await publisher.PublishAsync("PolicyCreated", new
            {
                policy.PolicyId,
                policy.Name,
                VehicleType = policy.VehicleType.ToString(),
                policy.Premium
            }, CancellationToken.None);
        }
    }
    catch
    {
        // RabbitMQ may not be available — non-fatal, will sync on next restart
    }
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();





