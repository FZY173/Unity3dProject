using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obi
{
	[RequireComponent(typeof(ObiRopeBase))]
	public class ObiRopeChainRenderer : ObiRopeRendererBase
	{	

		[HideInInspector] public List<GameObject> linkInstances;

		[SerializeProperty("RandomizeLinks")]
		[SerializeField] private bool randomizeLinks = false;

		public Vector3 linkScale = Vector3.one;		/**< Scale of chain links.*/
		public List<GameObject> linkPrefabs = new List<GameObject>();

		[Range(0,1)]
		public float twistAnchor = 0;			    /**< Normalized position of twisting origin along rope.*/

		public float sectionTwist = 0;				/**< Amount of twist applied to each section, in degrees.*/

		private ObiCurveFrame frame;

		protected override void Awake(){
			base.Awake();
			ClearChainLinkInstances();
		}

		public bool RandomizeLinks{
			get{return randomizeLinks;}
			set{
				if (value != randomizeLinks){
					randomizeLinks = value;
					CreateChainLinkInstances();
				}
			}
		}
		
		protected override void OnDisable(){

			base.OnDisable();

			ClearChainLinkInstances();
		}

		/**
		 * Destroys all chain link instances. Used when the chain must be re-created from scratch, and when the actor is disabled/destroyed.
		 */
		public void ClearChainLinkInstances(){

			if (linkInstances == null)
				return;

			for (int i = 0; i < linkInstances.Count; ++i){
				if (linkInstances[i] != null)
					GameObject.DestroyImmediate(linkInstances[i]); 
			}
			linkInstances.Clear();
		}

		public void CreateChainLinkInstances(){

			ClearChainLinkInstances();

			if (linkPrefabs.Count > 0){

				for (int i = 0; i < rope.TotalParticles; ++i){
	
					int index = randomizeLinks ? UnityEngine.Random.Range(0,linkPrefabs.Count) : i % linkPrefabs.Count;
	
					GameObject linkInstance = null;

					if (linkPrefabs[index] != null){
						linkInstance = GameObject.Instantiate(linkPrefabs[index]);
						linkInstance.transform.SetParent(rope.transform,false);
						linkInstance.hideFlags = HideFlags.HideAndDontSave;
						linkInstance.SetActive(false);
					}
	
					linkInstances.Add(linkInstance);
				}
			}
		}

		public override void UpdateRenderer(object sender, EventArgs e){

			// In case there are no link prefabs to instantiate:
			if (linkPrefabs.Count == 0)
				return;

			// Regenerate instances if needed:
			if (linkInstances == null || linkInstances.Count < rope.TotalParticles){
				CreateChainLinkInstances();
			}

			int constraintCount = rope.GetStructuralConstraintCount();

			// we will define and transport a reference frame along the curve using parallel transport method:
			if (frame == null) 			
				frame = new ObiCurveFrame();
			frame.Reset();
			frame.SetTwist(-sectionTwist * constraintCount * twistAnchor);

			int lastParticle = -1;
			int tearCount = 0;

			for (int i = 0; i < constraintCount; ++i){

				int particle1 = -1, particle2 = -1;
				rope.GetStructuralConstraintParticles(i,ref particle1, ref particle2);

				Vector3 pos = rope.GetParticlePosition(particle1);
				Vector3 nextPos = rope.GetParticlePosition(particle2);
				Vector3 linkVector = nextPos-pos;
				Vector3 tangent = linkVector.normalized;

				// update tear prefab at the first side of tear:
				if (i > 0 && particle1 != lastParticle){
					rope.UpdateTearPrefab(frame,ref tearCount,false);
					// reset frame at discontinuities:
					frame.Reset();
				}

				// update frame: TODO: allow mesh rendering offset from torsion (twist)
				rope.TransportFrame(frame,new ObiCurveSection(nextPos,tangent,rope.GetParticleOrientation(particle1) * Vector3.up,Color.white),sectionTwist);

				// update tear prefab at the other side of the tear:
				if (i > 0 && particle1 != lastParticle){
					frame.position = pos;
					rope.UpdateTearPrefab(frame,ref tearCount, false);
				}

				// update start/end prefabs:
				if (!rope.Closed){
					if (i == 0 && rope.startPrefabInstance != null)
						rope.PlaceObjectAtCurveFrame(frame,rope.startPrefabInstance,Space.World,false);
					else if (i == constraintCount-1 && rope.endPrefabInstance != null){
						frame.position = nextPos;
						rope.PlaceObjectAtCurveFrame(frame,rope.endPrefabInstance,Space.World,true);
					}
				}

				if (linkInstances[i] != null){
					linkInstances[i].SetActive(true);
					Transform linkTransform = linkInstances[i].transform;
					linkTransform.position = pos + linkVector * 0.5f;
					linkTransform.localScale = rope.thicknessFromParticles ? (rope.principalRadii[particle1][0]/rope.thickness) * linkScale : linkScale;
					linkTransform.rotation = Quaternion.LookRotation(tangent,frame.normal);
				}

				lastParticle = particle2;

			}		

			for (int i = constraintCount; i < linkInstances.Count; ++i){
				if (linkInstances[i] != null)
					linkInstances[i].SetActive(false);
			}
		}
	}
}

