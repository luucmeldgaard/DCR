namespace DCR.Core
{
    // A tree of DCR-graphs
    // A node may have multiple children
    // A child may only have one parent (the other one went to get milk)
    public class TreeNode
    {
        public readonly DCR_Engine graph;
        private List<TreeNode> children = new();
        private TreeNode? parent;

        public TreeNode(DCR_Engine graph)
        {
            this.graph = graph;
        }

        public void AddChild(TreeNode node)
        {
            node.parent?.RemoveChild(node);
            node.parent = this;
            this.children.Add(node);
        }

        public void RemoveChild(TreeNode node)
        {
            if (this.children.Remove(node))
            {
                node.parent = null;
            }
        }

        public void RemoveAllChildren()
        {
            foreach (TreeNode child in children)
            {
                child.parent = null;
            }
            this.children = [];
        }

        public IReadOnlyList<TreeNode> GetChildren()
        {
            return this.children.ToList();
        }

        public TreeNode? GetParent()
        {
            return this.parent;
        }

        public DCR_Engine? FindGraph(Guid id)
        {
            if (GetId() == id) return graph;

            foreach (TreeNode child in children)
            {
                DCR_Engine? found = child.FindGraph(id);
                if (found != null) return found;
            }

            return null;
        }

        // Any children that are in a non-accepting state?
        public bool HasNonAcceptingDescendant()
        {
            foreach (TreeNode child in children)
            {
                if (!child.graph.IsAcceptingState()) return true;
            }
            return false;
        }

        // Any children that has a pending activity with an overdue deadline?
        public bool HasOverdueDescendant()
        {
            foreach (TreeNode child in children)
            {
                if (child.graph.ruleEngine.HasOverdue()) return true;
                if (child.HasOverdueDescendant()) return true;
            }
            return false;
        }

        public Guid GetId()
        {
            return this.graph.Id;
        }
    }
}