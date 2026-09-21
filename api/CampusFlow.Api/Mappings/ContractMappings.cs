using CampusFlow.Api.Contracts;
using CampusFlow.Api.Data.Entities;

namespace CampusFlow.Api.Mappings;

public static class ContractMappings
{
    public static UserProfileResponse ToResponse(this UserProfileEntity user) => new(
        user.UserId,
        user.FullName,
        user.Email,
        user.ProfileImage,
        user.Institution,
        user.Course,
        user.YearOfStudy);

    public static UserSettingsResponse ToResponse(this UserSettingsEntity settings) => new(
        settings.Theme,
        settings.Language,
        settings.NotificationsEnabled,
        settings.PomodoroWorkMinutes,
        settings.PomodoroBreakMinutes);

    public static ModuleResponse ToResponse(this ModuleEntity module) => new(
        module.ModuleId,
        module.ModuleCode,
        module.ModuleName,
        module.Lecturer,
        module.Colour);

    public static TimetableEventResponse ToResponse(this TimetableEventEntity timetableEvent) => new(
        timetableEvent.EventId,
        timetableEvent.ModuleId,
        timetableEvent.EventType,
        timetableEvent.DayOfWeek,
        timetableEvent.StartTime,
        timetableEvent.EndTime,
        timetableEvent.Venue,
        timetableEvent.RecurrenceEndDate);
}
