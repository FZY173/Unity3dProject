using System;
using UnityEngine;
using UnityEditor;

namespace Obi
{
	public static class ObiEditorCurveDrawing
	{

		public static void DrawSplineGizmos(ObiCurve curve, int resolution, Color color, Color culledColor, Color orientationColor, bool drawOrientation = true){

			if (curve == null || curve.GetNumSpans() == 0) return;

			Matrix4x4 prevMatrix = Handles.matrix;
			UnityEngine.Rendering.CompareFunction oldZTest = Handles.zTest;
			Color oldColor = Handles.color;

			Handles.matrix = curve.transform.localToWorldMatrix;
			Handles.color = orientationColor;

	        // Draw the curve:
			int curveSegments = curve.GetNumSpans() * resolution;
			Vector3[] samples = new Vector3[curveSegments+1];

			for (int i = 0; i <= curveSegments; ++i){

				float mu = i/(float)curveSegments;
				samples[i] = curve.GetPositionAt(mu);

				if (drawOrientation){
					Vector3 right = Vector3.Cross(curve.GetFirstDerivativeAt(mu),curve.GetNormalAt(mu)).normalized;
					Vector3 direction = Vector3.Cross(right,curve.GetFirstDerivativeAt(mu)).normalized;
					Handles.DrawLine(samples[i], samples[i] + direction * 0.075f);
				}
			}

			Handles.color = color;
			Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
			Handles.DrawPolyLine(samples);

			Handles.color = culledColor;
			Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
			Handles.DrawPolyLine(samples);

			Handles.color = oldColor;
			Handles.matrix = prevMatrix; 
			Handles.zTest = oldZTest;
		}

		public static float ScreenPointToCurveMu(ObiCurve curve, Vector2 screenPoint, int samples = 30)
        {
			
			if (curve.controlPoints.Count >= curve.MinPoints){
	
				samples = Mathf.Max(1,samples);
				float step = 1/(float)samples;
	
				float closestMu = 0;
				float minDistance = float.MaxValue;

				Matrix4x4 l2w = curve.transform.localToWorldMatrix;
	
				for(int k = 0; k < curve.GetNumSpans(); ++k) {
	
					Vector3 _p = l2w.MultiplyPoint3x4(curve.controlPoints[k].position);
					Vector3 p = l2w.MultiplyPoint3x4(curve.controlPoints[k].GetOutTangent());
					Vector3 p_ = l2w.MultiplyPoint3x4(curve.controlPoints[(k+1) % curve.controlPoints.Count].GetInTangent());
					Vector3 p__ = l2w.MultiplyPoint3x4(curve.controlPoints[(k+1) % curve.controlPoints.Count].position);
	
					Vector2 lastPoint = HandleUtility.WorldToGUIPoint(curve.Evaluate3D(_p,p,p_,p__,0));
					for(int i = 1; i <= samples; ++i){

						Vector2 currentPoint = HandleUtility.WorldToGUIPoint(curve.Evaluate3D(_p,p,p_,p__,i*step));
	
						float mu;
						float distance = Vector2.SqrMagnitude((Vector2)ObiUtils.ProjectPointLine(screenPoint,lastPoint,currentPoint,out mu) - screenPoint);
	
						if (distance < minDistance){
							minDistance = distance;
							closestMu = (k + (i-1)*step + mu/samples) / (float)curve.GetNumSpans();
						}
						lastPoint = currentPoint;
					}
	
				}

				return closestMu;
	
			}else{
				Debug.LogWarning("Curve needs at least 2 control points to be defined.");
			}
			return 0;

        }

	}
}

