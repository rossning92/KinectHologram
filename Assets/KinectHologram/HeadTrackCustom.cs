using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;

public class HeadTrackCustom : MonoBehaviour
{
    public KinectInterop.JointType joint = KinectInterop.JointType.Head;
    public float OffsetX;
    public float OffsetY;
    public float OffsetZ;
    public float MultiplyX = 1f;
    public float MultiplyY = 1f;
    public float MultiplyZ = 1f;
	public GameObject headJointNode;

	private List<string> debugStr = new List<string>();

	void OnGUI()
	{
		int SPACE_Y = 20;
		int y = 0;
		foreach (string s in debugStr) {
			GUI.Label(new Rect(0, y, Screen.width, SPACE_Y), s);
			y += SPACE_Y;
		}
	}

    void Update()
    {
		debugStr.Clear ();

        KinectManager manager = KinectManager.Instance;

        if (manager && manager.IsInitialized())
        {
            if (manager.IsUserDetected())
            {
                long userId = manager.GetPrimaryUserID();

                if (manager.IsJointTracked(userId, (int)joint))
                {
                    
                    Vector3 jointPos = manager.GetJointPosition(userId, (int)joint);
                    float NewOffsetX = jointPos.x * MultiplyX + OffsetX;
                    float NewOffsetY = jointPos.y * MultiplyY + OffsetY;
                    float NewOffsetZ = jointPos.z * MultiplyZ + OffsetZ;
                    //float NewOffsetZ = OffsetZ;  // no tracking for Z

					debugStr.Add (string.Format ("HeadPosition: " + jointPos));
                    
					jointPos = new Vector3 (-NewOffsetX, NewOffsetY, NewOffsetZ);
					headJointNode.transform.position = GetComponent<Transform> ().TransformPoint (jointPos);
                    
					//transform.position = new Vector3(NewOffsetX, NewOffsetY, transform.position.z);
                }
            }
        }
    }
}
