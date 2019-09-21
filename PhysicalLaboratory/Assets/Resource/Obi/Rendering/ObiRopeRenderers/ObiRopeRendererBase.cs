using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obi
{
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	public abstract class ObiRopeRendererBase : MonoBehaviour
	{

		protected ObiRopeBase rope;
		protected bool subscribed = false;

		protected virtual void Awake(){
			rope = GetComponent<ObiRopeBase>();
			rope.OnInitialized += UpdateRenderer;
			rope.OnAddedToSolver += Subscribe;
			rope.OnRemovedFromSolver += Unsubscribe;
		}			

		private void Subscribe(object sender, EventArgs e){

			UpdateRenderer(sender,e);

			if (!subscribed && rope.Solver != null){
				subscribed = true;
				rope.Solver.OnFrameEnd += UpdateRenderer;
			}
		}
	
		private void Unsubscribe(object sender, EventArgs e){

			if (subscribed && rope.Solver != null){
				subscribed = false;
				rope.Solver.OnFrameEnd -= UpdateRenderer;
			}
		}

		protected virtual void OnEnable(){
			Subscribe(null,EventArgs.Empty); 
		}

		protected virtual void OnDisable(){
			Unsubscribe(null,EventArgs.Empty); 
		}

		protected void OnDestroy(){
			rope.OnInitialized -= UpdateRenderer;
			rope.OnAddedToSolver -= Subscribe;
			rope.OnRemovedFromSolver -= Unsubscribe;
		}

		public abstract void UpdateRenderer(object sender, EventArgs e);

	}
}

