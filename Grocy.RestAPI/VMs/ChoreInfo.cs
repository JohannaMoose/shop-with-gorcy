namespace Grocy.RestAPI.VMs;

public record ChoreInfo(string Id, string ChoreId, string ChoreName, DateTime LastTrackedTime, DateTime NextEstimatedExecutionTime, bool TrackDateOnly, string? NextExecutionAssignedToUserId, bool IsRescheduled, bool IsReassigned, string? NextExecutionAssignedUser);