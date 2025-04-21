using Microsoft.EntityFrameworkCore;
using WasteManagement3.Data;
using WasteManagement3.Services;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<AuthService>();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WasteCollection API",
        Version = "v1",
        Description = "API for waste collection management"
    });

    // JWT configuration for Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// JWT Authentication Configuration
var secretKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddLogging();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600; // 100 MB
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WasteManagement3 v1");
    });
}

// Check for command-line argument before middleware setup
if (args.Contains("update-weekly-stats"))
{
    await UpdateWeeklyStats(app.Services);
    Console.WriteLine("✅ WeeklyStats table updated successfully!");
    return;
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Request logging middleware
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    var request = await FormatRequest(context.Request);
    Console.WriteLine($"RAW REQUEST:\n{request}");
    await next();
});

app.MapControllers();

app.MapGet("/", () => "Waste Management API is running...");

app.Run();

async Task<string> FormatRequest(HttpRequest request)
{
    var body = request.Body;
    request.Body.Position = 0;
    var buffer = new byte[Convert.ToInt32(request.ContentLength)];
    await request.Body.ReadAsync(buffer, 0, buffer.Length);
    request.Body.Position = 0;

    return $"{request.Method} {request.Path}{request.QueryString}\n" +
           $"Headers:\n{string.Join("\n", request.Headers.Select(h => $"{h.Key}: {h.Value}"))}\n" +
           $"Body:\n{Encoding.UTF8.GetString(buffer)}";
}

static async Task UpdateWeeklyStats(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var stats = await context.WeeklyStats.ToListAsync();
    foreach (var stat in stats)
    {
        stat.TotalQuantity += 5; // Adjust this with your actual logic
    }
    await context.SaveChangesAsync();
}