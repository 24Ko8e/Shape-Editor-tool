using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ShapeCreator))]
public class ShapeEditor : Editor
{
    ShapeCreator shapeCreator;
    SelectionInfo selectionInfo;
    bool needsRepaint;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        string helpMessage = "Left click to add points.\nShift-left click on point to delete.\nShift-left click on empty space to create new shape";
        EditorGUILayout.HelpBox(helpMessage, MessageType.Info);
        int shapeDeleteIndex = -1;

        shapeCreator.showShapesList = EditorGUILayout.Foldout(shapeCreator.showShapesList, "Show Shapes List");
        if (shapeCreator.showShapesList)
        {
            for (int i = 0; i < shapeCreator.shapes.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Shape" + (i + 1));
                GUI.enabled = i != selectionInfo.selectedShapeIndex;
                if (GUILayout.Button("Select"))
                {
                    selectionInfo.selectedShapeIndex = i;
                }
                GUI.enabled = true;
                if (GUILayout.Button("Delete"))
                {
                    shapeDeleteIndex = i;
                }
                GUILayout.EndHorizontal();
            }
        }
        if (shapeDeleteIndex != -1)
        {
            Undo.RecordObject(shapeCreator, "Delete shape");
            shapeCreator.shapes.RemoveAt(shapeDeleteIndex);
            selectionInfo.selectedShapeIndex = Mathf.Clamp(selectionInfo.selectedShapeIndex, 0, shapeCreator.shapes.Count - 1);
        }
        if (GUI.changed)
        {
            needsRepaint = true;
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI()
    {
        Event guiEvent = Event.current;

        if (guiEvent.type == EventType.Repaint)
        {
            Draw();
        }
        else if (guiEvent.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }
        else
        {
            HandleInput(guiEvent);

            if (needsRepaint)
            {
                HandleUtility.Repaint();
            }
        }
    }

    void createNewShape()
    {
        Undo.RecordObject(shapeCreator, "Create Shape");
        shapeCreator.shapes.Add(new Shape());
        selectionInfo.selectedShapeIndex = shapeCreator.shapes.Count - 1;
    }

    void HandleInput(Event guiEvent)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
        float drawPlaneHeight = 0;
        float distToDrawPlane = (drawPlaneHeight - ray.origin.y) / ray.direction.y;
        Vector3 mousePosition = ray.GetPoint(distToDrawPlane);

        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.None)
        {
            HandleLeftMouseDown(mousePosition);
        }
        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.Shift)
        {
            HandleLeftShiftMouseDown(mousePosition);
        }
        if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0)
        {
            HandleLeftMouseUp(mousePosition);
        }
        if (guiEvent.type == EventType.MouseDrag && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.None)
        {
            HandleLeftMouseDrag(mousePosition);
        }
        if (!selectionInfo.pointIsSelected)
        {
            UpdateMouseOverInfo(mousePosition);
        }
    }

    void CreateNewPoint(Vector3 position)
    {
        bool mouseIsOverSelectedShape = selectionInfo.mouseOverShapeIndex == selectionInfo.selectedShapeIndex;
        int newPointIndex = (selectionInfo.mouseIsOverLine && mouseIsOverSelectedShape) ? selectionInfo.lineIndex + 1 : selectedShape.points.Count;
        Undo.RecordObject(shapeCreator, "Add point");
        selectedShape.points.Insert(newPointIndex, position);
        selectionInfo.pointIndex = newPointIndex;
        selectionInfo.mouseOverShapeIndex = selectionInfo.selectedShapeIndex;
        needsRepaint = true;
        SelectPointUnderMouse();
    }

    void DeletePointUnderMouse()
    {
        Undo.RecordObject(shapeCreator, "Delete Point");
        selectedShape.points.RemoveAt(selectionInfo.pointIndex);
        selectionInfo.pointIsSelected = false;
        selectionInfo.mouseIsOverPoint = false;
        needsRepaint = true;
    }

    void SelectPointUnderMouse()
    {
        selectionInfo.pointIsSelected = true;
        selectionInfo.mouseIsOverPoint = true;
        selectionInfo.mouseIsOverLine = false;
        selectionInfo.lineIndex = -1;
        selectionInfo.positionAtStartOfDrag = selectedShape.points[selectionInfo.pointIndex];
        needsRepaint = true;
    }

    void SeletShapeUnderMouse()
    {
        if (selectionInfo.mouseOverShapeIndex != -1)
        {
            selectionInfo.selectedShapeIndex = selectionInfo.mouseOverShapeIndex;
            needsRepaint = true;
        }
    }

    void HandleLeftShiftMouseDown(Vector3 mousePosition)
    {
        if (selectionInfo.mouseIsOverPoint)
        {
            SeletShapeUnderMouse();
            DeletePointUnderMouse();
        }
        else
        {
            createNewShape();
            CreateNewPoint(mousePosition);
        }
    }

    void HandleLeftMouseDown(Vector3 mousePosition)
    {
        if (shapeCreator.shapes.Count == 0)
        {
            createNewShape();
        }

        SeletShapeUnderMouse();

        if (selectionInfo.mouseIsOverPoint)
        {
            SelectPointUnderMouse();
        }
        else
        {
            CreateNewPoint(mousePosition);
        }
    }

    void HandleLeftMouseUp(Vector3 mousePosition)
    {
        if (selectionInfo.pointIsSelected)
        {
            selectedShape.points[selectionInfo.pointIndex] = selectionInfo.positionAtStartOfDrag;
            Undo.RecordObject(shapeCreator, "Move Point");
            selectedShape.points[selectionInfo.pointIndex] = mousePosition;

            selectionInfo.pointIsSelected = false;
            selectionInfo.pointIndex = -1;
            needsRepaint = true;
        }
    }

    void HandleLeftMouseDrag(Vector3 mousePosition)
    {
        if (selectionInfo.pointIsSelected)
        {
            selectedShape.points[selectionInfo.pointIndex] = mousePosition;
            needsRepaint = true;
        }
    }

    void UpdateMouseOverInfo(Vector3 mousePosition)
    {
        int mouseOverPointIndex = -1;
        int mouseOverShapeIndex = -1;

        for (int shapeIndex = 0; shapeIndex < shapeCreator.shapes.Count; shapeIndex++)
        {
            Shape currentShape = shapeCreator.shapes[shapeIndex];
            for (int i = 0; i < currentShape.points.Count; i++)
            {
                if (Vector3.Distance(mousePosition, currentShape.points[i]) < shapeCreator.handleRadius)
                {
                    mouseOverPointIndex = i;
                    mouseOverShapeIndex = shapeIndex;
                    break;
                }
            }
        }
        if (mouseOverPointIndex != selectionInfo.pointIndex || mouseOverShapeIndex != selectionInfo.mouseOverShapeIndex)
        {
            selectionInfo.mouseOverShapeIndex = mouseOverShapeIndex;
            selectionInfo.pointIndex = mouseOverPointIndex;
            selectionInfo.mouseIsOverPoint = mouseOverPointIndex != -1;
            needsRepaint = true;
        }

        if (selectionInfo.mouseIsOverPoint)
        {
            selectionInfo.mouseIsOverLine = false;
            selectionInfo.lineIndex = -1;
        }
        else
        {
            int mouseOverLineIndex = -1;
            float closestLineDst = shapeCreator.handleRadius;


            for (int shapeIndex = 0; shapeIndex < shapeCreator.shapes.Count; shapeIndex++)
            {
                Shape currentShape = shapeCreator.shapes[shapeIndex];
                for (int i = 0; i < currentShape.points.Count; i++)
                {
                    Vector3 nextPointInShape = currentShape.points[(i + 1) % currentShape.points.Count];
                    float distanceFromMouseToLine = HandleUtility.DistancePointToLineSegment(mousePosition.ConvertToVector2(), currentShape.points[i].ConvertToVector2(), nextPointInShape.ConvertToVector2());
                    if (distanceFromMouseToLine < closestLineDst)
                    {
                        closestLineDst = distanceFromMouseToLine;
                        mouseOverLineIndex = i;
                        mouseOverShapeIndex = shapeIndex;
                    }
                }
            }

            if (selectionInfo.lineIndex != mouseOverLineIndex || mouseOverShapeIndex != selectionInfo.mouseOverShapeIndex)
            {
                selectionInfo.mouseOverShapeIndex = mouseOverShapeIndex;
                selectionInfo.lineIndex = mouseOverLineIndex;
                selectionInfo.mouseIsOverLine = mouseOverLineIndex != -1;
                needsRepaint = true;
            }
        }
    }

    void Draw()
    {
        for (int shapeIndex = 0; shapeIndex < shapeCreator.shapes.Count; shapeIndex++)
        {
            Shape shapeToDraw = shapeCreator.shapes[shapeIndex];
            bool shapeIsSelected = shapeIndex == selectionInfo.selectedShapeIndex;
            bool mouseIsOverShape = shapeIndex == selectionInfo.mouseOverShapeIndex;
            Color deslectedShapeColor = Color.grey;

            for (int i = 0; i < shapeToDraw.points.Count; i++)
            {
                Vector3 nextPoint = shapeToDraw.points[(i + 1) % shapeToDraw.points.Count];
                if (i == selectionInfo.lineIndex && mouseIsOverShape)
                {
                    Handles.color = Color.red;
                    Handles.DrawLine(shapeToDraw.points[i], nextPoint);
                }
                else
                {
                    Handles.color = (shapeIsSelected) ? Color.black : deslectedShapeColor;
                    Handles.DrawDottedLine(shapeToDraw.points[i], nextPoint, 4f);
                }

                if (i == selectionInfo.pointIndex && mouseIsOverShape)
                {
                    Handles.color = (selectionInfo.pointIsSelected) ? Color.black : Color.red;
                }
                else
                {
                    Handles.color = (shapeIsSelected) ? Color.white : deslectedShapeColor;
                }

                Handles.DrawSolidDisc(shapeToDraw.points[i], Vector3.up, shapeCreator.handleRadius);
            }
        }
        if (needsRepaint)
        {
            shapeCreator.UpdateMeshDisplay();
        }

        needsRepaint = false;
    }

    private void OnEnable()
    {
        needsRepaint = true;
        shapeCreator = (ShapeCreator)target;
        selectionInfo = new SelectionInfo();
        Undo.undoRedoPerformed += onUndo;
        Tools.hidden = true;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= onUndo;
        Tools.hidden = false;
    }

    void onUndo()
    {
        if (selectionInfo.selectedShapeIndex >= shapeCreator.shapes.Count || selectionInfo.selectedShapeIndex == -1)
        {
            selectionInfo.selectedShapeIndex = shapeCreator.shapes.Count - 1;
        }
        needsRepaint = true;
    }

    Shape selectedShape { get { return shapeCreator.shapes[selectionInfo.selectedShapeIndex]; } }

    public class SelectionInfo
    {
        public int selectedShapeIndex;
        public int mouseOverShapeIndex;

        public Vector3 positionAtStartOfDrag;
        public int pointIndex = -1;
        public bool mouseIsOverPoint;
        public bool pointIsSelected;

        public int lineIndex = -1;
        public bool mouseIsOverLine;
    }
}
