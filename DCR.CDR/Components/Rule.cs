
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
        RuleType Type
    );
}
