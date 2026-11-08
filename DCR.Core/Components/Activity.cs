
namespace DCR.Core.Components
{
    public sealed record Activity
    {
        public Guid Id { get; }
        public string Name { get; }
        public string? Role { get; }
        public string? Description { get; }

        /* An activity holds a refference to a function that may be executed
         * when the activity is executed in its DCR_Engine.
         * 
         * In:  (1) The Engine that executes the activity
         *      (2) The activity's unique identifier (Guid)
         * Out: 
         *      The returned result (ActivityResult)
        */
        private readonly Func<DCR_Engine, Guid, ActivityResult> FunctionCall;

        public Activity(Guid id, string name, string? role, string? description, Func<DCR_Engine, Guid, ActivityResult> functionCall)
        {
            Id = id;
            Name = name;
            Role = role;
            Description = description;
            this.FunctionCall = functionCall;
        }

        // Used to execute the function and return an ActivityResult to the caller
        public ActivityResult Execute(DCR_Engine graph)
        {
            return this.FunctionCall(graph, this.Id);
        }

    }

    // If a functionCall was successful, it should return Success=True and Error=null
    public sealed record ActivityResult(
        bool Success,
        string? Error = null
    );
}
