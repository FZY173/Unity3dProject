using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi
{
	
	/**
	 * Rod made of oriented Obi particles. No mesh or topology is needed to generate a physic representation from,
	 * since the mesh is generated procedurally.
	 */
	[ExecuteInEditMode]
	[AddComponentMenu("Physics/Obi/Obi Rod")]
	[RequireComponent(typeof (MeshRenderer))]
	[RequireComponent(typeof (MeshFilter))]
	[RequireComponent(typeof (ObiStretchShearConstraints))]
	[RequireComponent(typeof (ObiBendTwistConstraints))]
	[RequireComponent(typeof (ObiChainConstraints))]
	[DisallowMultipleComponent]
	public class ObiRod : ObiRopeBase
	{
		public const float DEFAULT_PARTICLE_ROTATIONAL_MASS = 0.005f;
		public new const float MAX_YOUNG_MODULUS = 25000000000;
		public new const float MIN_YOUNG_MODULUS = 10; 

		public bool keepInitialShape = true;

		public ObiBendTwistConstraints BendTwistConstraints{
			get{return GetConstraints(Oni.ConstraintType.BendTwist) as ObiBendTwistConstraints;}
		}
		public ObiStretchShearConstraints StretchShearConstraints{
			get{return GetConstraints(Oni.ConstraintType.StretchShear) as ObiStretchShearConstraints;}
		}
		public ObiChainConstraints ChainConstraints{
			get{return GetConstraints(Oni.ConstraintType.Chain) as ObiChainConstraints;}
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
			totalParticles = usedParticles;

			active = new bool[totalParticles];
			positions = new Vector3[totalParticles];
			orientations = new Quaternion[totalParticles];
			velocities = new Vector3[totalParticles];
			angularVelocities = new Vector3[totalParticles];
			invMasses  = new float[totalParticles];
			invRotationalMasses = new float[totalParticles];
			principalRadii = new Vector3[totalParticles];
			phases = new int[totalParticles];
			restPositions = new Vector4[totalParticles];
			restOrientations = new Quaternion[totalParticles];
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
				invRotationalMasses[i] = 1.0f/DEFAULT_PARTICLE_ROTATIONAL_MASS;
				float mu = ropePath.GetMuAtLenght(interParticleDistance*i);
				positions[i] = transform.InverseTransformPoint(ropePath.transform.TransformPoint(ropePath.GetPositionAt(mu)));
				principalRadii[i] = Vector3.one * radius;
				phases[i] = Oni.MakePhase(1,selfCollisions?Oni.ParticlePhase.SelfCollide:0);
				colors[i] = Color.white;

				if (i % 100 == 0)
					yield return new CoroutineJob.ProgressInfo("ObiRod: generating particles...",i/(float)usedParticles);

			}

			StretchShearConstraints.Clear();
			ObiStretchShearConstraintBatch stretchBatch = new ObiStretchShearConstraintBatch(false,false,MIN_YOUNG_MODULUS,MAX_YOUNG_MODULUS);
			StretchShearConstraints.AddBatch(stretchBatch);

			// rotation minimizing frame:
			ObiCurveFrame frame = new ObiCurveFrame();
			frame.Reset();

			for (int i = 0; i < numSegments; i++){

				int next = (i+1) % (ropePath.closed ? usedParticles:usedParticles+1);

				float mu = ropePath.GetMuAtLenght(interParticleDistance*i);
				Vector3 normal = transform.InverseTransformVector(ropePath.transform.TransformVector(ropePath.GetNormalAt(mu)));

				frame.Transport(positions[i],(positions[next] - positions[i]).normalized,0);

				orientations[i] = Quaternion.LookRotation(frame.tangent,normal);
				restOrientations[i] = orientations[i];

				// Also set the orientation of the next particle. If it is not the last one, we will overwrite it.
				// This makes sure that open rods provide an orientation for their last particle (or rather, a phantom segment past the last particle).

				orientations[next] = orientations[i];
				restOrientations[next] = orientations[i];

				stretchBatch.AddConstraint(i,next,interParticleDistance,Quaternion.identity,Vector3.one);	

				if (i % 500 == 0)
					yield return new CoroutineJob.ProgressInfo("ObiRod: generating structural constraints...",i/(float)numSegments);

			}

			BendTwistConstraints.Clear();
			ObiBendTwistConstraintBatch twistBatch = new ObiBendTwistConstraintBatch(false,false,MIN_YOUNG_MODULUS,MAX_YOUNG_MODULUS);
			BendTwistConstraints.AddBatch(twistBatch);

			// the last bend constraint couples the last segment and a phantom segment past the last particle.
			for (int i = 0; i < numSegments; i++){ 

				int next = (i+1) % (ropePath.closed ? usedParticles:usedParticles+1);

				Quaternion darboux = keepInitialShape ? ObiUtils.RestDarboux(orientations[i],orientations[next]) : Quaternion.identity;	
				twistBatch.AddConstraint(i,next,darboux,Vector3.one);

				if (i % 500 == 0)
					yield return new CoroutineJob.ProgressInfo("ObiRod: generating structural constraints...",i/(float)numSegments);

			}

			ChainConstraints.Clear();
			ObiChainConstraintBatch chainBatch = new ObiChainConstraintBatch(false,false,MIN_YOUNG_MODULUS,MAX_YOUNG_MODULUS);
			ChainConstraints.AddBatch(chainBatch);

			int[] indices = new int[usedParticles + (closed ? 1:0)];

			for (int i = 0; i < usedParticles; ++i)
				indices[i] = i;

			// Add the first particle as the last index of the chain, if closed.
			if (closed)
				indices[usedParticles] = 0;
			
			chainBatch.AddConstraint(indices,interParticleDistance,1,1);

			
			// Initialize tether constraints:
			TetherConstraints.Clear();

			// Initialize pin constraints:
			PinConstraints.Clear();
			ObiPinConstraintBatch pinBatch = new ObiPinConstraintBatch(false,false,MIN_YOUNG_MODULUS,MAX_YOUNG_MODULUS);
			PinConstraints.AddBatch(pinBatch);

			initializing = false;
			initialized = true;

			RegenerateRestPositions();

		}

		/**
		 * Generates new valid rest positions for the entire rope.
		 */
		public override void RegenerateRestPositions(){

			ObiStretchShearConstraintBatch distanceBatch = StretchShearConstraints.GetFirstBatch();		

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

		/**
		 * Returns the particle indices affected by a structural constraint
		 */
		public override bool GetStructuralConstraintParticles(int constraintIndex, ref int particleIndex1, ref int particleIndex2){
		
			ObiStretchShearConstraintBatch distanceBatch = StretchShearConstraints.GetFirstBatch();
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
			ObiStretchShearConstraintBatch distanceBatch = StretchShearConstraints.GetFirstBatch();
			if (distanceBatch != null && constraintIndex >= 0 && constraintIndex < distanceBatch.ConstraintCount)
				return distanceBatch.restLengths[constraintIndex];
			return 0;
		}

		/**
		 * Returns the amount of structural constraints in the rope.
		 */
		public override int GetStructuralConstraintCount(){
		
			ObiStretchShearConstraintBatch distanceBatch = StretchShearConstraints.GetFirstBatch();
			return distanceBatch != null ? distanceBatch.ConstraintCount:0;
		}

		/**
		 * Returns the index of the structural constraint at a given normalized rope coordinate.
		 */
		public override int GetConstraintIndexAtNormalizedCoordinate(float coord){

			ObiStretchShearConstraintBatch distanceBatch = StretchShearConstraints.GetFirstBatch();	
			if (distanceBatch != null){
				float mu = coord * distanceBatch.ConstraintCount;
				return Mathf.Clamp(Mathf.FloorToInt(mu),0,distanceBatch.ConstraintCount-1);
			}
			return -1;
		}

		public override void TransportFrame(ObiCurveFrame frame, ObiCurveSection section, float sectionTwist){
			if (frame != null)
				frame.Set(section);
		}
		
		/**
 		* Resets mesh to its original state.
 		*/
		public override void ResetActor(){
	
			PushDataToSolver(ParticleData.POSITIONS | ParticleData.VELOCITIES | ParticleData.ANGULAR_VELOCITIES);
			
			if (particleIndices != null){
				for(int i = 0; i < particleIndices.Length; ++i){
					solver.renderablePositions[particleIndices[i]] = positions[i];
				}
			}
		}
		
	}
}



