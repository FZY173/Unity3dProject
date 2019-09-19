using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
	 * Custom inspector for ObiRope components.
	 * Allows particle selection and constraint edition. 
	 * 
	 * Selection:
	 * 
	 * - To select a particle, left-click on it. 
	 * - You can select multiple particles by holding shift while clicking.
	 * - To deselect all particles, click anywhere on the object except a particle.
	 * 
	 * Constraints:
	 * 
	 * - To edit particle constraints, select the particles you wish to edit.
	 * - Constraints affecting any of the selected particles will appear in the inspector.
	 * - To add a new pin constraint to the selected particle(s), click on "Add Pin Constraint".
	 * 
	 */
	[CustomEditor(typeof(ObiRope)), CanEditMultipleObjects] 
	public class ObiRopeEditor : ObiParticleActorEditor
	{

		public class TearableRopeParticleProperty : ParticleProperty
		{
		  public const int TearResistance = 3;

		  public TearableRopeParticleProperty (int value) : base (value){}
		}

		[MenuItem("Assets/Create/Obi/Obi Rope Section")]
		public static void CreateObiRopeSection ()
		{
			ObiEditorUtils.CreateAsset<ObiRopeSection> ();
		}

		[MenuItem("GameObject/3D Object/Obi/Obi Rope (fully set up)",false,4)]
		static void CreateObiRope()
		{
			GameObject c = new GameObject("Obi Rope");
			Undo.RegisterCreatedObjectUndo(c,"Create Obi Rope");
			ObiRope rope = c.AddComponent<ObiRope>();
			ObiCurve path = c.AddComponent<ObiCurve>();
			ObiSolver solver = c.AddComponent<ObiSolver>();
			c.AddComponent<ObiRopeExtrudedRenderer>();
			
			rope.Solver = solver;
			rope.ropePath = path;
		}
		
		ObiRope rope;
		
		public override void OnEnable(){
			base.OnEnable();
			rope = (ObiRope)target;

			particlePropertyNames.AddRange(new string[]{"Tear Resistance"});
		}
		
		public override void OnDisable(){
			base.OnDisable();
			EditorUtility.ClearProgressBar();
		}

		public override void UpdateParticleEditorInformation(){
			
			for(int i = 0; i < rope.positions.Length; i++)
			{
				wsPositions[i] = rope.GetParticlePosition(i);
				wsOrientations[i] = rope.GetParticleOrientation(i);	
				facingCamera[i] = true;		
			}

		}
		
		protected override void SetPropertyValue(ParticleProperty property,int index, float value){
			if (index >= 0 && index < rope.invMasses.Length){
				switch(property){
					case ParticleProperty.Mass: 
							rope.invMasses[index] = 1.0f / Mathf.Max(value,0.00001f);
						break; 
					case ParticleProperty.Radius:
							rope.principalRadii[index] = Vector3.one * value;
						break;
					case ParticleProperty.Layer:
							rope.phases[index] = Oni.MakePhase((int)value,rope.SelfCollisions?Oni.ParticlePhase.SelfCollide:0);;
						break;
					case TearableRopeParticleProperty.TearResistance:
							rope.tearResistance[index] = value;
						break;
				}
			}
		}
		
		protected override float GetPropertyValue(ParticleProperty property, int index){
			if (index >= 0 && index < rope.invMasses.Length){
				switch(property){
					case ParticleProperty.Mass:
						return 1.0f/rope.invMasses[index];
					case ParticleProperty.Radius:
						return rope.principalRadii[index][0];
					case ParticleProperty.Layer:
						return Oni.GetGroupFromPhase(rope.phases[index]);
					case TearableRopeParticleProperty.TearResistance:
						return rope.tearResistance[index];
				}
			}
			return 0;
		}

		private void BakeMesh(){

			ObiRopeExtrudedRenderer extruded = rope.GetComponent<ObiRopeExtrudedRenderer>();
			ObiRopeMeshRenderer deformed = rope.GetComponent<ObiRopeMeshRenderer>();

			if (extruded != null && extruded.extrudedMesh != null){
				ObiEditorUtils.SaveMesh(extruded.extrudedMesh,"Save extruded mesh","rope mesh");
			}
			if (deformed != null && deformed.deformedMesh != null){
				ObiEditorUtils.SaveMesh(deformed.deformedMesh,"Save deformed mesh","rope mesh");
			}
		}

		public override void OnInspectorGUI() {
			
			serializedObject.Update();

			GUI.enabled = rope.Initialized;
			EditorGUI.BeginChangeCheck();
			editMode = GUILayout.Toggle(editMode,new GUIContent("Edit particles",Resources.Load<Texture2D>("EditParticles")),"LargeButton");
			if (EditorGUI.EndChangeCheck()){
				SceneView.RepaintAll();
			}
			GUI.enabled = true;			

			EditorGUILayout.LabelField("Status: "+ (rope.Initialized ? "Initialized":"Not initialized"));

			GUI.enabled = (rope.ropePath != null);
			if (GUILayout.Button("Initialize")){
				if (!rope.Initialized){
					EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
					CoroutineJob job = new CoroutineJob();
					routine = job.Start(rope.GeneratePhysicRepresentationForMesh());
					EditorCoroutine.ShowCoroutineProgressBar("Generating physical representation...",ref routine);
					EditorGUIUtility.ExitGUI();
				}else{
					if (EditorUtility.DisplayDialog("Actor initialization","Are you sure you want to re-initialize this actor?","Ok","Cancel")){
						EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
						CoroutineJob job = new CoroutineJob();
						routine = job.Start(rope.GeneratePhysicRepresentationForMesh());
						EditorCoroutine.ShowCoroutineProgressBar("Generating physical representation...",ref routine);
						EditorGUIUtility.ExitGUI();
					}
				} 
			}
			GUI.enabled = true;

			GUI.enabled = rope.Initialized;

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Set Rest State")){
				Undo.RecordObject(rope, "Set rest state");
				rope.PullDataFromSolver(ParticleData.POSITIONS | ParticleData.VELOCITIES);
			}
			if (GUILayout.Button("Bake Mesh")){
				BakeMesh();
			}
			GUILayout.EndHorizontal();

			GUI.enabled = true;	
			
			if (rope.ropePath == null){
				EditorGUILayout.HelpBox("Rope path spline is missing.",MessageType.Info);
			}

			Editor.DrawPropertiesExcluding(serializedObject,"m_Script","chainLinks");
	
			// Apply changes to the serializedProperty
			if (GUI.changed){
				serializedObject.ApplyModifiedProperties();
			}
			
		}
	}
}


