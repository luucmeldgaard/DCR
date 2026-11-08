
namespace DCR.Core.Components
{
    public enum RuleType
    {
        CONDITION,
        RESPONSE,
        MILESTONE,
        INCLUDE,
        EXCLUDE
    }

    public sealed record Rule(
        Guid Id,
        Activity Source,
        Activity Target,
        RuleType Type,
        TimeSpan? Duration = null
    )
    {
        public DateTime? Deadline { get; set; }

        // if the rule has a deadline (condition / response), it may return the remaining time left
        // before the time runs out. In case the deadline has passed, it will return a negative TimeSpan. <s
        public TimeSpan? TimeLeft()
        {
            if (Deadline is null) return null;
            return Deadline.Value - DateTime.Now;
        }
    }


    // For isntance used by timed responses, where the time may run out,
    // resulting in the graph state being non-accepting. 
    public sealed record RuleViolation(
        Rule rule,
        string Violation
    );
}
