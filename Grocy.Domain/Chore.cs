using Grocy.RestAPI.VMs;

namespace Grocy.Domain;

public record Chore
{
    public Chore(int Id, string Name, DateTime LastTrackedTime, DateTime NextEstimatedExecutionTime, bool TrackDateOnly, bool IsPriority, IDictionary<string, string?> Userfields)
    {
        this.Id = Id;
        this.Name = Name;
        this.LastTrackedTime = LastTrackedTime;
        this.NextEstimatedExecutionTime = NextEstimatedExecutionTime;
        this.TrackDateOnly = TrackDateOnly;
        this.IsPriority = IsPriority;
        this.Userfields = Userfields;
    }

    public Chore(ChoreInfo info, RestAPI.Models.Chore baseChore)
    {
        if (info.Id != baseChore.Id)
            throw new ArgumentException("ChoreInfo and Chore must have the same Id");

        Id = info.Id;
        Name = baseChore.Name;
        if(info.LastTrackedTime != null)
            LastTrackedTime = DateTime.Parse(info.LastTrackedTime);
        if (info.NextEstimatedExecutionTime != null)
            NextEstimatedExecutionTime = DateTime.Parse(info.NextEstimatedExecutionTime);
        
        TrackDateOnly = baseChore.TrackDateOnly == 1;
        IsPriority = baseChore.Userfields["priority"] == "1";
        Userfields = baseChore.Userfields;
    }

    public int Id { get; init; }
    public string Name { get; init; }
    public DateTime? LastTrackedTime { get; init; }
    public DateTime NextEstimatedExecutionTime { get; init; }
    public bool TrackDateOnly { get; init; }
    public bool IsPriority { get; init; }
    public IDictionary<string, string?> Userfields { get; init; }
}