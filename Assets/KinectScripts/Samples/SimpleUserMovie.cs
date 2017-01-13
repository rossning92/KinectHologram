using UnityEngine;
using System;

class SimpleUserMovie : MonoBehaviour
{
	[Tooltip("Index of the player, tracked by this component. 0 means the 1st player, 1 - the 2nd one, 2 - the 3rd one, etc.")]
	public int playerIndex = 0;

	[Tooltip("How far left or right from the camera may be the user, in meters.")]
	public float limitLeftRight = 1.2f;

	[Tooltip("Number of frames in the movie.")]
    public int numberOfFrames = 100;

	[Tooltip("Current frame number.")]
	public int currentFrame = 0;

	[Tooltip("GUI-Text to display status messages.")]
	public GUIText statusText = null;


	private KinectManager kinectManager;


	void Start()
	{
		kinectManager = KinectManager.Instance;
	}

	void Update()
	{
		if (kinectManager && kinectManager.IsInitialized()) 
		{
			long userId = kinectManager.GetUserIdByIndex(playerIndex);

			if (kinectManager.IsUserTracked(userId) && kinectManager.IsJointTracked(userId, (int)KinectInterop.JointType.SpineBase)) 
			{
				Vector3 userPos = kinectManager.GetJointPosition (userId, (int)KinectInterop.JointType.SpineBase);

				if (userPos.x >= -limitLeftRight && userPos.x <= limitLeftRight) 
				{
					// calculate the relative position in the movie
					float relPos = (userPos.x + limitLeftRight) / (2f * limitLeftRight);
					currentFrame = Mathf.RoundToInt(relPos * (numberOfFrames - 1));

					if (statusText) 
					{
						statusText.text = string.Format("X-Pos: {0:F2}, RelPos: {1:F3}, Frame: {2}", userPos.x, relPos, currentFrame);
					}
				}
			}

			// add here code to display the frame with 'currentFrame' number
			// ...

		}
	}

}
