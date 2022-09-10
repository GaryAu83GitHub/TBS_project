using UnityEditor;
using UnityEngine;
using Assets.Scripts.Backends.HexGrid;

[CustomPropertyDrawer(typeof(HexCoordinates))]
public class HexCoordinatesDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        HexCoordinates coordinates = new HexCoordinates(
            property.FindPropertyRelative("x").intValue,
            property.FindPropertyRelative("z").intValue
            );

        EditorGUI.LabelField(position, label.text, coordinates.ToString());
    }
}
