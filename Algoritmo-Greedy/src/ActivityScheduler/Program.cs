using TurnosSalas.Algorithms;
using TurnosSalas.Models;
using TurnosSalas.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<HistoryService>();

var app = builder.Build();

app.UseDefaultFiles();   
app.UseStaticFiles();    

app.MapPost("/api/schedule", async (ScheduleRequestBody body, HistoryService history) =>
{
    if (body?.Activities is null)
    {
        return Results.BadRequest(new { error = "Se esperaba un arreglo 'activities'." });
    }

    foreach (var a in body.Activities)
    {
        if (a.End <= a.Start)
        {
            return Results.BadRequest(new
            {
                error = $"Actividad inválida: \"{a.Name ?? a.Id}\" tiene horario inconsistente."
            });
        }
    }

    var result = GreedyScheduler.SelectActivities(body.Activities);

    var entry = new HistoryEntry(
        Timestamp: DateTime.UtcNow.ToString("o"),
        TotalSolicitadas: body.Activities.Count,
        TotalAsignadas: result.Selected.Count,
        TotalRechazadas: result.Rejected.Count);

    await history.AppendAsync(entry);

    return Results.Ok(result);
});

app.MapGet("/api/history", async (HistoryService history) =>
{
    var entries = await history.ReadAsync();
    return Results.Ok(entries);
});

app.Run();
