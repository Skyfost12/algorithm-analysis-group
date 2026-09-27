using Grafos.Service;

namespace Grafos
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo {
                    Title = "RutaOptima API",
                    Version = "v1",
                    Description = "API que calcula la ruta óptima entre dos puntos de un barrio " +
                                  "usando Dijkstra y A*, para el examen de Análisis de Algoritmos."
                });
            });

            // Nuestro grafo se carga una sola vez y se comparte entre peticiones.
            builder.Services.AddSingleton<GrafoService>();

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

            app.UseDefaultFiles();
            app.UseStaticFiles();

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
