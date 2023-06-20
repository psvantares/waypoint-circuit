using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace WaypointCircuit.Editor
{
    [CustomPropertyDrawer(typeof(Core.WaypointCircuit.WaypointList))]
    public class WaypointListDrawer : PropertyDrawer
    {
        private const float LINE_HEIGHT = 18;
        private const float SPACING = 4;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var x = position.x;
            var y = position.y;
            var inspectorWidth = position.width;

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var items = property.FindPropertyRelative("Items");
            var titles = new[] { "Transform", "", "", "" };
            var props = new[] { "transform", "^", "v", "-" };
            var widths = new[] { .7f, .1f, .1f, .1f };
            const float lineHeight = 18.0f;
            var changedLength = false;

            if (items.arraySize > 0)
            {
                for (var i = 0; i < items.arraySize; i++)
                {
                    var item = items.GetArrayElementAtIndex(i);
                    var rowX = x;

                    for (var n = 0; n < props.Length; ++n)
                    {
                        var w = widths[n] * inspectorWidth;
                        var rect = new Rect(rowX, y, w, lineHeight);

                        rowX += w;

                        if (i == -1)
                        {
                            EditorGUI.LabelField(rect, titles[n]);
                        }
                        else
                        {
                            if (n == 0)
                            {
                                EditorGUI.ObjectField(rect, item.objectReferenceValue, typeof(Transform), true);
                            }
                            else
                            {
                                if (GUI.Button(rect, props[n]))
                                {
                                    switch (props[n])
                                    {
                                        case "-":
                                            items.DeleteArrayElementAtIndex(i);
                                            items.DeleteArrayElementAtIndex(i);
                                            changedLength = true;
                                            break;
                                        case "v":
                                            if (i > 0)
                                            {
                                                items.MoveArrayElement(i, i + 1);
                                            }

                                            break;
                                        case "^":
                                            if (i < items.arraySize - 1)
                                            {
                                                items.MoveArrayElement(i, i - 1);
                                            }

                                            break;
                                    }
                                }
                            }
                        }
                    }

                    y += lineHeight + SPACING;
                    if (changedLength)
                    {
                        break;
                    }
                }
            }
            else
            {
                var addButtonRect = new Rect((x + position.width) - widths[^1] * inspectorWidth, y, widths[^1] * inspectorWidth, lineHeight);
                if (GUI.Button(addButtonRect, "+"))
                {
                    items.InsertArrayElementAtIndex(items.arraySize);
                }

                y += lineHeight + SPACING;
            }

            var addAllButtonRect = new Rect(x, y, inspectorWidth, lineHeight);
            if (GUI.Button(addAllButtonRect, "Assign using all child objects"))
            {
                var circuit = property.FindPropertyRelative("Circuit").objectReferenceValue as Core.WaypointCircuit;
                var children = new Transform[circuit.transform.childCount];
                var n = 0;

                foreach (Transform child in circuit.transform)
                {
                    children[n++] = child;
                }

                Array.Sort(children, new TransformNameComparer());
                circuit.WaypointListData.Items = new Transform[children.Length];
                for (n = 0; n < children.Length; ++n)
                {
                    circuit.WaypointListData.Items[n] = children[n];
                }
            }

            y += lineHeight + SPACING;

            var renameButtonRect = new Rect(x, y, inspectorWidth, lineHeight);
            if (GUI.Button(renameButtonRect, "Auto Rename numerically from this order"))
            {
                var circuit = property.FindPropertyRelative("Circuit").objectReferenceValue as Core.WaypointCircuit;
                var n = 0;

                foreach (var child in circuit.WaypointListData.Items)
                {
                    child.name = "Waypoint " + (n++).ToString("000");
                }
            }

            y += lineHeight + SPACING;

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var items = property.FindPropertyRelative("Items");
            const float lineAndSpace = LINE_HEIGHT + SPACING;
            return 40 + (items.arraySize * lineAndSpace) + lineAndSpace;
        }

        private class TransformNameComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                return string.Compare(((Transform)x)?.name, ((Transform)y)?.name, StringComparison.Ordinal);
            }
        }
    }
}