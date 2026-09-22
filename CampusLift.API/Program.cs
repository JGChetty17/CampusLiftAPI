using Supabase;

namespace CampusLift.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Controllers and Swagger
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Supabase
            var url = builder.Configuration["Supabase:Url"]
                ?? throw new InvalidOperationException("Supabase:Url is missing.");
            var key = builder.Configuration["Supabase:Key"]
                ?? throw new InvalidOperationException("Supabase:Key is missing.");

            var options = new SupabaseOptions
            {
                AutoRefreshToken = false,
                AutoConnectRealtime = false
            };

            var supabase = new Supabase.Client(url, key, options);
            await supabase.InitializeAsync();

            builder.Services.AddSingleton(supabase);

            // CORS for Android emulator / device
            builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
                p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors();
            app.MapControllers();

            app.Run();
        }
    }
    
}
public partial class Program { }