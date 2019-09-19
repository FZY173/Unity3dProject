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
	[RequireComponent(typeof (ObiTetherConstraints))]
	[RequireComponent(typeof (ObiPinConstraints))]
	[DisallowMultipleComponent]
	public abstract class ObiRopeBase : ObiActor
	{
		public const float DEFAULT_PARTICLE_MASS = 0.1f;
		public const float MAX_YOUNG_MODULUS = 25000000000;
		public const float MIN_YOUNG_MODULUS = 100; 

		[Tooltip("Path used to generate the rope.")]
		public ObiCurve ropePath = null;

		public bool thicknessFromParticles = true;	/**< Gets rope thickness from particle radius.*/

		[Tooltip("Thickness of the rope, it is equivalent to particle radius.")]
		[Indent]
		[VisibleIf("thicknessFromParticles",true)]
		public float thickness = 0.05f;				/**< Thickness of the rope.*/

		[Tooltip("Modulates the amount of particles per lenght unit. 1 means as many particles as needed for the given length/thickness will be used, which"+
				 "can be a lot in very thin and long ropes. Setting values between 0 and 1 allows you to override the amount of particles used.")]
		[Range(0,1)]
		public float resolution = 0.5f;												/**< modulates resolution of particle representation.*/

		[Range(0,3)]
		public uint smoothing = 1;						/**< Amount of smoothing applied to the particle representation.*/

		public GameObject startPrefab;
		public GameObject endPrefab;	

		[HideInInspector][SerializeField] protected bool closed = false;
		[HideInInspector][SerializeField] protected float interParticleDistance = 0;
		[HideInInspector][SerializeField] protected float restLength = 0;
		[HideInInspector][SerializeField] protected int usedParticles = 0;
		[HideInInspector][SerializeField] protected int totalParticles = 0;

		[HideInInspector] public GameObject startPrefabInstance;
		[HideInInspector] public GameObject endPrefabInstance;

		protected float curveLength = 0;
		protected int curveSections = 0;
		[HideInInspector][NonSerialized] public ObiList<ObiList<ObiCurveSection>> rawCurves = new ObiList<ObiList<ObiCurveSection>>(); 
		[HideInInspector][NonSerialized] public ObiList<ObiList<ObiCurveSection>> curves = new ObiList<ObiList<ObiCurveSection>>(); 

		public ObiTetherConstraints TetherConstraints{
			get{return GetConstraints(Oni.ConstraintType.Tether) as ObiTetherConstraints;}
		}
		public ObiPinConstraints PinConstraints{
			get{return GetConstraints(Oni.ConstraintType.Pin) as ObiPinConstraints;}
		}

		public virtual float InterparticleDistance{
			get{return interParticleDistance;}
		}

		public int TotalParticles{
			get{return totalParticles;}
		}

		public virtual int UsedParticles{
			get{return usedParticles;}
			set{usedParticles = value;}
		}

		public float RestLength{
			get{return restLength;}
			set{restLength = value;}
		}

		public float SmoothLength{
			get{return curveLength;}
		}

		public int SmoothSections{
			get{return curveSections;}
		}

		public bool Closed{
			get{return closed;}
		}
	     
		public virtual void OnValidate(){

			thickness = Mathf.Max(0.0001f,thickness);
			resolution = Mathf.Max(0.0001f,resolution);

	    }

		public override void OnSolverStepEnd(float deltaTime){	

			base.OnSolverStepEnd(deltaTime);

			if (isActiveAndEnabled){
	
				// breakable pin constraints:
				PinConstraints.BreakConstraints();
			}
		}

		public override void Start(){
			base.Start();
			GeneratePrefabInstances();
		}
		
		public override void OnDestroy(){
			base.OnDestroy();
			ClearPrefabInstances();
		}

		/**
		 * Generates new valid rest positions for the entire rope.
		 */
		public abstract void RegenerateRestPositions();

		/**
		 * Recalculates rest rope length.
		 */
		public void RecalculateRestLength(){

			restLength = 0;

			// Iterate trough all distance constraints in order:
			int constraintCount = GetStructuralConstraintCount();
			for (int i = 0; i < constraintCount; ++i)
				restLength += GetStructuralConstraintRestLength(i);
			
		}

		/**
		 * Returns actual rope length, including stretch.
		 */
		public float CalculateLength(){

			float actualLength = 0;
			int constraintCount = GetStructuralConstraintCount();
			for (int i = 0; i < constraintCount; ++i){
				int particle1 = -1, particle2 = -1;
				if (GetStructuralConstraintParticles(i,ref particle1,ref particle2)){
					actualLength += Vector3.Distance(GetParticlePosition(particle1),GetParticlePosition(particle2));
				}
			}
			return actualLength;
		}

		protected virtual void GeneratePrefabInstances(){

			ClearPrefabInstances();

			// create start/end prefabs
			if (startPrefabInstance == null && startPrefab != null){
				startPrefabInstance = GameObject.Instantiate(startPrefab);
				startPrefabInstance.hideFlags = HideFlags.HideAndDontSave;
			}
			if (endPrefabInstance == null && endPrefab != null){
				endPrefabInstance = GameObject.Instantiate(endPrefab);
				endPrefabInstance.hideFlags = HideFlags.HideAndDontSave;
			}
		}

		/**
		 * Destroys all prefab instances used as start/end caps and tear prefabs.
		 */
		protected virtual void ClearPrefabInstances(){

			GameObject.DestroyImmediate(startPrefabInstance);
			GameObject.DestroyImmediate(endPrefabInstance);

		}

		public virtual void UpdateTearPrefab(ObiCurveFrame frame, ref int tearCount, bool reverseLookDirection){
			return;
		}

		protected float CalculateCurveLength(ObiList<ObiCurveSection> curve){
			float length = 0;
			for (int i = 1; i < curve.Count; ++i){
				length += Vector3.Distance(curve[i].positionAndRadius,curve[i-1].positionAndRadius);
			}
			return length;
		}

		/**
		 * Returns the particle indices affected by a structural constraint
		 */
		public abstract bool GetStructuralConstraintParticles(int constraintIndex, ref int particleIndex1, ref int particleIndex2);
		/**
		 * Returns structural constraint rest length.
		 */
		public abstract float GetStructuralConstraintRestLength(int constraintIndex);
		/**
		 * Returns the amount of structural constraints in the rope.
		 */
		public abstract int GetStructuralConstraintCount();
		/**
		 * Returns the index of the structural constraint at a given normalized rope coordinate.
		 */
		public abstract int GetConstraintIndexAtNormalizedCoordinate(float coord);

		public abstract void TransportFrame(ObiCurveFrame frame, ObiCurveSection section, float sectionTwist);

		protected void AddCurve(int sections){

			if (sections > 1){

				if (rawCurves.Data[rawCurves.Count] == null){
					rawCurves.Data[rawCurves.Count] = new ObiList<ObiCurveSection>();
					curves.Data[curves.Count] = new ObiList<ObiCurveSection>();
				}

				rawCurves.Data[rawCurves.Count].SetCount(sections);

				rawCurves.SetCount(rawCurves.Count+1);
				curves.SetCount(curves.Count+1);
			}
		}

		/**
		 * Counts the amount of continuous sections in each chunk of rope.
		 */
		protected void CountContinuousSegments(){

			rawCurves.Clear();	

			int segmentCount = 0;
			int lastParticle = -1;

			// Iterate trough all distance constraints in order. If we find a discontinuity, reset segment count:
			int constraintCount = GetStructuralConstraintCount();
			for (int i = 0; i < constraintCount; ++i){

				int particle1 = -1, particle2 = -1;
				if (GetStructuralConstraintParticles(i,ref particle1,ref particle2)){
			
					// start new curve at discontinuities:
					if (particle1 != lastParticle && segmentCount > 0){
						
						// add a new curve with the correct amount of sections: 
						AddCurve(segmentCount+1);
						segmentCount = 0;
					}
	
					lastParticle = particle2;
					segmentCount++;
				}
			}

			// add the last curve:
			AddCurve(segmentCount+1);
		}

		/** 
		 * Generate a list of smooth curves using particles as control points. Will take into account cuts in the rope,
		 * generating one curve for each continuous piece of rope.
		 */
		public void SmoothCurvesFromParticles(){

			curves.Clear();

			curveSections = 0;
			curveLength = 0;

			// count amount of segments in each rope chunk:
			CountContinuousSegments();

			Matrix4x4 w2l = transform.worldToLocalMatrix;
			Quaternion matrixRotation = Quaternion.LookRotation(
							     		w2l.GetColumn(2),
							      		w2l.GetColumn(1));
			int firstSegment = 0;

			// generate curve for each rope chunk:
			for (int i = 0; i < rawCurves.Count; ++i){

				int segments = rawCurves[i].Count-1;

				// allocate memory for the curve:
				ObiList<ObiCurveSection> controlPoints = rawCurves[i];

				// get control points position:
				int lastParticle = -1;
				int particle1 = -1, particle2 = -1;
				for (int m = 0; m < segments; ++m){

					if (GetStructuralConstraintParticles(firstSegment + m,ref particle1,ref particle2)){

						if (m == 0) lastParticle = particle1;
			
						// Find next and previous vectors:
						Vector3 nextV =  GetParticlePosition(particle2) - GetParticlePosition(particle1);
						Vector3 prevV =  GetParticlePosition(particle1) - GetParticlePosition(lastParticle);

						Vector3 pos = w2l.MultiplyPoint3x4(GetParticlePosition(particle1));
						Quaternion orient = matrixRotation * Quaternion.SlerpUnclamped(GetParticleOrientation(lastParticle),GetParticleOrientation(particle1),0.5f);
						Vector3 tangent = w2l.MultiplyVector(prevV + nextV).normalized;
						Color color = (this.colors != null && particle1 < this.colors.Length) ? this.colors[particle1] : Color.white;

						controlPoints[m] = new ObiCurveSection(new Vector4(pos.x,pos.y,pos.z,principalRadii[particle1][0]),tangent,orient * Vector3.up,color);
			
						lastParticle = particle1;
					}
				}

				// last segment adds its second particle too:
				if (segments > 0){

					Vector3 pos = w2l.MultiplyPoint3x4(GetParticlePosition(particle2));
					Quaternion orient = matrixRotation * GetParticleOrientation(particle1);
					Vector3 tangent = w2l.MultiplyVector(GetParticlePosition(particle2) - GetParticlePosition(particle1)).normalized;
					Color color = (this.colors != null && particle2 < this.colors.Length) ? this.colors[particle2] : Color.white;

					controlPoints[segments] = new ObiCurveSection(new Vector4(pos.x,pos.y,pos.z,principalRadii[particle2][0]),tangent,orient * Vector3.up,color);
				}

				firstSegment += segments;

				// get smooth curve points:
				ObiCurveFrame.Chaikin(controlPoints,curves[i],smoothing);

				// count total curve sections and total curve length:
				curveSections += curves[i].Count-1;
				curveLength += CalculateCurveLength(curves[i]);
			}
	
		}

		public void PlaceObjectAtCurveFrame(ObiCurveFrame frame, GameObject obj, Space space, bool reverseLookDirection){
			if (space == Space.Self){
				Matrix4x4 l2w = transform.localToWorldMatrix;
				obj.transform.position = l2w.MultiplyPoint3x4(frame.position);
				if (frame.tangent != Vector3.zero)
					obj.transform.rotation = Quaternion.LookRotation(l2w.MultiplyVector(reverseLookDirection ? frame.tangent:-frame.tangent),
																 	 l2w.MultiplyVector(frame.normal));
			}else{
				obj.transform.position = frame.position;
				if (frame.tangent != Vector3.zero)
					obj.transform.rotation = Quaternion.LookRotation(reverseLookDirection ? frame.tangent:-frame.tangent,frame.normal);
			}
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

			//UpdateVisualRepresentation();

		}

		/**
		 * Automatically generates tether constraints for the cloth.
		 * Partitions fixed particles into "islands", then generates up to maxTethers constraints for each 
		 * particle, linking it to the closest point in each island.
		 */
		public override bool GenerateTethers(){
			
			if (!Initialized) return false;
	
			TetherConstraints.Clear();
			
			GenerateFixedTethers(2);
	        
	        return true;
	        
		}

		private void GenerateFixedTethers(int maxTethers){

			ObiTetherConstraintBatch tetherBatch = new ObiTetherConstraintBatch(true,false,MIN_YOUNG_MODULUS,MAX_YOUNG_MODULUS);
			TetherConstraints.AddBatch(tetherBatch);
			
			List<HashSet<int>> islands = new List<HashSet<int>>();
		
			// Partition fixed particles into islands:
			for (int i = 0; i < usedParticles; i++){

				if (invMasses[i] > 0 || !active[i]) continue;
				
				int assignedIsland = -1;
	
				// keep a list of islands to merge with ours:
				List<int> mergeableIslands = new List<int>();
					
				// See if any of our neighbors is part of an island:
				int prev = Mathf.Max(i-1,0);
				int next = Mathf.Min(i+1,usedParticles-1);
		
				for(int k = 0; k < islands.Count; ++k){

					if ((active[prev] && islands[k].Contains(prev)) || 
						(active[next] && islands[k].Contains(next))){

						// if we are not in an island yet, pick this one:
						if (assignedIsland < 0){
							assignedIsland = k;
                            islands[k].Add(i);
						}
						// if we already are in an island, we will merge this newfound island with ours:
						else if (assignedIsland != k && !mergeableIslands.Contains(k)){
							mergeableIslands.Add(k);
						}
					}
                }
				
				// merge islands with the assigned one:
				foreach(int merge in mergeableIslands){
					islands[assignedIsland].UnionWith(islands[merge]);
				}
	
				// remove merged islands:
				mergeableIslands.Sort();
				mergeableIslands.Reverse();
				foreach(int merge in mergeableIslands){
					islands.RemoveAt(merge);
				}
				
				// If no adjacent particle is in an island, create a new one:
				if (assignedIsland < 0){
					islands.Add(new HashSet<int>(){i});
				}
			}	
			
			// Generate tether constraints:
			for (int i = 0; i < usedParticles; ++i){
			
				if (invMasses[i] == 0) continue;
				
				List<KeyValuePair<float,int>> tethers = new List<KeyValuePair<float,int>>(islands.Count);
				
				// Find the closest particle in each island, and add it to tethers.
				foreach(HashSet<int> island in islands){
					int closest = -1;
					float minDistance = Mathf.Infinity;
					foreach (int j in island){

						// TODO: Use linear distance along the rope in a more efficient way. precalculate it on generation!
						int min = Mathf.Min(i,j);
						int max = Mathf.Max(i,j);
						float distance = 0;
						for (int k = min; k < max; ++k)
							distance += Vector3.Distance(positions[k],
														 positions[k+1]);

						if (distance < minDistance){
							minDistance = distance;
							closest = j;
						}
					}
					if (closest >= 0)
						tethers.Add(new KeyValuePair<float,int>(minDistance, closest));
				}
				
				// Sort tether indices by distance:
				tethers.Sort(
				delegate(KeyValuePair<float,int> x, KeyValuePair<float,int> y)
				{
					return x.Key.CompareTo(y.Key);
				}
				);
				
				// Create constraints for "maxTethers" closest anchor particles:
				for (int k = 0; k < Mathf.Min(maxTethers,tethers.Count); ++k){
					tetherBatch.AddConstraint(i,tethers[k].Value,tethers[k].Key,1,1);
				}
			}

			tetherBatch.Cook();
		}
		
	}
}



