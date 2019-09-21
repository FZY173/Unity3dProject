using UnityEngine;
using System.Collections;

namespace Obi
{
	[RequireComponent(typeof(ObiRope))]
	[RequireComponent(typeof(ObiCurve))]
	public class ObiRopeHelper : MonoBehaviour {

		public ObiSolver solver;
		public ObiRopeSection section;
		public Material material;
		public Transform start;
		public Transform end;
		
		private ObiRope rope;
		private ObiCurve path;
	
		void Start () {
	
			// Get all needed components and interconnect them:
			rope = GetComponent<ObiRope>();
			path = GetComponent<ObiCurve>();
			rope.Solver = solver;
			rope.ropePath = path;	
			//rope.section = section;
			GetComponent<MeshRenderer>().material = material;
			
			// Calculate rope start/end and direction in local space:
			Vector3 localStart = transform.InverseTransformPoint(start.position);
			Vector3 localEnd = transform.InverseTransformPoint(end.position);
			Vector3 direction = (localEnd-localStart).normalized;

			// Generate rope path:
			path.controlPoints.Clear();
			path.controlPoints.Add(new ObiCurve.ControlPoint(localStart-direction,Vector3.up));
			path.controlPoints.Add(new ObiCurve.ControlPoint(localStart,Vector3.up));
			path.controlPoints.Add(new ObiCurve.ControlPoint(localEnd,Vector3.up));
			path.controlPoints.Add(new ObiCurve.ControlPoint(localEnd+direction,Vector3.up));

			// Setup the simulation:
			StartCoroutine(Setup());
		}

		IEnumerator Setup(){

			// Generate particles and add them to solver:
			yield return StartCoroutine(rope.GeneratePhysicRepresentationForMesh());
			rope.AddToSolver(null);

			// Fix first and last particle in place:
			rope.Solver.invMasses[rope.particleIndices[0]] = rope.invMasses[0] = 0;
			rope.Solver.invMasses[rope.particleIndices[rope.UsedParticles-1]] = rope.invMasses[rope.UsedParticles-1] = 0;
		}
		
	}
}
