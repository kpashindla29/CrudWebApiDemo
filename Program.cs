
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql; 
using CrudWebApiDemo.Models;
namespace CrudWebApiDemo;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthorization();
        
        // builder.Services.AddDbContext<Data.ProductContext>(options =>
        //             options.UseInMemoryDatabase("ProductList"));

        
        
        // Your DbContext configuration
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<Data.ProductContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        // var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        // builder.Services.AddDbContext<Data.ProductContext>(options =>
        //     options.UseMySQL(connectionString)); 
                
        // Add controllers
        builder.Services.AddControllers();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
       builder.Services.AddEndpointsApiExplorer();
       builder.Services.AddSwaggerGen();

        var app = builder.Build();


        // Seed the database
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<Data.ProductContext>();
            context.Database.EnsureCreated(); // Ensure database is created
            
            // Check if products already exist to avoid duplicates
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
                Console.WriteLine("Sample data added to database.");
            }
        }



        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        
        app.MapControllers();


        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        app.MapGet("/weatherforecast", (HttpContext httpContext) =>
        {
            var forecast =  Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = summaries[Random.Shared.Next(summaries.Length)]
                })
                .ToArray();
            return forecast;
        })
        .WithName("GetWeatherForecast");

        app.Run();
    }

   

    
}
