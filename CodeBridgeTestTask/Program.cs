using CodeBridgeTestTask.Contexts;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CodeBridgeTestTask
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            
            var _configuration = builder.Configuration;
            
            builder.Services.AddControllers();
            builder.Services.AddSwaggerGen();


            builder.Services.AddDbContext<DogContext>(options
                => options.UseNpgsql(_configuration.GetConnectionString("DogConn")));
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddFixedWindowLimiter("fixed", config =>
                {
                    config.PermitLimit = 10;
                    config.Window = TimeSpan.FromSeconds(10);
                    config.QueueLimit = 0;
                });
            });
            var app = builder.Build();

            
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
           
            app.UseRouting();
            app.UseRateLimiter();
            app.MapControllers();
            

            app.Use(async (context, next) =>
            {
                Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
                await next.Invoke();
                Console.WriteLine($"Response: {context.Response.StatusCode}");
            });
            
            app.Run();
        }
    }
}
