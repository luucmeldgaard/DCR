
namespace DCR.Core.Components
{
    public sealed record Activity(
        Guid Id,
        string Name,
        string? Role, 
        string? Description
    );
}
