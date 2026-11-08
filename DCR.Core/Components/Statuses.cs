
namespace DCR.Core.Components
{
    public sealed record Statuses(
        HashSet<Guid> Executed,
        HashSet<Guid> Included,
        HashSet<Guid> Pending
    );
}
