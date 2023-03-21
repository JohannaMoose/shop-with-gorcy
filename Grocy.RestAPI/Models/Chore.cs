namespace Grocy.RestAPI.Models;

public record Chore : Entity
{
    public Chore(){}

    public string Name { get; init; }
    public string Description { get; init; }
    public string PeriodType { get; init; }
    public int PeriodDays { get; init; }
    public int TrackDateOnly { get; init; }
    public string PeriodConfig { get; init; }
    public string AssignmentType { get; init; }
    public string AssignmentConfig { get; init; }
    public string StartDate { get; init; }
    public string RescheduledDate { get; init; }
    public int PeriodInterval { get; init; }
    public IDictionary<string, string?> Userfields { get; init; }

    public DateTime NextDueDate
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(RescheduledDate))
                return DateTime.Parse(RescheduledDate);
            else
            {
                var nextDueDate = DateTime.Parse(StartDate);
                while (nextDueDate < DateTime.Now)
                {
                    if (PeriodType == "days")
                        nextDueDate = nextDueDate.AddDays(PeriodInterval);
                    else if (PeriodType == "daily")
                    {
                        nextDueDate = nextDueDate.AddDays(PeriodInterval);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }

                return nextDueDate; 
            }
        }
    }
}