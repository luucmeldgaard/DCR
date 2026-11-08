using DCR.Core.Components;

namespace DCR.Core
{
    public class RuleEngine
    {
        // Also called 'markings' in DCR-langauge
        private Statuses statuses;

        // All the rules in the graph seperated into ingoing and outging rules. 
        private Dictionary<Guid, List<Rule>> outgoingRules;
        private Dictionary<Guid, List<Rule>> incomingRules;

        public RuleEngine(Statuses statuses, HashSet<Rule> rules)
        {
            this.statuses = statuses;

            this.outgoingRules = new Dictionary<Guid, List<Rule>>();
            this.incomingRules = new Dictionary<Guid, List<Rule>>();

            foreach (Rule rule in rules)
            {
                if (!outgoingRules.ContainsKey(rule.Source.Id))
                {
                    outgoingRules[rule.Source.Id] = [];
                }
                if (!incomingRules.ContainsKey(rule.Target.Id))
                {
                    incomingRules[rule.Target.Id] = [];
                }
                incomingRules[rule.Target.Id].Add(rule);
                outgoingRules[rule.Source.Id].Add(rule);
            }

        }

        public List<RuleViolation> CheckForRuleViolations()
        {
            List<RuleViolation> ruleViolations = [];
            foreach (List<Rule> list in this.incomingRules.Values)
            {
                foreach (Rule rule in list)
                {
                    // If the rule is a response, is included in the graph, and the deadline has been passed, the graph has a violation
                    if (rule.Type == RuleType.RESPONSE
                        && statuses.Included.Contains(rule.Target.Id)
                        && statuses.Pending.Contains(rule.Target.Id)
                        && rule.TimeLeft() < TimeSpan.Zero)
                    {
                        ruleViolations.Add(new RuleViolation(rule,
                            $"{rule.Target.Name} should have been executed before '{rule.Deadline}', but is still pending at '{DateTime.Now}'."));
                    }
                }
            }

            return ruleViolations;
        }

        // Executes the outgoing rules, and handles the effects of the incoming rules
        public void Execute(Guid activityId)
        {
            if (!statuses.Executed.Contains(activityId))
            {
                statuses.Executed.Add(activityId);
            }

            // Aktiviteten er udført, så den skylder ikke længere noget
            statuses.Pending.Remove(activityId);

            if (incomingRules.TryGetValue(activityId, out var incoming))
            {
                foreach (Rule rule in incoming)
                {
                    if (rule.Type == RuleType.RESPONSE)
                    {
                        rule.Deadline = null;
                    }
                }
            }

            if (outgoingRules.TryGetValue(activityId, out var outgoing))
            {
                foreach (Rule rule in outgoing)
                {
                    ExecuteEffect(rule);
                }
            }
        }

        // First type of rules, that revovles around constraints
        private bool ConstraintsCheck(Rule rule)
        {
            switch (rule.Type)
            {
                case RuleType.CONDITION:
                    return Condition(rule);
                case RuleType.MILESTONE:
                    return Milestone(rule);
                default:
                    break;
            }
            return true;
        }

        // second type of rules, that revolves around effects on the graph. 
        private void ExecuteEffect(Rule rule)
        {
            switch (rule.Type)
            {
                case RuleType.RESPONSE:
                    Response(rule);
                    break;
                case RuleType.INCLUDE:
                    Inclusion(rule);
                    break;
                case RuleType.EXCLUDE:
                    Exclusion(rule);
                    break;
                default:
                    break;
            }
        }

        // Checks whether or not an action is legal
        public bool IsPossibleAction(Guid activityId)
        {

            if (!statuses.Included.Contains(activityId)) return false;
            if (!incomingRules.TryGetValue(activityId, out var incoming)) return true;

            foreach (Rule rule in incoming)
            {
                if (!ConstraintsCheck(rule)) return false;
            }
            return true;
        }

        // Checks if there is any rules that has a pending response in the graph. 
        public bool HasOverdue()
        {
            foreach (List<Rule> list in incomingRules.Values)
            {
                foreach (Rule rule in list)
                {
                    if (rule.Type == RuleType.RESPONSE
                        && statuses.Included.Contains(rule.Target.Id)
                        && statuses.Pending.Contains(rule.Target.Id)
                        && rule.TimeLeft() < TimeSpan.Zero)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool Condition(Rule rule)
        {
            if (rule.Type != RuleType.CONDITION) throw new Exception("Rule type must be CONDITION");
            if (!statuses.Included.Contains(rule.Source.Id)) return true;
            if (!statuses.Executed.Contains(rule.Source.Id)) return false;

            TimeSpan? left = rule.TimeLeft();
            return left is null || left <= TimeSpan.Zero;
        }

        private bool Milestone(Rule rule)
        {
            if (rule.Type != RuleType.MILESTONE) throw new Exception("Rule type must be MILESTONE");
            if (statuses.Included.Contains(rule.Source.Id) && statuses.Pending.Contains(rule.Source.Id)) return false;
            return true;
        }

        private void Response(Rule rule)
        {
            if (rule.Type != RuleType.RESPONSE) throw new Exception("Rule type must be RESPONSE");
            if (statuses.Executed.Contains(rule.Source.Id))
            {
                statuses.Pending.Add(rule.Target.Id);
                if (rule.Duration is not null)
                {
                    rule.Deadline = DateTime.Now + rule.Duration.Value;
                }
            }
        }

        private void Inclusion(Rule rule)
        {
            if (rule.Type != RuleType.INCLUDE) throw new Exception("Rule type must be INCLUDE");
            statuses.Included.Add(rule.Target.Id);
        }

        private void Exclusion(Rule rule)
        {
            if (rule.Type != RuleType.EXCLUDE) throw new Exception("Rule type must be EXCLUDE");
            statuses.Included.Remove(rule.Target.Id);
        }
    }
}
