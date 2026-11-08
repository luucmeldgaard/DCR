using DCR.Core.Components;
using Activity = DCR.Core.Components.Activity;
using Rule = DCR.Core.Components.Rule;

namespace DCR.Core
{
    public class DCR_Engine
    {

        private Statuses statuses { get; }
        public readonly Dictionary<Guid, Activity> activities;
        private readonly HashSet<Rule> rules;
        public RuleEngine ruleEngine;
        public readonly string name;
        public Guid Id;
        public TreeNode node;
        public event Action<Activity>? ActivityExecuted;


        public DCR_Engine(string? name = null)
        {
            this.statuses = new Statuses(
                new HashSet<Guid>(),
                new HashSet<Guid>(),
                new HashSet<Guid>()
            );

            this.activities = new Dictionary<Guid, Activity>();
            this.rules = new HashSet<Rule>();
            this.Id = Guid.NewGuid();
            this.node = new TreeNode(this);
            if (name is null)
            {
                this.name = Guid.NewGuid().ToString().Substring(0, 4);
            }
            else
            {
                this.name = name;
            }
        }

        public void Start()
        {
            if (!CheckIntitalGraphCorrectness())
                throw new InvalidOperationException("Graph is not correct");

            this.ruleEngine = new RuleEngine(this.statuses, this.rules);
        }

        public bool IsPending(Guid id) => statuses.Pending.Contains(id);
        public bool IsIncluded(Guid id) => statuses.Included.Contains(id);
        public bool IsExecuted(Guid id) => statuses.Executed.Contains(id);

        public List<Activity> GetPossibleActivities()
        {
            return statuses.Included
                .Where(id => ruleEngine.IsPossibleAction(id))
                .Select(id => activities[id])
                .ToList();
        }

        public List<Activity> GetTimeBlockedConditions()
        {
            return ruleEngine.GetTimeBlockedConditions();
        }

        public ActivityResult Execute(Activity activity)
        {
            // Make sure, that the acitivity is an option in the current state of the DCR-graph
            if (!ruleEngine.IsPossibleAction(activity.Id))
            {
                return new ActivityResult(false, "Activity is not enabled");
            }

            // It should not be ppossible to execute any activity in a DCR-graph, where there is a rule violation. 
            if (ruleEngine.HasOverdue() || node.HasOverdueDescendant())
            {
                return new ActivityResult(false, "Overdue\nThere are overdue activities in this graph or its subprocesses. \nAccept them with 'a' first from the affected graph.");
            }

            // Execute the activity's function refference. 
            ActivityResult result = activity.Execute(this);
            if (result.Success)
            {
                ExecuteGraph(activity);
            }
            return result;
        }

        /* A graph is in an accepting state if it has no pending activites, 
         * and its decendants are also in an accepting state. Furthermore, 
         * the graph nor its decendants may have an active violation. 
         * Although, this is not checked here, since the effects of any violation
         * means, that the graph and its efftected children may not execute any
         * actions while it is active. In this DCR-Engine, the only violation psooible
         * is having a pending activity with a passed deadline. This means, 
         * that if there is a violation, it will always have something pending, 
         * and will therefore always result in a non-accepting state. 
        */
        public bool IsAcceptingState()
        {
            bool pending = statuses.Pending.Any(id => statuses.Included.Contains(id));

            bool childrenNotAccepting = this.node.HasNonAcceptingDescendant();

            return !pending && !childrenNotAccepting;
        }

        public bool HasViolations()
        {
            return ruleEngine.CheckForRuleViolations().Count > 0;
        }

        // Removes the deadline for the currently violated rules. 
        public void ViolationOverride()
        {
            List<RuleViolation> ruleViolations = ruleEngine.CheckForRuleViolations();
            foreach (Rule rule in ruleViolations.Select(rv => rv.rule))
            {
                rule.Deadline = null;
            }
        }

        // Executes an activity in the DCR-graph and tells any observers that it was executed using the activity's Guid
        private void ExecuteGraph(Activity activity)
        {
            ruleEngine.Execute(activity.Id);
            ActivityExecuted?.Invoke(activity);
        }

        // must be run before initialization
        private bool CheckIntitalGraphCorrectness()
        {
            // All activity refferences used in the rules HashSet must also be present in the activities HashSet
            foreach (Rule rule in rules)
            {
                if (!activities.ContainsKey(rule.Source.Id) ||
                    !activities.ContainsKey(rule.Target.Id))
                {
                    return false;
                }


                // only one rule from source A to target B can exclude / include
                // target B. Both rules cannot be true at the same time. 
                if (rules.Where(inc => inc.Type == RuleType.INCLUDE)
                     .Any(inc => rules.Any(exc =>
                     exc.Type == RuleType.EXCLUDE &&
                     exc.Source == inc.Source &&
                     exc.Target == inc.Target
                     ))) return false;
            }

            // All activity refferences used in markings must also be present in the activities HashSet
            foreach (Guid id in statuses.Executed.Union(statuses.Included).Union(statuses.Pending)) {
                if (!activities.ContainsKey(id)) return false;
            }

            return true;
        }

        public static string FormatTimeLeft(DCR_Engine graph, Activity activity)
        {
            TimeSpan? left = graph.rules
                .Where(rule => (rule.Type == RuleType.RESPONSE || rule.Type == RuleType.CONDITION) && rule.Target.Id == activity.Id)
                .Select(rule => rule.TimeLeft())
                .Min();

            if (left is null) return "";

            return left.Value < TimeSpan.Zero
                ? $"  [Overdue by {left.Value.Duration():mm\\:ss}]"
                : $"  [{left.Value:mm\\:ss} left]";
        }

        // Add an activity to the graph
        public Activity AddActivity(string name, Func<DCR_Engine, Guid, ActivityResult> action, string? role=null, string? descripton = null, bool executed = false, bool included = true, bool pending = false)
        {
            Activity activity = new Activity(Guid.NewGuid(), name, role, descripton, action);
            activities[activity.Id] = activity;
            if (executed) statuses.Executed.Add(activity.Id);
            if (included) statuses.Included.Add(activity.Id);
            if (pending) statuses.Pending.Add(activity.Id);
            return activity;
        }

        // Add a rule to the graph. The rule MUST have a source and a target activity. 
        // Note: When assembiling a graph, it is easier to make all the activities first and only then include the rules. 
        public void AddRule(Activity source, Activity target, RuleType type, TimeSpan? duration = null)
        {
            rules.Add(new Rule(Guid.NewGuid(), source, target, type, duration));
        }
    }
}
