﻿using Daniell.Runtime.Systems.DialogueNodes;
using System;

namespace Daniell.Editor.Systems.DialogueNodes
{
    /// <summary>
    /// Node used as a starting point of every dialogue graph
    /// </summary>
    [NodeName("Start")]
    [NodeColor(60, 130, 60)]
    [NodeWidth(3)]
    [SingleInstanceNode]
    public class StartNode : BaseNode
    {
        /* ==========================
         * > Properties
         * -------------------------- */

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override Type RuntimeNodeType => typeof(StartRuntimeNode);


        /* ==========================
         * > Constructors
         * -------------------------- */

        public StartNode()
        {
            _nodePortHandler.AddOutputPort(StartRuntimeNode.NEXT_NODE_PORT);
        }


        /* ==========================
         * > Methods
         * -------------------------- */

        #region Node Properties override

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override bool IsMovable()
        {
            return false;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override bool IsSelectable()
        {
            return false;
        }

        #endregion
    }
}