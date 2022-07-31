using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

[EditorTool("Rotate Pivot", typeof(Transform))]
[CanEditMultipleObjects]
class RotatePivotTool : EditorTool
{
    GUIContent _toolbarIcon;
    public override GUIContent toolbarIcon => _toolbarIcon;
    protected string toolbarIconResourceName => "RotatePivotIcon";

    void OnEnable()
    {
        if (_toolbarIcon == null)
            _toolbarIcon = new GUIContent(Resources.Load<Texture2D>(toolbarIconResourceName), "Rotate Pivot Tool");
    }

    static int lastFrameCount;
    static Transform handleAnchor;
    static Vector3 lastHandleAnchorPositiom;
    static Quaternion lastHandleAnchorRotation;
    static Dictionary<Transform, Vector3> lastChildPositions = new Dictionary<Transform, Vector3>();
    static Dictionary<Transform, Quaternion> lastChildRotations = new Dictionary<Transform, Quaternion>();

    public override void OnToolGUI(EditorWindow window)
    {
        if (!(window is SceneView sceneView))
            return;

        if (Time.frameCount > lastFrameCount + 1)
        {
            // The tool was probably deselected for a while 
            Debug.Log("Lost frame " + Time.frameCount + ", " + lastFrameCount);
            handleAnchor = null;
        }
        lastFrameCount = Time.frameCount;

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
                        Undo.RecordObject(child, "Rotate Pivot");
                        child.position = lastPos;
                    }
                    if (lastChildRotations.TryGetValue(child, out Quaternion lastRot) && lastRot != child.rotation)
                    {
                        Undo.RecordObject(child, "Rotate Pivot");
                        child.rotation = lastRot;
                    }
                }
            }
        }

        Quaternion oldRot = handleAnchor.rotation;
        EditorGUI.BeginChangeCheck();
        Vector3 handlePos = handleAnchor.position;
        Quaternion newRot = Handles.RotationHandle(oldRot, handlePos);
        if (EditorGUI.EndChangeCheck())
        {
            HashSet<Transform> didRotate = new HashSet<Transform>();
            foreach (Transform tf in tfs)
            {
                ApplyRotation(tf, isSelected, didRotate, oldRot, newRot, handlePos);
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

    void ApplyRotation(Transform current, HashSet<Transform> selected, HashSet<Transform> didRotate, Quaternion fromRot, Quaternion toRot, Vector3 origin)
    {
        if (current == null || didRotate.Contains(current))
            return;

        // make sure our ancestors are updated first
        ApplyRotation(current.parent, selected, didRotate, fromRot, toRot, origin);

        if (selected.Contains(current))
        {
            // if our parent is also selected, we don't need to do anything
            if (!selected.Contains(current.parent))
            {
                // if our parent is not selected, it has not moved - so we're in our original position 
                // and we can just apply the rotation
                // (either it hasn't moved because there was no reason to, or else because one of its ancestors is selected
                // and was moved, but the child of that ancestor has already been moved back to compensate.)
                Undo.RecordObject(current, "Rotate Pivot");

                List<Vector3> childPositions = new List<Vector3>();
                List<Quaternion> childRotations = new List<Quaternion>();
                for (int childIdx = 0; childIdx < current.childCount; ++childIdx)
                {
                    Transform child = current.GetChild(childIdx);
                    childPositions.Add(child.position);
                    childRotations.Add(child.rotation);
                }
                
                Vector3 oldOffset = current.position - origin;
                Vector3 newOffset = toRot * Quaternion.Inverse(fromRot) * oldOffset;
                current.position = origin + newOffset;
                current.rotation = toRot * Quaternion.Inverse(fromRot) * current.rotation;

                for (int childIdx = 0; childIdx < current.childCount; ++childIdx)
                {
                    Transform child = current.GetChild(childIdx);
                    if (!selected.Contains(child))
                    {
                        Undo.RecordObject(child, "Rotate Pivot");
                        child.position = childPositions[childIdx];
                        child.rotation = childRotations[childIdx];
                    }
                }
            }
        }

        didRotate.Add(current);
    }
}
