using System;
using UnityEngine;

namespace Obi
{
	public struct ObiCurveSection{
		public Vector4 positionAndRadius;
		public Vector3 tangent;
		public Vector3 normal;
		public Vector4 color;

		public ObiCurveSection(Vector4 positionAndRadius, Vector3 tangent, Vector3 normal, Vector4 color){
			this.positionAndRadius = positionAndRadius;
			this.normal = normal;
			this.tangent = tangent;
			this.color = color;
		}

		public static ObiCurveSection operator +(ObiCurveSection c1, ObiCurveSection c2) 
	    {
			return new ObiCurveSection(c1.positionAndRadius + c2.positionAndRadius,c1.tangent + c2.tangent,c1.normal + c2.normal,c1.color + c2.color);
	    }

		public static ObiCurveSection operator *(float f,ObiCurveSection c) 
	    {
			return new ObiCurveSection(c.positionAndRadius * f, c.tangent * f, c.normal * f, c.color * f);
	    }
	}
}

