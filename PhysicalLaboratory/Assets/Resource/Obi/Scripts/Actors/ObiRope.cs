using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi
{
	
	/**
	 * Rope made of Obi particles. No mesh or topology is needed to generate a physic representation from,
	 * since the mesh is generated procedurally.
	 */
	[ExecuteInEditMode]
	[AddComponentMenu("Physics/Obi/Obi Rope")]
	[RequireComponent(typeof (ObiDistanceConstraints))]
	[RequireComponent(typeof (ObiBendingConstraints))]
	[RequireComponent(typeof (ObiTetherConstraints))]
	[RequireComponent(typeof (ObiPinConstraints))]
	[DisallowMultipleComponent]
	public class ObiRope : ObiRopeBase
	{

		[Tooltip("Amount of additional particles in this rope's pool that can be used to extend its lenght, or to tear it.")]
		public int pooledParticles = 10;

		public bool tearable = false;

		[Tooltip("Maximum strain betweeen particles before the spring constraint holding them together would break.")]
		[Indent(order = 0)]
		[VisibleIf("tearable",order = 1)]
		[MultiDelayed(order = 2)]
		public float tearResistanceMultiplier = 1000;

		[Indent]
		[VisibleIf("tearable")]
		public GameObject tearPrefab;		

		[HideInInspector] public float[] tearResistance; 	/**< Per-particle tear resistances.*/
		[HideInInspector][NonSerialized] public GameObject[] tearPrefabPool;

		public ObiDistanceConstraints DistanceConstraints{
			get{return GetConstraints(Oni.ConstraintType.Distance) as ObiDistanceConstraints;}
		}
		public ObiBendingConstraints BendingConstraints{
			get{return GetConstraints(Oni.ConstraintType.Bending) as ObiBendingConstraints;}
		}

		public override float InterparticleDistance{
			get{return interParticleDistance * DistanceConstraints.stretchingScale;}
		}

		public override int UsedParticles{
			get{return usedParticles;}
			set{
				usedParticles = value;
				pooledParticles = totalParticles-usedParticles;
			}
		}

		public int PooledParticles{
			get{return pooledParticles;}
		}
	     
		public override void OnValidate(){
			tearResistanceMultiplier = Mathf.Max(0.1f,tearResistanceMultiplier);
			base.OnValidate();
	    }

		public override void OnSolverStepEnd(float deltaTime){	

			base.OnSolverStepEnd(deltaTime);

			if (isActiveAndEnabled){
				ApplyTearing();
			}
		}
		
		/**
	 	* Generates the particle based physical representation of the rope. This is the initialization method for the rope object
		* and should not be called directly once the object has been created.
	 	*/
		protected override IEnumerator Initialize()
		{	
			initialized = false;			
			initializing = true;	
			interParticleDistance = -1;

			RemoveFromSolver(null);

			if (ropePath == null){
				Debug.LogError("Cannot initialize rope. There's no ropePath present. Please provide a spline to define the shape of the rope");
				yield break;
			}

			ropePath.RecalculateSplineLenght(0.00001f,7);
			closed = ropePath.closed;
			restLength = ropePath.Length;

			usedParticles = Mathf.CeilToInt(restLength/thickness * resolution) + (closed ? 0:1);
			totalParticles = usedParticles + pooledParticles; //allocate extra particles to allow for lenght change and tearing.

			active = new bool[totalParticles];
			positions = new Vector3[totalParticles];
			velocities = new Vector3[totalParticles];
			invMasses  = new float[totalParticles];
			principalRadii = new Vector3[totalParticles];
			phases = new int[totalParticles];
			restPositions = new Vector4[totalParticles];
			tearResistance = new float[totalParticles];
			colors = new Color[totalParticles];
			
			int numSegments = usedParticles - (closed ? 0:1);
			if (numSegments > 0)
				interParticleDistance = restLength/(float)numSegments;
			else 
				interParticleDistance = 0;

			float radius = interParticleDistance * resolution;

			for (int i = 0; i < usedParticles; i++){

				active[i] = true;
				invMasses[i] = 1.0f/DEFAULT_PARTICLE_MASS;
				float mu = ropePath.GetMuAtLenght(interParticleDistance*i);
				positions[i] = transform.InverseTransformPoint(ropePath.transform.TransformPoint(ropePath.GetPositionAt(mu)));
				principalRadii[i] = Vector3.one * radius;
				phases[i] = Oni.MakePhase(1,selfCollisions?Oni.ParticlePhase.SelfCollide:0);
				tearResistance[i] = 1;
				colors[i] = Color.white;

				if (i % 100 == 0)
					yield return new CoroutineJob.ProgressInfo("ObiRope: generating particles...",i/(float)usedParticles);

			}

			// Initialize basic data for pooled particles:
			for (int i = usedParticles; i < totalParticles; i++){

				active[i] = false;
				invMasses[i] = 1.0f/DEFAULT_PARTICLE_MASS;
				principalRadii[i] = Vector3.one * radius;
				phases[i] = Oni.MakePhase(1,selfCollisions?Oni.ParticlePhase.SelfCollide:0);
				tearResistance[i] = 1;
				colors[i] = Color.white;

				if (i % 100 == 0)
					yield return new CoroutineJob.ProgressInfo("ObiRope: generating particles...",i/(float)usedParticles);

			}

			DistanceConstraints.Clear();
			ObiDistanceConstraintBatch distanceBatch = new ObiDistanceConstraintBatch(false,false,MIN_YOUNG_MODULUS,MAX_YOUNG_MODULUS);
			DistanceConstraints.AddBatch(distanceBatch);

			for (int i = 0; i < numSegments; i++){

				distanceBatch.AddConstraint(i,(i+1) % (ropePath.closed ? usedParticles:usedParticles+1),interParticleDistance,1,1);		

				if (i % 500 == 0)
					yield return new CoroutineJob.ProgressInfo("ObiRope: generating structural constraints...",i/(float)numSegments);

			}

			BendingConstraints.Clear();
			ObiBendConstraintBatch bendingBatch = new ObiBendConstraintBatch(false,false,MIN_YOUNG_MODULUS,MAX_YOUNG_MODULUS);
			BendingConstraints.AddBatch(bendingBatch);
			for (int i = 0; i < usedParticles - (closed?0:2); i++){

				// rope bending constraints always try to keep it completely straight:
				bendingBatch.AddConstraint(i,(i+2) % usedParticles,(i+1) % usedParticles,0,0,1);
			
				if (i % 500 == 0)
					yield return new CoroutineJob.ProgressInfo("ObiRope: adding bend constraints...",i/(float)usedParticles);

			}
			
			// Initialize tether constraints:
			TetherConstraints.Clear();

			// Initialize pin constraints:
			PinConstraints.Clear();
			ObiPinConstraintBatch pinBatch = new ObiPinConstraintBatch(false,false,0,MAX_YOUNG_MODULUS);
			PinConstraints.AddBatch(pinBatch);

			initializing = false;
			initialized = true;

			RegenerateRestPositions();

		}

		/**
		 * Generates new valid rest positions for the entire rope.
		 */
		public override void RegenerateRestPositions(){

			ObiDistanceConstraintBatch distanceBatch = DistanceConstraints.GetFirstBatch();		

			// Iterate trough all distance constraints in order:
			int particle = -1;
			int lastParticle = -1;
			float accumulatedDistance = 0;
			for (int i = 0; i < distanceBatch.ConstraintCount; ++i){

				if (i == 0){
					lastParticle = particle = distanceBatch.springIndices[i*2];
					restPositions[particle] = new Vector4(0,0,0,1);
				}		
				
				accumulatedDistance += Mathf.Min(interParticleDistance,principalRadii[particle][0],principalRadii[lastParticle][0]);

				particle = distanceBatch.springIndices[i*2+1];
				restPositions[particle] = Vector3.right * accumulatedDistance;
				restPositions[particle][3] = 1; // activate rest position

			}

			PushDataToSolver(ParticleData.REST_POSITIONS);
		}

		protected override void GeneratePrefabInstances(){

			base.GeneratePrefabInstances();

			if (tearPrefab != null){

				// create tear prefab pool, two per potential cut:
				tearPrefabPool = new GameObject[pooledParticles*2];

				for (int i = 0; i < tearPrefabPool.Length; ++i){
					GameObject tearPrefabInstance = GameObject.Instantiate(tearPrefab);
					tearPrefabInstance.hideFlags = HideFlags.HideAndDontSave;
					tearPrefabInstance.SetActive(false);
					tearPrefabPool[i] = tearPrefabInstance;
				}

			}
		}

		protected override void ClearPrefabInstances(){

			base.ClearPrefabInstances();

			if (tearPrefabPool != null){
				for (int i = 0; i < tearPrefabPool.Length; ++i){
					if (tearPrefabPool[i] != null){
						GameObject.DestroyImmediate(tearPrefabPool[i]);
						tearPrefabPool[i] = null;
					}
				}
			}

		}

		public override void UpdateTearPrefab(ObiCurveFrame frame, ref int tearCount,bool reverseLookDirection){
			if (tearPrefabPool != null && tearCount < tearPrefabPool.Length){
				if (!tearPrefabPool[tearCount].activeSelf)
					 tearPrefabPool[tearCount].SetActive(true);
			
				PlaceObjectAtCurveFrame(frame,tearPrefabPool[tearCount],Space.Self, reverseLookDirection);
				tearCount++;
			}
		}

		/**
		 * Returns the particle indices affected by a structural constraint
		 */
		public override bool GetStructuralConstraintParticles(int constraintIndex, ref int particleIndex1, ref int particleIndex2){
		
			ObiDistanceConstraintBatch distanceBatch = DistanceConstraints.GetFirstBatch();
			if (distanceBatch != null && constraintIndex >= 0 && constraintIndex < distanceBatch.ConstraintCount){
				particleIndex1 = distanceBatch.springIndices[constraintIndex*2];
				particleIndex2 = distanceBatch.springIndices[constraintIndex*2+1];
				return true;
			}
			return false;
		}

		/**
		 * Returns the rest length of a structural constraint
		 */
		public override float GetStructuralConstraintRestLength(int constraintIndex){
			ObiDistanceConstraintBatch distanceBatch = DistanceConstraints.GetFirstBatch();
			if (distanceBatch != null && constraintIndex >= 0 && constraintIndex < distanceBatch.ConstraintCount)
				return distanceBatch.restLengths[constraintIndex];
			return 0;
		}

		/**
		 * Returns the amount of structural constraints in the rope.
		 */
		public override int GetStructuralConstraintCount(){
		
			ObiDistanceConstraintBatch distanceBatch = DistanceConstraints.GetFirstBatch();
			return distanceBatch != null ? distanceBatch.ConstraintCount : 0;
		}

		/**
		 * Returns the index of the structural constraint at a given normalized rope coordinate.
		 */
		public override int GetConstraintIndexAtNormalizedCoordinate(float coord){

			ObiDistanceConstraintBatch distanceBatch = DistanceConstraints.GetFirstBatch();	
			if (distanceBatch != null){
				float mu = coord * distanceBatch.ConstraintCount;
				return Mathf.Clamp(Mathf.FloorToInt(mu),0,distanceBatch.ConstraintCount-1);
			}
			return -1;		
		}

		public override void TransportFrame(ObiCurveFrame frame, ObiCurveSection section, float sectionTwist){
			if (frame != null)
				frame.Transport(section,sectionTwist);
		}
		
		/**
 		* Resets mesh to its original state.
 		*/
		public override void ResetActor(){
	
			PushDataToSolver(ParticleData.POSITIONS | ParticleData.VELOCITIES);
			
			if (particleIndices != null){
				for(int i = 0; i < particleIndices.Length; ++i){
					solver.renderablePositions[particleIndices[i]] = positions[i];
				}
			}

		}

		protected void ApplyTearing(){

			if (!tearable) 
				return;
	
			ObiDistanceConstraintBatch distanceBatch = DistanceConstraints.GetFirstBatch();
			float[] forces = new float[distanceBatch.ConstraintCount];
			Oni.GetBatchConstraintForces(distanceBatch.OniBatch,forces,distanceBatch.ConstraintCount,0);	
	
			List<int> tearedEdges = new List<int>();
			for (int i = 0; i < forces.Length; i++){
	
				float p1Resistance = tearResistance[distanceBatch.springIndices[i*2]];
				float p2Resistance = tearResistance[distanceBatch.springIndices[i*2+1]];

				// average particle resistances:
				float resistance = (p1Resistance + p2Resistance) * 0.5f * tearResistanceMultiplier;
	
				if (-forces[i] * 1000 > resistance){ // units are kilonewtons.
					tearedEdges.Add(i);
				}
			}
	
			if (tearedEdges.Count > 0){
	
				DistanceConstraints.RemoveFromSolver(null);
				BendingConstraints.RemoveFromSolver(null);
				for(int i = 0; i < tearedEdges.Count; i++)
					Tear(tearedEdges[i]);
				BendingConstraints.AddToSolver(this);
				DistanceConstraints.AddToSolver(this);
	
				// update active bending constraints:
				BendingConstraints.SetActiveConstraints();
	
				// upload active particle list to solver:
				solver.UpdateActiveParticles();
			}
			
		}

		/**
		 * Returns whether a bend constraint affects the two particles referenced by a given distance constraint:
		 */
		public bool DoesBendConstraintSpanDistanceConstraint(ObiDistanceConstraintBatch dbatch, ObiBendConstraintBatch bbatch, int d, int b){

		return (bbatch.bendingIndices[b*3+2] == dbatch.springIndices[d*2] &&
			 	bbatch.bendingIndices[b*3+1] == dbatch.springIndices[d*2+1]) ||

			   (bbatch.bendingIndices[b*3+1] == dbatch.springIndices[d*2] &&
			 	bbatch.bendingIndices[b*3+2] == dbatch.springIndices[d*2+1]) ||

			   (bbatch.bendingIndices[b*3+2] == dbatch.springIndices[d*2] &&
			 	bbatch.bendingIndices[b*3] == dbatch.springIndices[d*2+1]) ||

			   (bbatch.bendingIndices[b*3] == dbatch.springIndices[d*2] &&
			 	bbatch.bendingIndices[b*3+2] == dbatch.springIndices[d*2+1]);
		}	

		public void Tear(int constraintIndex){

			// don't allow splitting if there are no free particles left in the pool.
			if (usedParticles >= totalParticles) return;
	
			// get involved constraint batches: 
			ObiDistanceConstraintBatch distanceBatch = (ObiDistanceConstraintBatch)DistanceConstraints.GetFirstBatch();
			ObiBendConstraintBatch bendingBatch = (ObiBendConstraintBatch)BendingConstraints.GetFirstBatch();
	
			// get particle indices at both ends of the constraint:
			int splitIndex = distanceBatch.springIndices[constraintIndex*2];
			int intactIndex = distanceBatch.springIndices[constraintIndex*2+1];

			// see if the rope is continuous at the split index and the intact index:
			bool continuousAtSplit = (constraintIndex < distanceBatch.ConstraintCount-1 && distanceBatch.springIndices[(constraintIndex+1)*2] == splitIndex) || 
									 (constraintIndex > 0 && distanceBatch.springIndices[(constraintIndex-1)*2+1] == splitIndex);

			bool continuousAtIntact = (constraintIndex < distanceBatch.ConstraintCount-1 && distanceBatch.springIndices[(constraintIndex+1)*2] == intactIndex) || 
									  (constraintIndex > 0 && distanceBatch.springIndices[(constraintIndex-1)*2+1] == intactIndex);
	
			// we will split the particle with higher mass, so swap them if needed (and possible). Also make sure that the rope hasnt been cut there yet:
			if ((invMasses[splitIndex] > invMasses[intactIndex] || invMasses[splitIndex] == 0) &&
				continuousAtIntact){

				int aux = splitIndex;
				splitIndex = intactIndex;
				intactIndex = aux;

			} 

			// see if we are able to proceed with the cut:
			if (invMasses[splitIndex] == 0 || !continuousAtSplit){	
				return;
			}

			// halve the mass of the teared particle:
			invMasses[splitIndex] *= 2;

			// copy the new particle data in the actor and solver arrays:
			positions[usedParticles] = positions[splitIndex];
			velocities[usedParticles] = velocities[splitIndex];
			active[usedParticles] = active[splitIndex];
			invMasses[usedParticles] = invMasses[splitIndex];
			principalRadii[usedParticles] = principalRadii[splitIndex];
			phases[usedParticles] = phases[splitIndex];
	
			if (colors != null && colors.Length > 0)
				colors[usedParticles] = colors[splitIndex];
			tearResistance[usedParticles] = tearResistance[splitIndex];
			restPositions[usedParticles] = positions[splitIndex];
			restPositions[usedParticles][3] = 1; // activate rest position.
			
			// update solver particle data:
			solver.velocities[particleIndices[usedParticles]] = solver.velocities[particleIndices[splitIndex]];
			solver.startPositions[particleIndices[usedParticles]] = solver.positions [particleIndices[usedParticles]] = solver.positions [particleIndices[splitIndex]];
			
			solver.invMasses [particleIndices[usedParticles]] = solver.invMasses [particleIndices[splitIndex]] = invMasses[splitIndex];
			solver.principalRadii[particleIndices[usedParticles]] = solver.principalRadii[particleIndices[splitIndex]] = principalRadii[splitIndex];
			solver.phases	 [particleIndices[usedParticles]] = solver.phases	 [particleIndices[splitIndex]];

			// Update bending constraints:
			for (int i = 0 ; i < bendingBatch.ConstraintCount; ++i){

				// disable the bending constraint centered at the split particle:
				if (bendingBatch.bendingIndices[i*3+2] == splitIndex)
					bendingBatch.DeactivateConstraint(i);

				// update the one that bridges the cut:
				else if (!DoesBendConstraintSpanDistanceConstraint(distanceBatch,bendingBatch,constraintIndex,i)){

					// if the bend constraint does not involve the split distance constraint, 
					// update the end that references the split vertex:
					if (bendingBatch.bendingIndices[i*3] == splitIndex)
						bendingBatch.bendingIndices[i*3] = usedParticles;
					else if (bendingBatch.bendingIndices[i*3+1] == splitIndex)
						bendingBatch.bendingIndices[i*3+1] = usedParticles;

				}
			}

			// Update distance constraints at both ends of the cut:
			if (constraintIndex < distanceBatch.ConstraintCount-1){
				if (distanceBatch.springIndices[(constraintIndex+1)*2] == splitIndex)
					distanceBatch.springIndices[(constraintIndex+1)*2] = usedParticles;
				if (distanceBatch.springIndices[(constraintIndex+1)*2+1] == splitIndex)
					distanceBatch.springIndices[(constraintIndex+1)*2+1] = usedParticles;
			}	

			if (constraintIndex > 0){
				if (distanceBatch.springIndices[(constraintIndex-1)*2] == splitIndex)
					distanceBatch.springIndices[(constraintIndex-1)*2] = usedParticles;
				if (distanceBatch.springIndices[(constraintIndex-1)*2+1] == splitIndex)
					distanceBatch.springIndices[(constraintIndex-1)*2+1] = usedParticles;
			}

			usedParticles++;
			pooledParticles--;

		}

	}
}



