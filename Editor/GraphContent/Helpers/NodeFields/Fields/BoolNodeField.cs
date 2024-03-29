﻿using UnityEngine.UIElements;

namespace FM.Editor.Systems.DialogueNodes
{
    /// <summary>
    /// Node field used to enter a boolean value
    /// </summary>
    public class BoolNodeField : BaseTypeNodeField<Toggle, bool>
    {
        /* ==========================
         * > Constructors
         * -------------------------- */

        public BoolNodeField(string labelText) : base(labelText) { }
    }
}