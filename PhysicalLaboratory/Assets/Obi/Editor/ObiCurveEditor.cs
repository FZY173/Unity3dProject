using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	[CustomEditor(typeof(ObiCurve))] 
	public class ObiCurveEditor : Editor
	{
		
		protected ObiCurve spline;
		protected bool[] selectedStatus;

		protected Quaternion prevRot = Quaternion.identity;
		protected Vector3 prevScale = Vector3.one;

		protected Color handleColor = new Color(1,0.7f,0.2f);
		protected bool editMode = false;
		protected bool addControlPointsMode = false;
		protected bool orientControlPointsMode = false;
		
		public void OnEnable(){
			spline = (ObiCurve)target;
			selectedStatus = new bool[spline.controlPoints.Count];
			ResizeCPArrays();
		}

		public void OnDisable(){
			Tools.hidden = false;
		}		

		protected void ResizeCPArrays(){	
			Array.Resize(ref selectedStatus,spline.controlPoints.Count);
		}
		
		public override void OnInspectorGUI() {
			
			serializedObject.UpdateIfRequiredOrScript();

			ResizeCPArrays();

			EditorGUI.BeginChangeCheck();
			Tools.hidden = editMode = GUILayout.Toggle(editMode,new GUIContent("Edit curve",Resources.Load<Texture2D>("EditCurves")),"LargeButton");
			if (EditorGUI.EndChangeCheck()){
				SceneView.RepaintAll();
			}

			if (spline.controlPoints.Count < spline.MinPoints){
				EditorGUILayout.HelpBox("Curves need at least 2 control points.",MessageType.Error);
			}

			Editor.DrawPropertiesExcluding(serializedObject,"m_Script");
			
			// Apply changes to the serializedProperty
			if (GUI.changed){
				serializedObject.ApplyModifiedProperties();
			}
			
		}

		protected Vector3 GetControlPointAverage(List<ObiCurve.ControlPoint> controlPoints, out int lastSelected, out int selectedCount){

			lastSelected = -1;
			selectedCount = 0;
			Vector3 averagePos = Vector3.zero;

			// Find center of all selected control points:
			for (int i = 0; i < controlPoints.Count; ++i){
				if (selectedStatus[i]){

					averagePos += controlPoints[i].position;
					selectedCount++;
					lastSelected = i;

				}
			}
			if (selectedCount > 0)
				averagePos /= selectedCount;
			return averagePos;

		}

		protected void SplineCPTools(List<ObiCurve.ControlPoint> controlPoints){

			int lastSelected;
			int selectedCount;
			Vector3 averagePos = GetControlPointAverage(controlPoints,out lastSelected, out selectedCount);

			// Calculate handle rotation, for local or world pivot modes.
			Quaternion handleRotation = Tools.pivotRotation == PivotRotation.Local ? Quaternion.identity : Quaternion.Inverse(spline.transform.rotation);

			// Reset initial handle rotation/orientation after using a tool:
			if (GUIUtility.hotControl == 0){

				prevRot = handleRotation;
				prevScale = Vector3.one;

				if (selectedCount == 1 && Tools.pivotRotation == PivotRotation.Local && orientControlPointsMode){	
					prevRot = Quaternion.LookRotation(spline.controlPoints[lastSelected].normal);
				}
			}

			// Transform handles:
			if (selectedCount > 0){

				if (orientControlPointsMode){
					OrientTool(controlPoints,averagePos,handleRotation);
				}else{
					switch (Tools.current)
					{
						case Tool.Move:{
							MoveTool(controlPoints,averagePos,handleRotation);
						}break;
	
						case Tool.Scale:{
							ScaleTool(controlPoints,averagePos,handleRotation);
						}break;
	
						case Tool.Rotate:{
							RotateTool(controlPoints,averagePos,handleRotation);
						}break;
					}
				}
			}
		}

		/**
		 * Draws selected pin constraints in the scene view.
		 */
		public void OnSceneGUI(){

			if (!editMode || spline.controlPoints.Count < spline.MinPoints)
				return;

			ResizeCPArrays();

			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

			// Sceneview GUI:
			Handles.BeginGUI();			

			GUI.skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
			GUILayout.Window(1,new Rect(10,28,0,0),DrawUIWindow,"Curve editor");

			Handles.EndGUI();

			// During edit mode, allow to add/remove control points.
			if (addControlPointsMode)
				AddControlPointsMode();

			Matrix4x4 prevMatrix = Handles.matrix;
			Handles.matrix = spline.transform.localToWorldMatrix;

				DrawControlPoints(spline.controlPoints);
	
				// Control point selection handle:
				if (ObiSplineHandles.SplineCPSelector(spline.controlPoints,selectedStatus))
					Repaint();
	
				// Draw cp tool handles:
				SplineCPTools(spline.controlPoints);

			Handles.matrix = prevMatrix;
		
		}

		private void AddControlPointsMode(){

			float mu = ObiEditorCurveDrawing.ScreenPointToCurveMu(spline,Event.current.mousePosition);

			Vector3 pointOnSpline = spline.transform.TransformPoint(spline.GetPositionAt(mu));

			float size = HandleUtility.GetHandleSize(pointOnSpline) * 0.12f;

			Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			Handles.color = Color.red;
			Handles.DrawDottedLine(pointOnSpline,ray.origin,4); 
			Handles.SphereHandleCap(0,pointOnSpline,Quaternion.identity,size,Event.current.type);

			
			if (Event.current.type == EventType.MouseDown && Event.current.modifiers == EventModifiers.None){
				Undo.RecordObject(spline, "Add control point");
				int newIndex = spline.AddPoint(mu);
				if  (newIndex >= 0){
					ResizeCPArrays();
					for (int i = 0; i < selectedStatus.Length; ++i)
						selectedStatus[i] = false;
					selectedStatus[newIndex] = true;
				}
				Event.current.Use();
			}

			// Repaint the scene, so that the add control point helpers are updated every frame.
			SceneView.RepaintAll();

		}

		protected void MoveTool(List<ObiCurve.ControlPoint> controlPoints, Vector3 handlePosition, Quaternion handleRotation){

			EditorGUI.BeginChangeCheck();
			Vector3 newPos = Handles.PositionHandle(handlePosition,handleRotation);
			if (EditorGUI.EndChangeCheck()){

				Undo.RecordObject(spline, "Move control point");

				Vector3 delta = newPos - handlePosition;

				for (int i = 0; i < controlPoints.Count; ++i){

					if (selectedStatus[i]){
						controlPoints[i] = controlPoints[i].Transform(delta,Quaternion.identity);
					}
				}
			}
		}

		protected void ScaleTool(List<ObiCurve.ControlPoint> controlPoints, Vector3 handlePosition, Quaternion handleRotation){

			EditorGUI.BeginChangeCheck();
				Vector3 scale = Handles.ScaleHandle(prevScale,handlePosition,handleRotation,HandleUtility.GetHandleSize(handlePosition));

			if (EditorGUI.EndChangeCheck()){

				Vector3 deltaScale = new Vector3(scale.x/prevScale.x,scale.y/prevScale.y,scale.z/prevScale.z); 
				prevScale = scale;

				Undo.RecordObject(spline, "Scale control point");

				if (Tools.pivotMode == PivotMode.Center){
					for (int i = 0; i < controlPoints.Count; ++i){
						if (selectedStatus[i]){
							Vector3 newPos = handlePosition + Vector3.Scale(controlPoints[i].position - handlePosition,deltaScale);
							controlPoints[i] = controlPoints[i].Transform(newPos - controlPoints[i].position,Quaternion.identity);
						}
					}
				}else{
					// Scale all handles of selected control points relative to their control point:
					for (int i = 0; i < controlPoints.Count; ++i){
						if (selectedStatus[i]){

							ObiCurve.ControlPoint cp = controlPoints[i];
							cp.inTangent = Vector3.Scale(controlPoints[i].inTangent,deltaScale);
							cp.outTangent = Vector3.Scale(controlPoints[i].outTangent,deltaScale);
							controlPoints[i] = cp;

						}
					}
				}
			}
		}

		protected void RotateTool(List<ObiCurve.ControlPoint> controlPoints, Vector3 handlePosition, Quaternion handleRotation){

			EditorGUI.BeginChangeCheck();
				// TODO: investigate weird rotation gizmo:
				Quaternion newRotation = Handles.RotationHandle(prevRot,handlePosition);
	
			if (EditorGUI.EndChangeCheck()){
	
				Quaternion delta = newRotation * Quaternion.Inverse(prevRot);
					prevRot = newRotation;
	
				Undo.RecordObject(spline, "Rotate control point");
	
				if (Tools.pivotMode == PivotMode.Center){
	
					// Rotate all selected control points around their average:
					for (int i = 0; i < controlPoints.Count; ++i){
						if (selectedStatus[i]){
							Vector3 newPos = handlePosition + delta * (controlPoints[i].position - handlePosition);
							controlPoints[i] = controlPoints[i].Transform(newPos - controlPoints[i].position,Quaternion.identity);
						}
					}
	
				}else{
	
					// Rotate all handles of selected control points around their control point:
					for (int i = 0; i < controlPoints.Count; ++i){
						if (selectedStatus[i]){
							ObiCurve.ControlPoint cp = controlPoints[i];
							cp.inTangent = delta * cp.inTangent;
							cp.outTangent = delta * cp.outTangent;
							controlPoints[i] = cp;
						}
					}
				}
			}
		}

		protected void OrientTool(List<ObiCurve.ControlPoint> controlPoints, Vector3 averagePos, Quaternion pivotRotation){

			EditorGUI.BeginChangeCheck();
				Quaternion newRotation = Handles.RotationHandle(prevRot,averagePos);
	
			if (EditorGUI.EndChangeCheck()){
	
				Quaternion delta = newRotation * Quaternion.Inverse(prevRot);
					prevRot = newRotation;
	
				Undo.RecordObject(spline, "Orient control point");
	
				// Rotate all selected control points around their average:
				for (int i = 0; i < controlPoints.Count; ++i){
					if (selectedStatus[i]){
						ObiCurve.ControlPoint cp = controlPoints[i];
						cp.normal = delta * cp.normal;
						controlPoints[i] = cp;
					}
				}
				
			}
		}

		protected void DrawControlPoint(int i, bool selected){

			ObiCurve.ControlPoint cp = spline.controlPoints[i];

			float size = HandleUtility.GetHandleSize(cp.position) * 0.04f;

			if (selected && !orientControlPointsMode){

				Handles.color = handleColor;

				if (!(i == 0 && !spline.closed)){

					if (Event.current.type == EventType.Repaint)
						Handles.DrawDottedLine(cp.GetInTangent(),cp.position,2);

					EditorGUI.BeginChangeCheck();
					Vector3 newTangent = Handles.FreeMoveHandle(cp.GetInTangent(),Quaternion.identity,size,Vector3.zero,Handles.DotHandleCap);
					if (EditorGUI.EndChangeCheck()){
						Undo.RecordObject(spline, "Modify tangent");
						spline.controlPoints[i] = cp.SetInTangent(newTangent);
					}
				}

				if (!(i == spline.controlPoints.Count-1 && !spline.closed)){

					if (Event.current.type == EventType.Repaint)
						Handles.DrawDottedLine(cp.GetOutTangent(),cp.position,2);

					EditorGUI.BeginChangeCheck();
					Vector3 newTangent = Handles.FreeMoveHandle(cp.GetOutTangent(),Quaternion.identity,size,Vector3.zero,Handles.DotHandleCap);
					if (EditorGUI.EndChangeCheck()){
						Undo.RecordObject(spline, "Modify tangent");
						spline.controlPoints[i] = cp.SetOutTangent(newTangent);
					}
				}

				
			}	
		
			if (Event.current.type == EventType.Repaint){

				Handles.color = selected ? handleColor : Color.white;

				if (orientControlPointsMode)
					Handles.ArrowHandleCap(0,cp.position,Quaternion.LookRotation(cp.normal),HandleUtility.GetHandleSize(cp.position),EventType.Repaint);

				Handles.SphereHandleCap(0,cp.position,Quaternion.identity,size*3,EventType.Repaint); 

			}

		}

		protected void DrawControlPoints(List<ObiCurve.ControlPoint> controlPoints){

			// Draw control points:
			Handles.color = handleColor;
			for (int i = 0; i < controlPoints.Count; ++i){

				DrawControlPoint(i, selectedStatus[i]);
			}

		}

		protected void DrawUIWindow(int windowID) {
			
			GUILayout.BeginHorizontal();
			addControlPointsMode = GUILayout.Toggle(addControlPointsMode,new GUIContent(Resources.Load<Texture2D>("AddControlPoint") ,"Add CPs"),"Button",GUILayout.MaxHeight(24),GUILayout.Width(42));

			if (GUILayout.Button(new GUIContent(Resources.Load<Texture2D>("RemoveControlPoint") ,"Remove selected CPs"),GUILayout.MaxHeight(24),GUILayout.Width(42))){

				Undo.RecordObject(spline, "Remove control point");

				for (int i = spline.controlPoints.Count-1; i >= 0; --i){
					if (selectedStatus[i]){ 
						spline.controlPoints.RemoveAt(i);
					}
				}

				for (int i = 0; i < selectedStatus.Length; ++i)
					selectedStatus[i] = false;

				ResizeCPArrays();
			}

			orientControlPointsMode = GUILayout.Toggle(orientControlPointsMode,new GUIContent(Resources.Load<Texture2D>("OrientControlPoint") ,"Orient CPs"),"Button",GUILayout.MaxHeight(24),GUILayout.Width(42));
			GUILayout.EndHorizontal();

			EditorGUI.showMixedValue = false;
			ObiCurve.ControlPoint.BezierCPMode mode = ObiCurve.ControlPoint.BezierCPMode.Free;
			bool firstSelected = true;
			for (int i = 0; i < spline.controlPoints.Count; ++i){
				if (selectedStatus[i]){
					if (firstSelected){
						mode = spline.controlPoints[i].tangentMode;
						firstSelected = false;
					}else if (mode != spline.controlPoints[i].tangentMode){
						EditorGUI.showMixedValue = true;
						break;
					}
				}
			}

			EditorGUI.BeginChangeCheck();
			ObiCurve.ControlPoint.BezierCPMode newMode = (ObiCurve.ControlPoint.BezierCPMode) EditorGUILayout.EnumPopup(mode,GUI.skin.FindStyle("DropDown"));
			EditorGUI.showMixedValue = false;
			if (EditorGUI.EndChangeCheck()){

				Undo.RecordObject(spline, "Change control points mode");

				for (int i = 0; i < spline.controlPoints.Count; ++i){
					if (selectedStatus[i]){
						ObiCurve.ControlPoint cp = spline.controlPoints[i];
						cp.tangentMode = newMode;
						spline.controlPoints[i] = cp;
					}
				}
			}
			
		}

		[DrawGizmo(GizmoType.Selected)]
	    private static void DrawGizmos(ObiCurve spline, GizmoType gizmoType)
	    {
			ObiEditorCurveDrawing.DrawSplineGizmos(spline,30,Color.white,new Color(1,0,0,0.5f),new Color(1,1,1,0.5f));
	    }

		[DrawGizmo(GizmoType.NonSelected)]
	    private static void DrawGizmosNonSelected(ObiCurve spline, GizmoType gizmoType)
	    {
			ObiEditorCurveDrawing.DrawSplineGizmos(spline,30,new Color(1,1,1,0.5f),new Color(1,0,0,0.5f),Color.white,false);
	    }
		
	}
}

