using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	[CustomEditor(typeof(ObiRopeExtrudedRenderer)), CanEditMultipleObjects] 
	public class  ObiRopeExtrudedRendererEditor : Editor
	{
		
		ObiRopeExtrudedRenderer renderer;
		
		public void OnEnable(){
			renderer = (ObiRopeExtrudedRenderer)target;
		}
		
		public override void OnInspectorGUI() {
			
			serializedObject.UpdateIfRequiredOrScript();
			
			Editor.DrawPropertiesExcluding(serializedObject,"m_Script");
			
			// Apply changes to the serializedProperty
			if (GUI.changed){
				
				serializedObject.ApplyModifiedProperties();
				
				renderer.UpdateRenderer(renderer,null);
				
			}
			
		}
		
	}
}

