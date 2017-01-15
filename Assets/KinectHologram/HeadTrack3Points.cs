using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HeadTrack3Points : MonoBehaviour {

	private enum State {
		Ready,
		LowerLeftCorner,
		LowerRightCorner,
		TopRightCorner,
		Done
	}

	public GameObject camera;

	private List<Vector3> points = new List<Vector3> ();

	// Use this for initialization
	void Start () {
	
	}


	private void UpdateHeadPos() {
		if (points.Count != 3)
			return;

		KinectManager manager = KinectManager.Instance;

		if (!manager || !manager.IsInitialized ())
			return;

		if (!manager.IsUserDetected ())
			return;



		long userId = manager.GetPrimaryUserID();
		var joint = KinectInterop.JointType.Head;
		if (!manager.IsJointTracked (userId, (int)joint)) {
			return;
		}


		Vector3 pos = manager.GetJointPosition(userId, (int)joint);

		Vector3 ux = (points [1] - points [0]).normalized;
		Vector3 uy = (points [2] - points [1]).normalized;
		float lx = (points [1] - points [0]).magnitude;
		float ly = (points [2] - points [1]).magnitude;
		Vector3 uz = Vector3.Cross (ux, uy);
		Vector3 center = (points [0] + points [2]) / 2;

		pos -= center;
		float x = Vector3.Dot (pos, ux) * 2 / lx;
		float y = Vector3.Dot (pos, uy) * 2 / ly;
		float z = -(Vector3.Dot (pos, uz) + 0.5f) * 2 / lx;

		camera.transform.localPosition = new Vector3 (x, y, z);
		Debug.Log ("POS: " + x + "," + y + "," + z);
	}

	// Update is called once per frame
	void Update () {

		UpdateHeadPos ();

		KinectManager manager = KinectManager.Instance;

		if (!manager || !manager.IsInitialized ())
			return;
		
		if (!manager.IsUserDetected ())
			return;


		var joint = KinectInterop.JointType.Head;
		long userId = manager.GetPrimaryUserID();
		if (!manager.IsJointTracked (userId, (int)joint)) {
			Debug.Log ("Not tracked");
			return;
		}

		Vector3 pos = manager.GetJointPosition(userId, (int)joint);

		if (Input.GetKeyDown(KeyCode.Space)) {
			if (points.Count < 3) {
				points.Add (pos);
				Debug.Log (pos);
			}
		}

	}
}
