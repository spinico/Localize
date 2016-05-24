namespace Spinico.Localize
{
    using System.Collections.ObjectModel;
    using System.Windows.Data;

    internal class BindingNode
    {
        private Collection<BindingNode> _nodes = new Collection<BindingNode>();        

        public Collection<BindingNode> Nodes { get { return _nodes; } }
        
        public MultiBinding MultiBinding { get; private set; }
        
        public Binding Binding { get; private set; }

        public int Index { get; private set; }        

        public BindingNode(MultiBinding multiBinding)
            : this(null , - 1)
        {
            this.MultiBinding = multiBinding;            
        }

        public BindingNode(Binding binding, int index)
        {
            this.Binding = binding;
            this.Index = index;
        }

        public void Add(BindingNode node)
        {
            if (node != null)
            {
                _nodes.Add(node);
            }
        }
    }
}
