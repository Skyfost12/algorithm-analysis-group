using RutaOptima.Servicios;

namespace RutaOptima {
    public class Program {
        public static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            // --- Servicios ---
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo {
                    Title = "RutaOptima API",
                    Version = "v1",
                    Description = "API que calcula la ruta óptima de un repartidor (depósito, entregas y regreso) " +
                                  "con Programación Dinámica (Held-Karp), para el examen de Análisis de Algoritmos."
                });
            });

            // Nuestro grafo se carga una sola vez y se comparte entre peticiones.
            builder.Services.AddSingleton<GrafoService>();

            // Ruta óptima de repartidor (Held-Karp). La fuente de distancias es intercambiable:
            // hoy son distancias por calle sobre el grafo; para usar OSRM/Google bastaría otra IDistanceProvider.
            builder.Services.AddSingleton<IDistanceProvider, StreetGraphDistanceProvider>();
            builder.Services.AddSingleton<RepartidorService>();

            // CORS abierto para poder conectar libremente el frontend (Leaflet/JS)
            // que corre en un origen distinto (ej. file:// o localhost:otro-puerto).
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("PermitirFrontend", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // --- Pipeline HTTP ---
            // El frontend (wwwroot/index.html) queda en la raíz "/".
            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Swagger disponible siempre, en /swagger, para probar la API sin Postman.
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "RutaOptima API v1");
                options.RoutePrefix = "swagger";
            });

            app.UseCors("PermitirFrontend");
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
