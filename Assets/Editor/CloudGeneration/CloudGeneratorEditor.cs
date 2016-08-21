using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(CloudGenerator))]
public class CloudGeneratorEditor : Editor
{

    public override void OnInspectorGUI()
    {
        CloudGenerator cloudGenerator = (CloudGenerator)target;

        if (DrawDefaultInspector() && cloudGenerator.autoUpdate)
        {
            cloudGenerator.DrawMapInEditor();
        }

        if (GUILayout.Button("GenerateClouds"))
        {
            cloudGenerator.DrawMapInEditor();
        }
    }
}
