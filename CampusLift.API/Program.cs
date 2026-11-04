using CampusLift.API.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
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

            // ────────────────────────────────────────────────────────
            // Firebase Admin — server-side FCM sender
            // ────────────────────────────────────────────────────────
            // Credentials are loaded from config key "Firebase:CredentialsJson"
            // which must contain the FULL service-account JSON as a base64 string.
            //
            // Local dev:
            //   dotnet user-secrets set "Firebase:CredentialsJson" "<base64>"
            // GitHub Actions:
            //   Secret FIREBASE_CREDENTIALS_BASE64 → env var Firebase__CredentialsJson
            //
            // If not configured, we skip init gracefully so the app still boots
            // (useful in CI where you may not want to send pushes).
            // ────────────────────────────────────────────────────────
            var firebaseCredsB64 = builder.Configuration["Firebase:CredentialsJson"];
            if (!string.IsNullOrWhiteSpace(firebaseCredsB64))
            {
                try
                {
                    var credsJson = System.Text.Encoding.UTF8.GetString(
                        Convert.FromBase64String(firebaseCredsB64));

                    if (FirebaseApp.DefaultInstance == null)
                    {
                        FirebaseApp.Create(new AppOptions
                        {
                            Credential = GoogleCredential.FromJson(credsJson)
                        });
                    }

                    Console.WriteLine("[Firebase] Admin SDK initialized successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Firebase] WARNING: failed to initialize Admin SDK: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("[Firebase] WARNING: 'Firebase:CredentialsJson' not set — FCM will be skipped.");
            }

            // Our FCM wrapper — singleton, safe to call even if Firebase isn't initialized
            builder.Services.AddSingleton<FcmService>();
            builder.Services.AddSingleton<SupabaseStorageService>();

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

            app.Run("http://0.0.0.0:5056");
        }
    }
}
public partial class Program { }