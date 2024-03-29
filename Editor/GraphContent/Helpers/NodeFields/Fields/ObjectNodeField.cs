﻿using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace FM.Editor.Systems.DialogueNodes
{
    /// <summary>
    /// Node field used to link an object
    /// </summary>
    /// <typeparam name="T">Type of object to use</typeparam>
    public class ObjectNodeField<T> : GenericNodeField<T> where T : UnityEngine.Object
    {
        /* ==========================
         * > Private Fields
         * -------------------------- */

        private ObjectField _objectField;


        /* ==========================
         * > Constructors
         * -------------------------- */

        public ObjectNodeField(string labelText) : base(labelText) { }


        /* ==========================
         * > Methods
         * -------------------------- */

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override T GetValue()
        {
            return (T)_objectField.value;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override void SetValue(T fieldValue)
        {
            base.SetValue(fieldValue);
            _objectField.value = fieldValue;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override VisualElement CreateNodeField()
        {
            _objectField = new ObjectField();
            _objectField.objectType = typeof(T);
            return _objectField;
        }
    }
}