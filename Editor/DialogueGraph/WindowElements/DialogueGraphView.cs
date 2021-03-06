using FM.Runtime.Helpers.Reflection;
using FM.Editor.Systems.DialogueNodes;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using FM.Runtime.Systems.DialogueNodes;
using System;

namespace FM.Editor.DialogueNodes
{
    /// <summary>
    /// Custom dialogue node GraphView element
    /// </summary>
    public class DialogueGraphView : GraphView
    {
        /* ==========================
         * > Private Fields
         * -------------------------- */

        private DialogueGraphSearchWindow _searchWindow;
        private readonly EditorWindow _parent;
        private readonly DialogueFile _dialogueFile;


        /* ==========================
         * > Constructors
         * -------------------------- */

        public DialogueGraphView(string name, EditorWindow parent, DialogueFile dialogueFile)
        {
            this.name = name;
            _dialogueFile = dialogueFile;
            _parent = parent;

            // Style the graph
            AddManipulators();
            SetupZoom(0.25f, 1.25f);
            ApplyGridBackground();
            SetupSearchWindow();

            // Create the start node
            CreateNode<StartNode>();
        }


        /* ==========================
         * > Methods
         * -------------------------- */

        #region Graph Settings

        /// <summary>
        /// Add all required manipulators
        /// </summary>
        private void AddManipulators()
        {
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
        }

        /// <summary>
        /// Apply a grid background
        /// </summary>
        private void ApplyGridBackground()
        {
            // Add a grid background
            GridBackground gridBackground = new GridBackground();
            Insert(0, gridBackground);
            gridBackground.StretchToParentSize();

            styleSheets.Add(Resources.Load<StyleSheet>("DialogGraph"));
        }

        /// <summary>
        /// Create a search window
        /// </summary>
        private void SetupSearchWindow()
        {
            _searchWindow = ScriptableObject.CreateInstance<DialogueGraphSearchWindow>();

            // Get the list of supported nodes
            var dialogueFileType = _dialogueFile.GetType();
            var supportedNodeTypeAttributes = ReflectionHelpers.GetAttributesForType<SupportedNodeTypeAttribute>(dialogueFileType);

            List<Type> supportedNodeTypes = new List<Type>();

            for (int i = 0; i < supportedNodeTypeAttributes.Length; i++)
            {
                SupportedNodeTypeAttribute attribute = supportedNodeTypeAttributes[i];

                var baseNodeTypes = (
                    from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                    from assemblyType in domainAssembly.GetTypes()
                    where assemblyType.IsSubclassOf(typeof(BaseNode)) && !assemblyType.IsAbstract
                    select assemblyType).ToArray();

                foreach (var baseNodeType in baseNodeTypes)
                {
                    var runtimeNodeTypeAttribute = ReflectionHelpers.GetAttributeForType<RuntimeNodeTypeAttribute>(baseNodeType);

                    if (runtimeNodeTypeAttribute.Type == attribute.Type)
                    {
                        supportedNodeTypes.Add(baseNodeType);
                        break;
                    }
                }
            }

            _searchWindow.Initialize(_parent, this, supportedNodeTypes);

            nodeCreationRequest = context =>
            {
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchWindow);
            };
        }

        #endregion

        #region Nodes

        /// <summary>
        /// Create a new node and add it to the graph
        /// </summary>
        /// <typeparam name="T">Node Type</typeparam>
        /// <returns>Node created</returns>
        public BaseNode CreateNode<T>() where T : BaseNode, new()
        {
            // Check if node can have multiple instances
            var singleInstanceAttribute = ReflectionHelpers.GetAttributeForType<SingleInstanceNodeAttribute>(typeof(T));

            // If this node can only have one instance
            if (singleInstanceAttribute != null)
            {
                // Check if a node is already loaded
                var nodes = this.nodes.ToList().Cast<BaseNode>();

                foreach (var n in nodes)
                {
                    if (n.GetType() == typeof(T))
                    {
                        return n;
                    }
                }
            }

            BaseNode node = new T();
            AddElement(node);
            return node;
        }

        #endregion

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatiblePorts = new List<Port>();

            ports.ForEach(port =>
            {
                if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
                {
                    compatiblePorts.Add(port);
                }
            });

            return compatiblePorts;
        }
    }
}