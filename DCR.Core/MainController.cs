using DCR.Core;
using DCR.Core.Components;
using Activity = DCR.Core.Components.Activity;

namespace DCR
{
    public class MainController
    {
        private readonly DCR_Engine shop;
        private readonly TreeNode root;

        // The active DCR-graph
        private TreeNode currentNode;
        private readonly bool debug = true;

        public static void Main()
        {
            new MainController();
        }

        public MainController()
        {
            SetEventHandlers();

            this.shop = DCR_Builder.Assemble.Shop();
            this.root = this.shop.node;
            this.currentNode = this.root;

            if (debug == true)
            {
                PromptUserForAction();
            }
        }

        private void SetEventHandlers()
        {
            // When a new order has been assembled,
            // the active DCR-graph should be switched to the order
            DCR_Builder.OrderSpawned += order =>
            {
                currentNode = order.node;
            };

            // When an order has finished, the active DCR-graph should be 
            // switched back to its parent. 
            DCR_Builder.OrderFinished += (parent, order) =>
            {
                if (currentNode == order.node)
                {
                    currentNode = parent.node;
                }
            };
        }

        public void PromptUserForAction()
        {
            while (true)
            {
                DCR_Engine graph = currentNode.graph;
                Console.WriteLine($"State: {(graph.IsAcceptingState() ? "Accepting" : "Not accepting")}");
                Console.WriteLine($"{(currentNode != root ? "'p' parent, " : "")}'u' update actions, 'a' accept violation, 'r' remove current graph (and decendants)");
                Console.WriteLine($"==============================");
                Console.WriteLine($"  Available actions ({this.currentNode.graph.name.ToUpper()})");
                Console.WriteLine("==============================");

                // Activities that can be executed
                int count = 1;
                Dictionary<int, Activity> choices = [];
                List<Activity> possibleActivities = graph.GetPossibleActivities();
                foreach (Activity activity in possibleActivities)
                {
                    choices[count] = activity;
                    bool isPending = graph.IsPending(activity.Id);
                    string pending = isPending ? "  [Pending]" : "";
                    string timeLeft = isPending ? DCR_Engine.FormatTimeLeft(graph, activity) : "";

                    Console.WriteLine($"  {count,2}. {activity.Name}{pending}{timeLeft}");
                    count += 1;
                }

                // Activities that are pending and included, but cannot be executed yet due to constraints
                List<Activity> blockedPendingActivities = graph.activities.Values
                    .Where(activity => graph.IsPending(activity.Id)
                                    && graph.IsIncluded(activity.Id)
                                    && !possibleActivities.Contains(activity))
                    .ToList();

                if (blockedPendingActivities.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Blocked pending activities:");
                    foreach (Activity activity in blockedPendingActivities)
                    {
                        Console.WriteLine($"   - {activity.Name}{DCR_Engine.FormatTimeLeft(graph, activity)}  (BLOCKED)");
                    }
                }

                List<Activity> blockedTimedConditions = graph.GetTimeBlockedConditions();
                if (blockedTimedConditions.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Blocked timed activities:");
                    foreach (Activity activity in blockedTimedConditions)
                    {
                        Console.WriteLine($"   - {activity.Name} [Available in {DCR_Engine.FormatTimeLeft(graph, activity)}]");
                    }
                }

                // Children - continued enumeratation of choices. 
                Dictionary<int, TreeNode> childChoices = [];
                if (currentNode.GetChildren().Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Child processes: ");
                    foreach (TreeNode child in currentNode.GetChildren())
                    {
                        childChoices[count] = child;
                        Console.WriteLine($"  {count,2}. {child.graph.name}{(child.graph.HasViolations() ? "  (Violation)" : "")}{((child.graph.IsAcceptingState() && !child.HasNonAcceptingDescendant()) ? "" : " (Has pending)")}");
                        count += 1;
                    }
                }

                Console.WriteLine("------------------------------");
                Console.Write("Choose an action: ");
                string? choice = Console.ReadLine();
                Console.Clear();

                if (choice != null)
                {
                    TreeNode? selectedChild = currentNode.GetChildren()
                        .FirstOrDefault(child => child.graph.name.Equals(choice, StringComparison.OrdinalIgnoreCase));

                    if (int.TryParse(choice, out int choiceInt) && choices.ContainsKey(choiceInt))
                    {
                        Activity activity = choices[choiceInt];

                        ActivityResult activityResult = graph.Execute(activity);
                        if (activityResult.Success)
                        {
                            Console.WriteLine($"[OK]    Executed: {activity.Name}");
                        }
                        else
                        {
                            Console.WriteLine($"[ERROR] Failed:   {activity.Name}");
                            Console.WriteLine($"        Reason:   {activityResult.Error}\n");
                        }
                    }
                    else if (int.TryParse(choice, out int childInt) && childChoices.ContainsKey(childInt))
                    {
                        currentNode = childChoices[childInt];
                    }
                    else if (choice.ToLower() == "p")
                    {
                        if (currentNode.GetParent() != null)
                        {
                            currentNode = currentNode.GetParent()!;
                        }
                    }
                    else if (choice.ToLower() == "u")
                    {
                        continue;
                    }
                    else if (choice.ToLower() == "a")
                    {
                        graph.ViolationOverride();
                    }
                    else if (choice.ToLower() == "r")
                    {
                        if (currentNode.GetParent() != null)
                        {
                            TreeNode temp = currentNode;
                            currentNode = currentNode.GetParent()!;
                            currentNode.RemoveChild(temp);
                        }
                        else
                        {
                            return;
                        }
                    }
                    else if (selectedChild != null)
                    {
                        currentNode = selectedChild;
                    }
                    else
                    {
                        Console.WriteLine("[!]     Please choose a valid action.");
                    }
                }
                else
                {
                    Console.WriteLine("[!]     No action was selected.");
                }

                Console.WriteLine();
            }
        }
    }
}
