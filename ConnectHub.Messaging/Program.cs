using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ConnectHub.Messages.Repository.Data;
using ConnectHub.Messages.Repositories;
using ConnectHub.Messages.Repositories.Interface;
using ConnectHub.Messages.Services;
using ConnectHub.MessageService.Models;
using ConnectHub.Messages.Services.Interface;
using ConnectHub.Messaging.Middleware;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Configuration (PostgreSQL/Neon)
builder.Services.AddDbContext<MessageDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Dependency Injection - Mapping Interfaces to Implementations
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddHttpClient();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ConnectHub.Messaging.Consumers.MessageSentConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitUri = builder.Configuration["RabbitMQ:ConnectionUri"];
        if (!string.IsNullOrEmpty(rabbitUri))
        {
            cfg.Host(new Uri(rabbitUri));
        }
        else
        {
            cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
            {
                h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
                h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
            });
        }

        cfg.ReceiveEndpoint("message-sent-queue", e =>
        {
            e.ConfigureConsumer<ConnectHub.Messaging.Consumers.MessageSentConsumer>(context);
        });
    });
});

// 3. Controller Configuration
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


// 4. JWT Authentication Setup 
// Note: The Key, Issuer, and Audience MUST match the Auth-Service settings
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// 5. Swagger with Bearer Token support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "ConnectHub Message Service", Version = "v1" });
    
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "*" };
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            if (allowedOrigins.Contains("*"))
            {
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            }
            else
            {
                policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
            }
        }
    });
});


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MessageDbContext>();
    db.Database.Migrate();
}

// 6. Middleware Pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

// Enable Swagger in all environments for deployment testing
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ConnectHub Messaging API V1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowFrontend");

// app.UseHttpsRedirection();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
