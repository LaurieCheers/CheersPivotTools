using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

[EditorTool("Move Pivot", typeof(Transform))]
[CanEditMultipleObjects]
class MovePivotTool : EditorTool
{
    GUIContent _toolbarIcon;
    public override GUIContent toolbarIcon => _toolbarIcon;
    protected string toolbarIconResourceName => "MovePivotIcon";

    void OnEnable()
    {
        if (_toolbarIcon == null)
            _toolbarIcon = new GUIContent(Resources.Load<Texture2D>(toolbarIconResourceName), "Move Pivot Tool");
    }

    public override void OnActivated()
    {
        handleAnchor = null;
    }

    static Transform handleAnchor;
    static Vector3 lastHandleAnchorPositiom;
    static Quaternion lastHandleAnchorRotation;
    static Dictionary<Transform, Vector3> lastChildPositions = new Dictionary<Transform, Vector3>();
    static Dictionary<Transform, Quaternion> lastChildRotations = new Dictionary<Transform, Quaternion>();

    public override void OnToolGUI(EditorWindow window)
    {
        if (!(window is SceneView sceneView))
            return;

        Transform[] tfs = targets.Select(obj => obj as Transform).ToArray();
        HashSet<Transform> isSelected = new HashSet<Transform>();
        foreach (var tf in tfs)
        {
            isSelected.Add(tf);
        }

        // try to keep the first selected object as our anchor position, unless it has been deselected
        if (handleAnchor == null || !tfs.Contains(handleAnchor))
        {
            handleAnchor = tfs[0];
        }
        else if (tfs.Length == 1)
        {
            // correct child positions when typing in inspector
            if (handleAnchor.position != lastHandleAnchorPositiom || handleAnchor.rotation != lastHandleAnchorRotation)
            {
                for (int Idx = 0; Idx < handleAnchor.childCount; ++Idx)
                {
                    Transform child = handleAnchor.GetChild(Idx);
                    if (lastChildPositions.TryGetValue(child, out Vector3 lastPos) && lastPos != child.position)
                    {
                        Undo.RecordObject(child, "Move Pivot");
                        child.position = lastPos;
                    }
                    if (lastChildRotations.TryGetValue(child, out Quaternion lastRot) && lastRot != child.rotation)
                    {
                        Undo.RecordObject(child, "Move Pivot");
                        child.rotation = lastRot;
                    }
                }
            }
        }
        Vector3 handlePosition = handleAnchor.position;
        Quaternion handleRotation = handleAnchor.rotation;

        EditorGUI.BeginChangeCheck();
        Vector3 newPos = Handles.PositionHandle(handlePosition, handleRotation);
        if (EditorGUI.EndChangeCheck())
        {
            Vector3 offset = newPos - handlePosition;
            foreach (Transform tf in tfs)
            {
                // if the parent is also selected to move, we don't need to actually move this one
                if (!isSelected.Contains(tf.parent))
                {
                    Undo.RecordObject(tf, "Move Pivot");
                    tf.position += offset;
                }
                for (int Idx = 0; Idx < tf.childCount; ++Idx)
                {
                    Transform child = tf.GetChild(Idx);
                    if (!isSelected.Contains(child))
                    {
                        Undo.RecordObject(child, "Move Pivot");
                        child.position -= offset;
                    }
                }
            }
        }

        if (tfs.Length == 1)
        {
            lastHandleAnchorPositiom = handleAnchor.position;
            lastHandleAnchorRotation = handleAnchor.rotation;
            lastChildPositions.Clear();
            lastChildRotations.Clear();
            for (int Idx = 0; Idx < handleAnchor.childCount; ++Idx)
            {
                Transform child = handleAnchor.GetChild(Idx);
                lastChildPositions.Add(child, child.position);
                lastChildRotations.Add(child, child.rotation);
            }
        }
    }
}
