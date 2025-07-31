using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static MoveablePlatform;

[CustomEditor(typeof(MoveablePlatform), true)]
public class MoveablePlatformPathEditor : Editor
{
    protected virtual void OnSceneGUI()
    {
        int next = new int();

        Handles.color = Color.yellow;
        MoveablePlatform platform = target as MoveablePlatform;

        // Check if points will be show or not.
        if (platform.showPoints)
        {
            for (int i = 0; i < platform.points.Count; i++)
            {
                Vector2 position = platform.points[i];

                next = i + 1;

                if (next == platform.points.Count)
                {
                    next = 0;

                    // If it's not ascending, don't connect dot between first and last point.
                    if (platform.GetPlatformType() != PlatformType.Ascending)
                    {
                        next = i;
                    }
                }

                position = Handles.FreeMoveHandle(position, Quaternion.identity, platform.pointSize, Vector3.one * platform.pointSize, Handles.SphereHandleCap);
                
                Handles.DrawLine(platform.points[i], platform.points[next], platform.lineThinkness);
                Handles.Label(platform.points[i], "Pos " + (i + 1).ToString());
                
                //Checks to see if there aren't any changes within the Editor.
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "MoveablePlatform");
                    platform.points[i] = position;
                }
            }
        }
    }
}