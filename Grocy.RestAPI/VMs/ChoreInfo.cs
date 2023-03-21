namespace Grocy.RestAPI.VMs;

public record ChoreInfo(int Id, int ChoreId, string ChoreName, string? LastTrackedTime, string? NextEstimatedExecutionTime, int TrackDateOnly, string? NextExecutionAssignedToUserId, int IsRescheduled, int IsReassigned, string? NextExecutionAssignedUser);