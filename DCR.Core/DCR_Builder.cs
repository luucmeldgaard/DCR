using DCR.Core.Components;

namespace DCR.Core
{
    public static class DCR_Builder
    {
        // MainController listens to these and control the currentNode (active DCR-graph)
        public static event Action<DCR_Engine>? OrderSpawned;               // (order) - for context switching
        public static event Action<DCR_Engine, DCR_Engine>? OrderFinished;  // (parent, order) - for context switching


        // Here you can assemle a tested DCR-graph and return it
        public static class Assemble
        {
            public static DCR_Engine Shop(string name = "SHOP")
            {
                return BuildShop(name);
            }

            public static DCR_Engine Order(DCR_Engine parent)
            {
                return BuildOrder(parent);
            }

            private static DCR_Engine BuildShop(string name)
            {
                DCR_Engine shop = new DCR_Engine(name);

                // Activities
                var openShop = shop.AddActivity("Open shop", Methods.TestMethod1, pending: true);
                var receiveOrder = shop.AddActivity("Receive order", Methods.SpawnHandleOrder, included: false);
                var closeShop = shop.AddActivity("Close shop", Methods.TestMethod1, included: false);

                // Open shop
                shop.AddRule(openShop, receiveOrder, RuleType.INCLUDE);
                shop.AddRule(openShop, closeShop, RuleType.INCLUDE);
                shop.AddRule(openShop, closeShop, RuleType.RESPONSE);
                shop.AddRule(openShop, openShop, RuleType.EXCLUDE);

                // Close shop
                shop.AddRule(closeShop, receiveOrder, RuleType.EXCLUDE);
                shop.AddRule(closeShop, closeShop, RuleType.EXCLUDE);
                shop.AddRule(closeShop, openShop, RuleType.INCLUDE);

                shop.Start();
                return shop;
            }

            private static DCR_Engine BuildOrder(DCR_Engine parent)
            {
                var order = new DCR_Engine("ORDER-" + Guid.NewGuid().ToString().Substring(0, 4));

                // Activities
                var pickFlowers = order.AddActivity("Pick flowers", Methods.TestMethod1, pending: true);
                var makeBouquet = order.AddActivity("Make bouquet", Methods.TestMethod1, included: true);
                var takePayment = order.AddActivity("Take payment", Methods.TestMethod1, included: true);
                var deliver = order.AddActivity("Deliver", Methods.TestMethod2, included: true);
                var cancel = order.AddActivity("Cancel order", Methods.TestMethod2, included: true);

                // Pick flowers
                order.AddRule(pickFlowers, makeBouquet, RuleType.CONDITION, duration: TimeSpan.FromSeconds(5));
                //order.AddRule(pickFlowers, makeBouquet, RuleType.RESPONSE);

                // Make bouquet
                order.AddRule(makeBouquet, deliver, RuleType.CONDITION);
                order.AddRule(makeBouquet, deliver, RuleType.RESPONSE, duration: TimeSpan.FromSeconds(5));

                // Take payment
                order.AddRule(takePayment, deliver, RuleType.CONDITION);

                // Deliver
                order.AddRule(deliver, cancel, RuleType.EXCLUDE);

                // Cancel order
                order.AddRule(cancel, pickFlowers, RuleType.EXCLUDE);
                order.AddRule(cancel, makeBouquet, RuleType.EXCLUDE);
                order.AddRule(cancel, deliver, RuleType.EXCLUDE);

                // When an order is finished (delivered / cancelled): Remove it from its parent and notify any observers
                order.ActivityExecuted += activity =>
                {
                    if ((activity.Id == deliver.Id || activity.Id == cancel.Id) &&
                        order.IsAcceptingState())
                    {
                        Console.WriteLine($"Order {order.name} was processed");
                        parent.node.RemoveChild(order.node);
                        OrderFinished?.Invoke(parent, order); // notifies any observers that the order finished. 
                    }
                };

                order.Start();
                parent.node.AddChild(order.node);
                return order;
            }
        }


        /* All the methods that may be used by the assembler class. 
         * These are meant to be executed by the activity holding a refference
         * to any of these methods. All methods must be Func. 
         * Method execution happens through the DCR_Engine in the Execute method.
         * 
         * If you want to test any of these methods seperately from the UI and the DCR-graph, 
         * it is recommended to execute these methods directly. 
        */
        public static class Methods
        {
            public static ActivityResult SpawnHandleOrder(DCR_Engine graph, Guid id)
            {
                Console.WriteLine("Spawning new HandleOrder subprocess");
                DCR_Engine order = Assemble.Order(graph);
                OrderSpawned?.Invoke(order); // notifies any observers, that a new order ha been assembled
                return new ActivityResult(true);
            }

            public static ActivityResult TestMethod1(DCR_Engine graph, Guid id)
            {
                Console.WriteLine("--- TEST 1 EXECUTED ---");
                return new ActivityResult(true);
            }

            public static ActivityResult TestMethod2(DCR_Engine graph, Guid id)
            {
                Console.WriteLine("");
                return new ActivityResult(true);
            }
        }
    }
}