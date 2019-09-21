using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
	 * Custom inspector for ObiRod components.
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
	[CustomEditor(typeof(ObiRod)), CanEditMultipleObjects] 
	public class ObiRodEditor : ObiParticleActorEditor
	{

		public class RodParticleProperty : ParticleProperty
		{
		  public const int RotationalMass = 3;

		  public RodParticleProperty (int value) : base (value){}
		}

		[MenuItem("GameObject/3D Object/Obi/Obi Rod (fully set up)",false,4)]
		static void CreateObiRope()
		{
			GameObject c = new GameObject("Obi Rod");
			Undo.RegisterCreatedObjectUndo(c,"Create Obi Rod");
			ObiRod rope = c.AddComponent<ObiRod>();
			ObiCurve path = c.AddComponent<ObiCurve>();
			ObiSolver solver = c.AddComponent<ObiSolver>();
			c.AddComponent<ObiRopeExtrudedRenderer>();
			
			rope.Solver = solver;
			rope.ropePath = path;
		}
		
		ObiRod rod;
		
		public override void OnEnable(){
			base.OnEnable();
			rod = (ObiRod)target;

			particlePropertyNames.AddRange(new string[]{"Rotational Mass"});
		}
		
		public override void OnDisable(){
			base.OnDisable();
			EditorUtility.ClearProgressBar();
		}

		public override void UpdateParticleEditorInformation(){
			
			for(int i = 0; i < rod.positions.Length; i++)
			{
				wsPositions[i] = rod.GetParticlePosition(i);
				wsOrientations[i] = rod.GetParticleOrientation(i);	
				facingCamera[i] = true;		
			}

		}
		
		protected override void SetPropertyValue(ParticleProperty property,int index, float value){
			if (index >= 0 && index < rod.invMasses.Length){
				switch(property){
					case RodParticleProperty.Mass: 
							rod.invMasses[index] = 1.0f / Mathf.Max(value,0.00001f);
						break; 
					case RodParticleProperty.RotationalMass: 
							rod.invRotationalMasses[index] = 1.0f / Mathf.Max(value,0.00001f);
						break;
					case RodParticleProperty.Radius:
							rod.principalRadii[index] = Vector3.one * value;
						break;
					case RodParticleProperty.Layer:
							rod.phases[index] = Oni.MakePhase((int)value,rod.SelfCollisions?Oni.ParticlePhase.SelfCollide:0);
						break;
				}
			}
		}
		
		protected override float GetPropertyValue(ParticleProperty property, int index){
			if (index >= 0 && index < rod.invMasses.Length){
				switch(property){
					case RodParticleProperty.Mass:
						return 1.0f/rod.invMasses[index];
					case RodParticleProperty.RotationalMass:
						return 1.0f/rod.invRotationalMasses[index];
					case RodParticleProperty.Radius:
						return rod.principalRadii[index][0];
					case RodParticleProperty.Layer:
						return Oni.GetGroupFromPhase(rod.phases[index]);
				}
			}
			return 0;
		}

		protected override void UpdatePropertyInSolver(){

			base.UpdatePropertyInSolver();

			switch(currentProperty){
				case RodParticleProperty.RotationalMass:
				 	rod.PushDataToSolver(ParticleData.INV_ROTATIONAL_MASSES);
				break;
			}

		}

		protected override void FixSelectedParticles(){
			base.FixSelectedParticles();
			for(int i = 0; i < selectionStatus.Length; i++){
				if (selectionStatus[i]){
					if (rod.invRotationalMasses[i] != 0){	
						SetPropertyValue(RodParticleProperty.RotationalMass,i,Mathf.Infinity);
						newProperty = GetPropertyValue(currentProperty,i);
						rod.angularVelocities[i] = Vector3.zero;
					}
				}
			}
			rod.PushDataToSolver(ParticleData.INV_ROTATIONAL_MASSES | ParticleData.ANGULAR_VELOCITIES);
		}

		protected override void FixSelectedParticlesTranslation(){
			base.FixSelectedParticlesTranslation();
			for(int i = 0; i < selectionStatus.Length; i++){
				if (selectionStatus[i]){
					if (rod.invRotationalMasses[i] == 0){	
						SetPropertyValue(RodParticleProperty.RotationalMass,i,1);
						newProperty = GetPropertyValue(currentProperty,i);
					}
				}
			}
			rod.PushDataToSolver(ParticleData.INV_ROTATIONAL_MASSES | ParticleData.ANGULAR_VELOCITIES);
		}

		protected override void UnfixSelectedParticles(){
			base.UnfixSelectedParticles();
			for(int i = 0; i < selectionStatus.Length; i++){
				if (selectionStatus[i]){
					if (rod.invRotationalMasses[i] == 0){	
						SetPropertyValue(RodParticleProperty.RotationalMass,i,1);
						newProperty = GetPropertyValue(currentProperty,i);
					}
				}
			}
			rod.PushDataToSolver(ParticleData.INV_ROTATIONAL_MASSES);
		}		

		public override void OnInspectorGUI() {
			
			serializedObject.Update();

			GUI.enabled = rod.Initialized;
			EditorGUI.BeginChangeCheck();
			editMode = GUILayout.Toggle(editMode,new GUIContent("Edit particles",Resources.Load<Texture2D>("EditParticles")),"LargeButton");
			if (EditorGUI.EndChangeCheck()){
				SceneView.RepaintAll();
			}
			GUI.enabled = true;			

			EditorGUILayout.LabelField("Status: "+ (rod.Initialized ? "Initialized":"Not initialized"));

			GUI.enabled = (rod.ropePath != null);
			if (GUILayout.Button("Initialize")){
				if (!rod.Initialized){
					EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
					CoroutineJob job = new CoroutineJob();
					routine = job.Start(rod.GeneratePhysicRepresentationForMesh());
					EditorCoroutine.ShowCoroutineProgressBar("Generating physical representation...",ref routine);
					EditorGUIUtility.ExitGUI();
				}else{
					if (EditorUtility.DisplayDialog("Actor initialization","Are you sure you want to re-initialize this actor?","Ok","Cancel")){
						EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
						CoroutineJob job = new CoroutineJob();
						routine = job.Start(rod.GeneratePhysicRepresentationForMesh());
						EditorCoroutine.ShowCoroutineProgressBar("Generating physical representation...",ref routine);
						EditorGUIUtility.ExitGUI();
					}
				}
			}
			GUI.enabled = true;

			GUI.enabled = rod.Initialized;
			if (GUILayout.Button("Set Rest State")){
				Undo.RecordObject(rod, "Set rest state");
				rod.PullDataFromSolver(ParticleData.POSITIONS | ParticleData.VELOCITIES);
			}
			GUI.enabled = true;	
			
			if (rod.ropePath == null){
				EditorGUILayout.HelpBox("Rod path spline is missing.",MessageType.Info);
			}

			Editor.DrawPropertiesExcluding(serializedObject,"m_Script","chainLinks");
	
			// Apply changes to the serializedProperty
			if (GUI.changed){
				serializedObject.ApplyModifiedProperties();
			}
			
		}
	}
}


