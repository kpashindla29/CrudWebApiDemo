using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using CrudWebApiDemo.Models;
using Microsoft.IdentityModel.Tokens;

namespace CrudWebApiDemo;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        
        // Configure Swagger with Azure AD OAuth
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo 
            { 
                Title = "Product API", 
                Version = "v1",
                Description = "Product Management API with Azure AD Authentication"
            });
            
            // Add OAuth2 security definition for Azure AD
            c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri($"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/oauth2/v2.0/authorize"),
                        TokenUrl = new Uri($"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/oauth2/v2.0/token"),
                        Scopes = new Dictionary<string, string>
                        {
                            { $"api://{builder.Configuration["AzureAd:ClientId"]}/access_as_user", "Access the API" }
                        }
                    },
                    // For client credentials flow
                    ClientCredentials = new OpenApiOAuthFlow
                    {
                        TokenUrl = new Uri($"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/oauth2/v2.0/token"),
                        Scopes = new Dictionary<string, string>
                        {
                            { $"api://{builder.Configuration["AzureAd:ClientId"]}/.default", "Access the API" }
                        }
                    }
                }
            });
            
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "oauth2"
                        }
                    },
                    new[] { $"api://{builder.Configuration["AzureAd:ClientId"]}/access_as_user" }
                }
            });
        });

        // REPLACE JWT with Azure AD Authentication
        // builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        //     .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
        //     .EnableTokenAcquisitionToCallDownstreamApi()
        //     .AddInMemoryTokenCaches();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = "https://login.microsoftonline.com/<Tenent-ID>";
            options.Audience = "api://<Client-ID>";
            
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "https://sts.windows.net/e1a3709b-5e96-40af-9f96-dfec799a4803/",
                ValidAudience = "api://<Client-ID>"
            };
        });

        // Or if you want to keep both JWT and Azure AD (hybrid approach)
        // builder.Services.AddAuthentication()
        //     .AddJwtBearer("JWT", options =>
        //     {
        //         // Keep your existing JWT config here
        //     })
        //     .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"), "AzureAD");

        builder.Services.AddAuthorization(options =>
        {
            // You can create specific policies
            options.AddPolicy("RequireAccess", policy =>
                policy.RequireAuthenticatedUser());
                
            // Policy that requires specific scope
            options.AddPolicy("AccessAsUser", policy =>
                policy.RequireClaim("scp", "access_as_user"));
        });

        // Configure DbContext
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<Data.ProductContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        var app = builder.Build();

        // Middleware order
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product API v1");
                c.OAuthClientId(builder.Configuration["AzureAd:ClientId"]);
                c.OAuthUsePkce();
                c.OAuthScopeSeparator(" ");
            });
        }

        app.UseHttpsRedirection();

        // Authentication must come before Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Seed database
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<Data.ProductContext>();
            context.Database.EnsureCreated();

            if (!context.Products.Any())
            {
                context.Products.AddRange(
                    new Product { Name = "Laptop", Price = 999.99m, Description = "High-performance laptop" },
                    new Product { Name = "Mouse", Price = 25.50m, Description = "Wireless mouse" },
                    new Product { Name = "Keyboard", Price = 75.00m, Description = "Mechanical keyboard" },
                    new Product { Name = "Monitor", Price = 299.99m, Description = "27-inch 4K monitor" },
                    new Product { Name = "Headphones", Price = 149.99m, Description = "Noise-cancelling headphones" }
                );
                context.SaveChanges();
            }
        }

        app.Run();
    }
}