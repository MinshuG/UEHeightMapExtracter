using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HeightMapExtractor
// namespace BlenderUmap
{
    public class TreeNode<T> : IEnumerable<TreeNode<T>>
    {
        public T Data { get; set; }
        public TreeNode<T>? Parent { get; set; }
        public ICollection<TreeNode<T>> Children { get; set; }

        public Boolean IsRoot => Parent == null;

        public Boolean IsLeaf
        {
            get { return Children.Count == 0; }
        }

        public int Level
        {
            get
            {
                if (IsRoot) return 0;
                return Parent!.Level + 1;
            }
        }

        public TreeNode(T data)
        {
            this.Data = data;
            this.Children = new LinkedList<TreeNode<T>>();

            this.ElementsIndex = new LinkedList<TreeNode<T>>();
            this.ElementsIndex.Add(this);
        }

        public TreeNode<T> AddChild(T child)
        {
            TreeNode<T> childNode = new TreeNode<T>(child) { Parent = this };
            this.Children.Add(childNode);

            this.RegisterChildForSearch(childNode);

            return childNode;
        }

        public TreeNode<T> FindOrCreateChild(T child)
        {
            var childNode = FindTreeNodeInParentNodes(node => node.Data.Equals(child));
            if (childNode == null)
                childNode = AddChild(child);
            return childNode;
        }

        public override string ToString()
        {
            return (Data != null ? Data.ToString() : "[data null]") ?? throw new InvalidOperationException();
        }


        #region searching
        
        private ICollection<TreeNode<T>> ElementsIndex { get; set; }

        private void RegisterChildForSearch(TreeNode<T> node)
        {
            ElementsIndex.Add(node);
            if (Parent != null)
                Parent.RegisterChildForSearch(node);
        }

        public TreeNode<T>? FindTreeNodeInParentNodes(Func<TreeNode<T>, bool> predicate)
        {
            // return this.ElementsIndex.FirstOrDefault(predicate);
            var currentNode = this;
            while (currentNode != null)
            {
                if (predicate(currentNode))
                    return currentNode;
                currentNode = currentNode.Parent;
            }
            return null;
        }

        public TreeNode<T>? FindTreeNodeInParentNodes(T data)
        {
            return FindTreeNodeInParentNodes(node => node.Data.Equals(data));
        }
        #endregion


        #region iterating
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<TreeNode<T>> GetEnumerator()
        {
            yield return this;
            foreach (var directChild in this.Children)
            {
                foreach (var anyChild in directChild)
                    yield return anyChild;
            }
        }

        #endregion
    }
}