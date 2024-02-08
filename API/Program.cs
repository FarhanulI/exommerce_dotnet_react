using System.Text;
using API.Data;
using API.Entities;
using API.Middleware;
using API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Configure services for the application.
builder.Services.AddControllers();

// Configure Swagger/OpenAPI for API documentation.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DbContext to use PostgreSQL with the connection string from appsettings.json.
builder.Services.AddDbContext<StoreContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Cross-Origin Resource Sharing (CORS).
builder.Services.AddCors();

// Configure Identity services.
builder.Services.AddIdentityCore<User>(opt => opt.User.RequireUniqueEmail = true)
    .AddRoles<Role>()
    .AddEntityFrameworkStores<StoreContext>();

// Configure authentication and authorization services.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(opt =>
                {
                    opt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWTSettings:TokenKey"]))
                    };
                });
builder.Services.AddAuthorization();

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<PaymentService>();

// Build the application.
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionMiddleware>(); // Custom exception handling middleware.

// If in development, enable Swagger for API documentation.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable Cross-Origin Resource Sharing (CORS).
app.UseCors(opt => opt.AllowAnyHeader().AllowAnyMethod().AllowCredentials().WithOrigins("http://localhost:3000"));

// Enable authorization.
app.UseAuthorization();

// Map controllers for handling incoming requests.
app.MapControllers();

// Seed data during application startup.
var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

try
{
    // Obtain instances of DbContext and UserManager.
    var context = services.GetRequiredService<StoreContext>();
    var userManager = services.GetRequiredService<UserManager<User>>();

    // Apply pending database migrations.
    context.Database.Migrate();

    // Seed initial data using a custom DbInitializer.
    await DbInitializer.Initialize(context, userManager);
}
catch (Exception ex)
{
    // Handle exceptions during database seeding.
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while seeding the database.");
}

// Start the application.
app.Run();
