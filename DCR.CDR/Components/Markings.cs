using System;
using System.Collections.Generic;
using System.Text;

namespace DCR.Core.Components
{
    public enum MarkingType
    {
        EXECUTED,
        INCLUDED,
        PENDING
    }

    public sealed record Markings(
        HashSet<Guid> Executed,
        HashSet<Guid> Included,
        HashSet<Guid> Pending
    );
}
