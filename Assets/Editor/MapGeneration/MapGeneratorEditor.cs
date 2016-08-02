using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor :  Editor{

    public override void OnInspectorGUI()
    {
        MapGenerator mapGenerator = (MapGenerator) target;

        if (DrawDefaultInspector() && mapGenerator.autoUpdate)
        {
            mapGenerator.DrawMapInEditor();
        }

        if (GUILayout.Button("GenerateMap"))
        {
            mapGenerator.DrawMapInEditor();
        }
    }
}
