namespace CampusFlow.Api.Contracts;

public sealed record DashboardSummaryResponse(
    int ModuleCount,
    IReadOnlyList<TimetableEventResponse> UpcomingEvents);

public sealed record DailyQuoteResponse(
    string Quote,
    string? Author);
