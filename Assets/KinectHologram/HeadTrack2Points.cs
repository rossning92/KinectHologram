using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HeadTrack2Points : MonoBehaviour {

	public GameObject headJointNode;
	public float distanceFromScreen = 1.0f;

	private List<Vector3> points = new List<Vector3> ();
	private Dictionary<string, object> debugStr = new Dictionary<string, object> ();

	void Start() {
		
	}

	private void UpdateCameraPos() {
		if (points.Count != 2) {
			return;
		}

		Vector3 pos = GetJointPos ();
		if (pos == Vector3.zero)
			return;
		
		Vector3 ux = (points [1] - points [0]).normalized;
		float lx = (points [1] - points [0]).magnitude;

		Vector3 uy = new Vector3 (0, 1, 0);
		float ly = lx * 9 / 16; // screen ratio

		Vector3 uz = Vector3.Cross (ux, uy);


		Vector3 topRight = points [0] + ux * lx + uy * ly;
		Vector3 center = (points [0] + topRight) / 2;

		pos -= center;
		float x = Vector3.Dot (pos, ux) * 2 / lx;
		float y = Vector3.Dot (pos, uy) * 2 / ly;
		float z = -(Vector3.Dot (pos, uz) + distanceFromScreen) * 2 / lx;

		Vector3 cameraPos = new Vector3 (x, y, z);
		headJointNode.transform.localPosition = cameraPos;
		debugStr ["Camera Local Pos"] = cameraPos;
	}

	Vector3 GetJointPos() {
		KinectManager manager = KinectManager.Instance;
		if (!manager || !manager.IsInitialized ()) {
			debugStr["Status"] = "Kinect not initialized";
			return Vector3.zero;
		}

		if (!manager.IsUserDetected ()) {
			debugStr["Status"] = "Body not found";
			return Vector3.zero;
		}

		var joint = KinectInterop.JointType.Head;
		long userId = manager.GetPrimaryUserID();
		if (!manager.IsJointTracked (userId, (int)joint)) {
			debugStr["Status"] = "Joint not tracked";
			return Vector3.zero;
		}

		debugStr["Status"] = "Joint is tracking";
		return manager.GetJointPosition(userId, (int)joint);
	}

	void CalibrateScreenPos() {

		Vector3 pos = GetJointPos ();
		if (pos == Vector3.zero)
			return;

		if (Input.GetKeyDown(KeyCode.Space)) {
			if (points.Count < 2) {
				points.Add (pos);

				if (points.Count == 1) {
					debugStr ["Screen Lower Left"] = pos;
				} else if (points.Count == 2) {
					debugStr ["Screen Lower Right"] = pos;
				}
			}
		}
	}

	// Update is called once per frame
	void Update () {
		
		CalibrateScreenPos ();

		UpdateCameraPos ();

	}

	void OnGUI() {

		string str = "";
		foreach(var entry in debugStr)
		{
			str += entry.Key + ": " + entry.Value + "\n";
		}

		GUI.Label(new Rect(10, 10, 10000, 10000), str);
	}
}
