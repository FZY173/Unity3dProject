using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obi;

[ExecuteInEditMode]
public class SpiralCurve : MonoBehaviour {

	public float radius = 0.25f;
	public float radialStep = 0.8f;
	public float heightStep = 0.04f;
	public float points = 30;

	void Awake () {

		ObiCurve path = GetComponent<ObiCurve>();

		path.controlPoints.Clear();
		float ang = 0;
		float height = 0;
		
		for (int i = 0; i < points; ++i)
		{
			Vector3 point = new Vector3(Mathf.Cos(ang)*radius,height,Mathf.Sin(ang)*radius);

			// optimal handle length for circle approximation: 4/3 tan(pi/(2n))
			Vector3 tangent = new Vector3(-point.z,heightStep,point.x).normalized * (4.0f/3.0f)*Mathf.Tan(radialStep/4.0f) * radius;

			path.controlPoints.Add(new ObiCurve.ControlPoint(point,Vector3.up,-tangent,tangent,ObiCurve.ControlPoint.BezierCPMode.Mirrored));
		    ang += radialStep;
		    height += heightStep;
		}

	

	}

}
