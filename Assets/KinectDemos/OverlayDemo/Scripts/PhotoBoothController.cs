using UnityEngine;
using System.Collections;
using System.IO;

public class PhotoBoothController : MonoBehaviour, KinectGestures.GestureListenerInterface, InteractionListenerInterface
{
	[Tooltip("GUI-texture used to display the color camera feed on the scene background.")]
	public GUITexture backgroundImage;

	[Tooltip("Camera that will be used to render the background.")]
	public Camera backroundCamera;

	[Tooltip("Camera that will be used to overlay the 3D-objects over the background.")]
	public Camera foreroundCamera;

	[Tooltip("Index of the player, tracked by this component. 0 means the 1st player, 1 - the 2nd one, 2 - the 3rd one, etc.")]
	public int playerIndex = 0;

	[Tooltip("Reference to the head joint-overlayer component.")]
	public JointOverlayer headOverlayer;

	[Tooltip("Reference to the left hand joint-overlayer component.")]
	public JointOverlayer leftHandOverlayer;

	[Tooltip("Reference to the chest joint-overlayer component.")]
	public JointOverlayer chestOverlayer;

	[Tooltip("Array of sprite transforms that will be used for head overlays on each step.")]
	public Transform[] headMasks;

	[Tooltip("Array of sprite transforms that will be used for left hand overlays on each step.")]
	public Transform[] leftHandMasks;

	[Tooltip("Array of sprite transforms that will be used for chest overlays on each step.")]
	public Transform[] chestMasks;

	[Tooltip("Array of sprite transforms that will be used for displaying the countdown until image shot.")]
	public Transform[] countdown;

	[Tooltip("GUI-Text used to display information messages.")]
	public GUIText infoText;


	private int maskCount = 0;
	private int currentIndex = -1;
	private int prevIndex = -1;


	void Start () 
	{
		maskCount = 0;

		if (headMasks != null && headMasks.Length > maskCount)
			maskCount = headMasks.Length;

		if (leftHandMasks != null && leftHandMasks.Length > maskCount)
			maskCount = leftHandMasks.Length;

		if (chestMasks != null && chestMasks.Length > maskCount)
			maskCount = chestMasks.Length;
	}
	
	void Update () 
	{
		KinectManager manager = KinectManager.Instance;

		if (manager && manager.IsInitialized ()) 
		{
			if (backgroundImage && (backgroundImage.texture == null)) 
			{
				backgroundImage.texture = manager.GetUsersClrTex ();
			}
		}

		if (currentIndex != prevIndex) 
		{
			prevIndex = currentIndex;

			if (headOverlayer && headMasks != null) 
			{
				if (headOverlayer.overlayObject)
					headOverlayer.overlayObject.gameObject.SetActive (false);
				
				headOverlayer.overlayObject = currentIndex >= 0 && currentIndex < headMasks.Length ? headMasks [currentIndex] : null;
				headOverlayer.playerIndex = playerIndex;

				if (headOverlayer.overlayObject)
					headOverlayer.overlayObject.gameObject.SetActive (true);
			}

			if (leftHandOverlayer && leftHandMasks != null) 
			{
				if (leftHandOverlayer.overlayObject)
					leftHandOverlayer.overlayObject.gameObject.SetActive (false);

				leftHandOverlayer.overlayObject = currentIndex >= 0 && currentIndex < leftHandMasks.Length ? leftHandMasks [currentIndex] : null;
				leftHandOverlayer.playerIndex = playerIndex;

				if (leftHandOverlayer.overlayObject)
					leftHandOverlayer.overlayObject.gameObject.SetActive (true);
			}

			if (chestOverlayer && chestMasks != null) 
			{
				if (chestOverlayer.overlayObject)
					chestOverlayer.overlayObject.gameObject.SetActive (false);

				chestOverlayer.overlayObject = currentIndex >= 0 && currentIndex < chestMasks.Length ? chestMasks [currentIndex] : null;
				chestOverlayer.playerIndex = playerIndex;

				if (chestOverlayer.overlayObject)
					chestOverlayer.overlayObject.gameObject.SetActive (true);
			}
		}
	}


	// GestureListenerInterface

	public void UserDetected(long userId, int userIndex)
	{
		KinectManager manager = KinectManager.Instance;
		if(!manager || (userIndex != playerIndex))
			return;

		manager.DetectGesture(userId, KinectGestures.Gestures.SwipeLeft);
		manager.DetectGesture(userId, KinectGestures.Gestures.SwipeRight);

		currentIndex = 0;

		if (infoText) 
		{
			infoText.text = "Swipe left or right to change props. Make hand grip to take photo.";
		}
	}

	public void UserLost(long userId, int userIndex)
	{
		if(userIndex != playerIndex)
			return;

		currentIndex = -1;
	}

	public void GestureInProgress(long userId, int userIndex, KinectGestures.Gestures gesture, float progress, KinectInterop.JointType joint, Vector3 screenPos)
	{
		// nothing to do here
	}

	public bool GestureCompleted(long userId, int userIndex, KinectGestures.Gestures gesture, KinectInterop.JointType joint, Vector3 screenPos)
	{
		if(userIndex != playerIndex)
			return false;

		switch (gesture)
		{
		case KinectGestures.Gestures.SwipeLeft:
			currentIndex++;
			if (currentIndex >= maskCount)
				currentIndex = 0;
			break;

		case KinectGestures.Gestures.SwipeRight:
			currentIndex--;
			if (currentIndex < 0)
				currentIndex = maskCount - 1;
			break;
		}

		return true;
	}

	public bool GestureCancelled(long userId, int userIndex, KinectGestures.Gestures gesture, KinectInterop.JointType joint)
	{
		if(userIndex != playerIndex)
			return false;

		return true;
	}


	// InteractionListenerInterface

	public void HandGripDetected(long userId, int userIndex, bool isRightHand, bool isHandInteracting, Vector3 handScreenPos)
	{
		if (userIndex != playerIndex)
			return;

		if (isRightHand && handScreenPos.y >= 0.5f) 
		{
			if (infoText) 
			{
				infoText.text = "Hand grip detected.";
			}

			StartCoroutine(CountdownAndTakePicture());
		}
	}

	public void HandReleaseDetected(long userId, int userIndex, bool isRightHand, bool isHandInteracting, Vector3 handScreenPos)
	{
		if (userIndex != playerIndex)
			return;

		// nothing to do here
	}

	public bool HandClickDetected(long userId, int userIndex, bool isRightHand, Vector3 handScreenPos)
	{
		if (userIndex != playerIndex)
			return false;

		return true;
	}


	// counts from 3 and takes a picture
	private IEnumerator CountdownAndTakePicture()
	{
		if (countdown.Length > 0) 
		{
			for(int i = 0; i < countdown.Length; i++)
			{
				if (countdown [i])
					countdown [i].gameObject.SetActive(true);
				
				yield return new WaitForSeconds(1.0f);

				if (countdown [i])
					countdown [i].gameObject.SetActive(false);
			}
		}

		TakePicture();
		yield return null;
	}


	// saves the screen image as png picture
	private string TakePicture()
	{
		if (backroundCamera && foreroundCamera) 
		{
			int resWidth = Screen.width;
			int resHeight = Screen.height;

			Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false); //Create new texture
			RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);

			// hide the info-text, if any
			if (infoText) 
			{
				infoText.text = string.Empty;
			}

			// render background and foreground cameras
			backroundCamera.targetTexture = rt;
			backroundCamera.Render();

			foreroundCamera.targetTexture = rt;
			foreroundCamera.Render();

			// get the screenshot
			RenderTexture.active = rt;
			screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);

			// clean-up
			backroundCamera.targetTexture = null;
			foreroundCamera.targetTexture = null;

			RenderTexture.active = null;
			Destroy(rt);

			byte[] btScreenShot = screenShot.EncodeToJPG();
			Destroy(screenShot);

#if !UNITY_WSA
			// save the screenshot as jpeg file
			string sDirName = Application.dataPath + "/Screenshots";
			if (!Directory.Exists(sDirName))
				Directory.CreateDirectory (sDirName);
			
			string sFileName = sDirName + "/" + string.Format ("{0:F0}", Time.realtimeSinceStartup * 10f) + ".jpg";
			File.WriteAllBytes(sFileName, btScreenShot);

			if (infoText) 
			{
				infoText.text = "Saved to: " + sFileName;
			}
				
			// open file
			System.Diagnostics.Process.Start(sFileName);

			return sFileName;
#endif
		}

		return string.Empty;
	}

}
