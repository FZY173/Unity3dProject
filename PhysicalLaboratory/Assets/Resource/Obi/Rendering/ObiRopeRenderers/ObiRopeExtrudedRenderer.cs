using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obi
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(MeshRenderer))]
	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(ObiRopeBase))]
	public class ObiRopeExtrudedRenderer : ObiRopeRendererBase
	{
		private List<Vector3> vertices = new List<Vector3>();
		private List<Vector3> normals = new List<Vector3>();
		private List<Vector4> tangents = new List<Vector4>();
		private List<Vector2> uvs = new List<Vector2>();
		private List<Color> vertColors = new List<Color>();
		private List<int> tris = new List<int>();

		private ObiCurveFrame frame;

		[HideInInspector][NonSerialized] public Mesh extrudedMesh;

		[Range(0,1)]
		public float uvAnchor = 0;					/**< Normalized position of texture coordinate origin along rope.*/

		public Vector2 uvScale = Vector2.one;		/**< Scaling of uvs along rope.*/

		public bool normalizeV = true;

		public ObiRopeSection section = null;		/**< Section asset to be extruded along the rope.*/

		public float sectionTwist = 0;				/**< Amount of twist applied to each section, in degrees.*/

		public float sectionThicknessScale = 0.8f;	/**< Scales section thickness.*/

		protected override void OnEnable(){

			CreateMeshIfNeeded();

			base.OnEnable();

		}

		protected override void OnDisable(){

			base.OnDisable();

			GameObject.DestroyImmediate(extrudedMesh);
		}

		private void CreateMeshIfNeeded(){
			if (extrudedMesh == null){ 
				extrudedMesh = new Mesh();
				extrudedMesh.name = "extrudedMesh";
				extrudedMesh.MarkDynamic();
				GetComponent<MeshFilter>().mesh = extrudedMesh;
			}
		}

		public override void UpdateRenderer(object sender, EventArgs e){ 

			if (section == null || extrudedMesh == null) 
				return;

			rope.SmoothCurvesFromParticles();

			CreateMeshIfNeeded(); 
			ClearMeshData();

			float actualToRestLengthRatio = rope.SmoothLength/rope.RestLength;

			int sectionSegments = section.Segments;
			int verticesPerSection = sectionSegments + 1; 				// the last vertex in each section must be duplicated, due to uv wraparound.

			float vCoord = -uvScale.y * rope.RestLength * uvAnchor;	// v texture coordinate.
			int sectionIndex = 0;
			int tearCount = 0;

			// we will define and transport a reference frame along the curve using parallel transport method:
			if (frame == null) 			
				frame = new ObiCurveFrame();
			frame.Reset();
			frame.SetTwist(-sectionTwist * rope.SmoothSections * uvAnchor);

			// for closed curves, last frame of the last curve must be equal to first frame of first curve.
			Vector3 firstTangent = Vector3.forward;

			Vector4 texTangent = Vector4.zero;
			Vector2 uv = Vector2.zero;

			for (int c = 0; c < rope.curves.Count; ++c){
				
				ObiList<ObiCurveSection> curve = rope.curves[c];

				// Reinitialize frame for each curve.
				frame.Reset();

				for (int i = 0; i < curve.Count; ++i){
	
					// Calculate previous and next curve indices:
					//int nextIndex = Mathf.Min(i+1,curve.Count-1);
					int prevIndex = Mathf.Max(i-1,0);
	
					// Calculate current tangent as the vector between previous and next curve points:
					//Vector3 nextV;

					// The next tangent of the last segment of the last curve in a closed rope, is the first tangent again:
					/*if (rope.Closed && c == rope.curves.Count-1 && i == curve.Count-1 )
						nextV = firstTangent;
					else 
						nextV = curve[nextIndex].positionAndRadius - curve[i].positionAndRadius;*/

					//Vector3 prevV = curve[i].positionAndRadius - curve[prevIndex].positionAndRadius;
					//Vector3 tangent = nextV + prevV;

					// update frame:
					rope.TransportFrame(frame,curve[i],sectionTwist);//curve[i].positionAndRadius,tangent,rope.sectionTwist);

					// update tear prefabs (first segment of not first curve, last segment of not last curve)
					if (c > 0 && i == 0)
						rope.UpdateTearPrefab(frame,ref tearCount,false);
					if (c < rope.curves.Count-1 && i == curve.Count-1)
						rope.UpdateTearPrefab(frame,ref tearCount,true);

					// update start/end prefabs:
					if (c == 0 && i == 0){

						// store first tangent of the first curve (for closed ropes):
						firstTangent = frame.tangent;

						if (rope.startPrefabInstance != null && !rope.Closed)
							rope.PlaceObjectAtCurveFrame(frame,rope.startPrefabInstance, Space.Self, false);

					}else if (c == rope.curves.Count-1 && i == curve.Count-1 && rope.endPrefabInstance != null && !rope.Closed){
							rope.PlaceObjectAtCurveFrame(frame,rope.endPrefabInstance,Space.Self, true);
					}
		
					// advance v texcoord:
					vCoord += uvScale.y * (Vector3.Distance(curve[i].positionAndRadius,curve[prevIndex].positionAndRadius) / 
										       (normalizeV ? rope.SmoothLength : actualToRestLengthRatio));
	
					// calculate section thickness (either constant, or particle radius based):
					float sectionThickness = (rope.thicknessFromParticles ? curve[i].positionAndRadius.w : rope.thickness) * sectionThicknessScale;

					// Loop around each segment:
					for (int j = 0; j <= sectionSegments; ++j){
	
						vertices.Add(frame.position + (section.vertices[j].x*frame.normal + section.vertices[j].y*frame.binormal) * sectionThickness);
						normals.Add(vertices[vertices.Count-1] - frame.position);
						texTangent = Vector3.Cross(normals[normals.Count-1],frame.tangent);
						texTangent.w = -1;
						tangents.Add(texTangent);

						vertColors.Add(curve[i].color);
						uv.Set((j/(float)sectionSegments)*uvScale.x,vCoord);
						uvs.Add(uv);
	
						if (j < sectionSegments && i < curve.Count-1){

							tris.Add(sectionIndex*verticesPerSection + j); 			
							tris.Add((sectionIndex+1)*verticesPerSection + j); 	
							tris.Add(sectionIndex*verticesPerSection + (j+1)); 			
	
							tris.Add(sectionIndex*verticesPerSection + (j+1)); 		
							tris.Add((sectionIndex+1)*verticesPerSection + j); 	
							tris.Add((sectionIndex+1)*verticesPerSection + (j+1)); 		

						}
					}

					sectionIndex++;
				}

			}

			CommitMeshData();

		}

		private void ClearMeshData(){
			extrudedMesh.Clear();
			vertices.Clear();
			normals.Clear();
			tangents.Clear(); 
			uvs.Clear();
			vertColors.Clear();
			tris.Clear();
		}

		private void CommitMeshData(){
			extrudedMesh.SetVertices(vertices);
			extrudedMesh.SetNormals(normals);
			extrudedMesh.SetTangents(tangents);
			extrudedMesh.SetColors(vertColors);
			extrudedMesh.SetUVs(0,uvs);
			extrudedMesh.SetTriangles(tris,0,true);
		}
	}
}

