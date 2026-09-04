namespace TurnosSalas.Models;

/// <summary>
/// Una solicitud de actividad enviada desde el frontend.
/// Start/End se expresan en minutos desde medianoche (ej: 9:30 -> 570).
/// </summary>
public record ActivityRequest(string Id, string Name, string Room, int Start, int End);

/// <summary>
/// Una actividad ya procesada por el algoritmo (aceptada).
/// </summary>
public record ScheduledActivity(string Id, string Name, string Room, int Start, int End);

/// <summary>
/// Una actividad rechazada por el algoritmo, con el motivo.
/// </summary>
public record RejectedActivity(string Id, string Name, string Room, int Start, int End, string Reason);

/// <summary>
/// Resultado completo de una corrida del algoritmo greedy.
/// </summary>
public record ScheduleResult(List<ScheduledActivity> Selected, List<RejectedActivity> Rejected);

/// <summary>
/// Cuerpo esperado del POST /api/schedule.
/// </summary>
public record ScheduleRequestBody(List<ActivityRequest> Activities);

/// <summary>
/// Una entrada en el historial de corridas (para estadísticas).
/// </summary>
public record HistoryEntry(string Timestamp, int TotalSolicitadas, int TotalAsignadas, int TotalRechazadas);