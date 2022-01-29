﻿using Daniell.Runtime.Helpers.Reflection;
using Daniell.Runtime.Systems.DialogueNodes;
using Daniell.Runtime.Systems.SimpleSave;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Daniell.Editor.DialogueNodes
{
    /// <summary>
    /// Editor window used to edit dialogue files
    /// </summary>
    public class DialogueGraphWindow : EditorWindow
    {
        /* ==========================
         * > Private Fields
         * -------------------------- */

        /// <summary>
        /// List of all the nodes currently in the graph
        /// </summary>
        public IEnumerable<BaseNode> Nodes => _graphView.nodes.ToList().Cast<BaseNode>();


        /* ==========================
         * > Private Fields
         * -------------------------- */

        private DialogueGraphView _graphView;
        private DialogueGraphToolbar _toolbar;
        private DialogueFile _dialogueFile;


        /* ==========================
         * > Methods
         * -------------------------- */

        [MenuItem("Daniell/Dialogue Graph")]
        public static void Open()
        {
            var window = GetWindow<DialogueGraphWindow>();
            window.titleContent = new GUIContent("Dialogue Graph");
        }

        #region Enable / Disable

        private void OnEnable()
        {
            // Create the toolbar
            CreateToolbar();
        }

        private void OnDisable()
        {
            // Remove the toolbar
            RemoveToolbar();

            // Remove the graph view
            RemoveGraphView();
        }

        #endregion

        #region Toolbar

        /// <summary>
        /// Create the toolbar
        /// </summary>
        private void CreateToolbar()
        {
            if (_toolbar == null)
            {
                _toolbar = new DialogueGraphToolbar();
                _toolbar.OnDialogueFileLoaded += OnDialogueFileValueChanged;
                _toolbar.OnSaveButtonPressed += Save;

                rootVisualElement.Add(_toolbar);
            }
        }

        /// <summary>
        /// Remove the toolbar
        /// </summary>
        private void RemoveToolbar()
        {
            if (_toolbar != null)
            {
                _toolbar.OnDialogueFileLoaded -= OnDialogueFileValueChanged;
                _toolbar.OnSaveButtonPressed -= Save;

                rootVisualElement.Remove(_toolbar);
            }
        }

        #endregion

        #region GraphView

        /// <summary>
        /// Create the GraphView
        /// </summary>
        private void CreateGraphView()
        {
            if (_graphView == null)
            {
                // Create the graph view instance
                _graphView = new DialogueGraphView("Dialogue Graph", this, _dialogueFile);

                // Stretch view to container
                _graphView.StretchToParentSize();

                // Add to the window
                rootVisualElement.Add(_graphView);

                // Send the view to the back so that the toolbar stays on top
                _graphView.SendToBack();
            }
        }

        /// <summary>
        /// Remove the GraphView
        /// </summary>
        private void RemoveGraphView()
        {
            if (_graphView != null)
            {
                // Remove from the window
                rootVisualElement.Remove(_graphView);

                // Reset reference
                _graphView = null;
            }
        }

        #endregion

        #region Event Callbacks

        private void OnDialogueFileValueChanged(DialogueFile dialogueFile)
        {
            // Cache value
            _dialogueFile = dialogueFile;

            if (dialogueFile != null)
            {
                // Create the graph
                CreateGraphView();

                Load();
            }
            else
            {
                // Remove the graph
                RemoveGraphView();
            }
        }

        #endregion

        #region Save & Load

        /// <summary>
        /// Save all the node and their data
        /// </summary>
        public void Save()
        {
            if(_dialogueFile == null)
            {
                return;
            }

            // List all the nodes in the graphview
            _dialogueFile.ClearData();

            foreach (BaseNode node in Nodes)
            {
                // Create a new save data bundle with node GUID
                SaveDataBundle nodeData = new SaveDataBundle(node.GUID);
                node.SaveNodeData(nodeData);

                // Save node connection infos in data bundle
                nodeData.Set("Connections", node.GetNodeConnections());

                // Save the data bundle as json in the dialogue file
                string bundleAsJson = JsonUtility.ToJson(nodeData, true);
                _dialogueFile.AddData(bundleAsJson);
            }

            if (_dialogueFile != null)
            {
                EditorUtility.SetDirty(_dialogueFile);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Load graph content from the dialogue file
        /// </summary>
        public void Load()
        {
            // Do not continue if there is no dialogue file
            if(_dialogueFile == null)
            {
                return;
            }

            List<NodeConnection> nodeConnections = new List<NodeConnection>();

            // Create nodes and load connections
            for (int i = 0; i < _dialogueFile.Count; i++)
            {
                var nodeData = JsonUtility.FromJson<SaveDataBundle>(_dialogueFile[i]);

                // Create the right type of node
                string methodName = nameof(DialogueGraphView.CreateNode);
                Type type = Type.GetType(nodeData.Get<string>("Type"));
                var node = ReflectionHelpers.CallGenericMethod<DialogueGraphView>(methodName, _graphView, null, type);

                var baseNode = (BaseNode)node;
                baseNode.LoadNodeData(nodeData);

                // Load connections
                nodeConnections.AddRange(nodeData.Get<NodeConnection[]>("Connections"));
            }

            // Connect nodes
            for (int i = 0; i < nodeConnections.Count; i++)
            {
                var nodeConnection = nodeConnections[i];
                ConnectNodes(nodeConnection);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Connect two ports together
        /// </summary>
        /// <param name="nodeConnection">Node connection infos</param>
        private void ConnectNodes(NodeConnection nodeConnection)
        {
            GetNodeAndPortFromIdentifier(nodeConnection.ConnectionOrigin, out BaseNode _, out Port originPort);
            GetNodeAndPortFromIdentifier(nodeConnection.ConnectionTarget, out BaseNode _, out Port targetPort);

            // Do not execute if ports are connected
            if(ArePortsConnected(originPort, targetPort))
            {
                return;
            }

            // Create a new edge
            Edge edge = new Edge
            {
                input = originPort.direction == Direction.Input ? originPort : targetPort,
                output = targetPort.direction == Direction.Output ? targetPort : originPort
            };

            // Connect edge to ports
            originPort.Connect(edge);
            targetPort.Connect(edge);

            // Add edge to the graph view
            _graphView.Add(edge);
        }

        /// <summary>
        /// Are both ports connected to each other?
        /// </summary>
        /// <param name="origin">Origin port</param>
        /// <param name="target">Target port</param>
        /// <returns>True if ports are connected</returns>
        private bool ArePortsConnected(Port origin, Port target)
        {
            if (!origin.connected || !target.connected)
            {
                return false;
            }

            foreach (var connection in origin.connections)
            {
                if(connection.input == target || connection.output == target)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Outputs a node and a port from an identifier
        /// </summary>
        /// <param name="nodePortID">Node Port infos</param>
        /// <param name="node">Node with matching GUID</param>
        /// <param name="port">Port with matching ID</param>
        private void GetNodeAndPortFromIdentifier(NodePortIdentifier nodePortID, out BaseNode node, out Port port)
        {
            node = FindNodeByGUID(nodePortID.NodeGUID);
            port = node.GetPortByID(nodePortID.PortID);
        }

        /// <summary>
        /// Find a node by its GUID
        /// </summary>
        /// <param name="guid">GUID to look for</param>
        /// <returns>Node with matching GUID</returns>
        public BaseNode FindNodeByGUID(string guid)
        {
            foreach (BaseNode node in Nodes)
            {
                if (node.GUID == guid)
                {
                    return node;
                }
            }

            return null;
        }

        #endregion
    }
}