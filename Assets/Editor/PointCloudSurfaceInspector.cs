using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(PointCloudSurfaceClip))]
public class PointCloudSurfaceClipInspector : Editor
{
    public override void OnInspectorGUI()
    {
        PointCloudSurfaceClip clip = (PointCloudSurfaceClip)target;

        EditorGUILayout.LabelField("Clip Directory", clip.clipDirectory);
        if( EditorGUILayout.DropdownButton(new GUIContent("ChooseDirectory"), FocusType.Keyboard) )
        {
            string dataPath = Application.dataPath;
            dataPath = dataPath.Replace("/Assets", "");

            string path = EditorUtility.OpenFolderPanel("Choose Directory With Point Cloud Clips", dataPath + clip.clipDirectory, "");

            clip.clipDirectory = path;
 
            if (path.StartsWith(dataPath))
            {
                clip.clipDirectory = clip.clipDirectory.Substring(dataPath.Length);
            }

            clip.numberOfFrames = 0;

            string[] files = Directory.GetFiles(path);

            foreach (string file in files)
                if (file.EndsWith(".bnzs"))
                    clip.numberOfFrames++;
        }
        EditorGUILayout.LabelField("Number Of Frames", clip.numberOfFrames.ToString());
        clip.startFrame = EditorGUILayout.IntField("Start Frame", clip.startFrame);
        clip.endFrame = EditorGUILayout.IntField("End Frame", clip.endFrame);
        clip.loop = EditorGUILayout.Toggle("Loop", clip.loop);
        clip.maxVertices = EditorGUILayout.IntField("Max Vertices", clip.maxVertices);
        clip.maxIndices = EditorGUILayout.IntField("Max Indices", clip.maxIndices);
    }
}
