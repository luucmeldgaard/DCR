using DCR.Core.Components;

namespace DCR.Core
{
    public class DCR_Engine
    {

        private Markings markings;
        private HashSet<Activity> activities;
        private HashSet<Rule> rules;

        public DCR_Engine()
        {
            markings = new Markings(
                new HashSet<Guid>(),
                new HashSet<Guid>(),
                new HashSet<Guid>()
            );

            activities = new HashSet<Activity>();
            rules = new HashSet<Rule>();
        }

        public void Start()
        {
            CheckIntitalGraphCorrectness();

        }

        // must be run before initialization
        private bool CheckIntitalGraphCorrectness()
        {
            // All activity refferences used in the rules HashSet must also be present in the activities HashSet
            foreach (Rule rule in rules)
            {
                if (!activities.Any(a => a.Id == rule.Source.Id) ||
                    !activities.Any(a => a.Id == rule.Target.Id))
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
            foreach (Guid id in markings.Executed.Union(markings.Included).Union(markings.Pending)) {
                if (!activities.Any(a => a.Id == id)) return false;
            }

            return true;
        }

        public Activity AddActivity(string name, string? role=null, string? descripton = null, bool executed = false, bool included = true, bool pending = false)
        {
            Activity activity = new Activity(Guid.NewGuid(), name, role, descripton);
            activities.Add(activity);
            if (executed) markings.Executed.Add(activity.Id);
            if (included) markings.Included.Add(activity.Id);
            if (pending) markings.Pending.Add(activity.Id);
            return activity;
        }

        public void AddRule(Activity source, Activity target, RuleType type)
        {
            rules.Add(new Rule(Guid.NewGuid(), source, target, type));
        }

    }
}
