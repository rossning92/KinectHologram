using UnityEngine;
using System;

namespace Assets.HolographicDisplay
{
    /// <summary>
	/// Script that emulates a 3D holographic display based on the viewer position
	/// by Davi Loots (Twitter: @davloots), Start() & Update()-functions added by Rumen Filkov (Twitter: @roumenf)
    /// Usage:
    /// - Attach to a camera.
	/// - Update the HeadPosition each frame either in this or in an external script based on some form of headtracking
    /// - For best effect - and if available - use a stereoscopic display and calculate the head 
    ///   position twice by simply offsetting the HeadPosition .03 to the left and to the right for
    ///   each of the views.
    /// </summary>
    class SimpleHolographicCamera : MonoBehaviour
    {
		[Tooltip("Index of the player, tracked by this component. 0 means the 1st player, 1 - the 2nd one, 2 - the 3rd one, etc.")]
		public int playerIndex = 0;

		[Tooltip("How high above the ground is the center of the display, in meters.")]
		public float ScreenCenterY = 0.5f;

		[Tooltip("Phisical width of the display, in meters.")]
        public float ScreenWidth = 0.88f;

		[Tooltip("Phisical height of the display, in meters.")]
        public float ScreenHeight = 0.50f;

		[Tooltip("GUI-Text to display status messages.")]
		public GUIText statusText = null;

        private float left = -0.2F;
        private float right = 0.2F;
        private float top = 0.2F;
        private float bottom = -0.2F;

		private KinectManager kinectManager;
		private Vector3 screenCenterPos;

		private Vector3 headPosition;
		private bool headPosValid = false;


		void Start()
		{
			kinectManager = KinectManager.Instance;
			screenCenterPos = new Vector3 (0f, ScreenCenterY, 0f);
		}

		void Update()
		{
			headPosValid = false;

			if (kinectManager && kinectManager.IsInitialized()) 
			{
				long userId = kinectManager.GetUserIdByIndex(playerIndex);

				if (kinectManager.IsUserTracked(userId) && kinectManager.IsJointTracked(userId, (int)KinectInterop.JointType.Head)) 
				{
					Vector3 jointHeadPos = kinectManager.GetJointPosition (userId, (int)KinectInterop.JointType.Head);
					headPosition = jointHeadPos - screenCenterPos;
					headPosValid = true;

					if (statusText) 
					{
						string sStatusMsg = string.Format("Head position: {0}", jointHeadPos);
						statusText.text = sStatusMsg;
					}
				}
			}

		}


        /// <summary>
        /// Updates the projection matrix and camera position to get the correct anamorph perspective
        /// </summary>
        void LateUpdate()
        {
			if (headPosValid) 
			{
				Camera cam = GetComponent<Camera>();

				left = cam.nearClipPlane * (-(ScreenWidth / 2) - headPosition.x) / headPosition.z;
				right = cam.nearClipPlane * (ScreenWidth / 2 - headPosition.x) / headPosition.z;

				bottom = cam.nearClipPlane * (-(ScreenHeight / 2) - headPosition.y) / headPosition.z;
				top = cam.nearClipPlane * (ScreenHeight / 2 - headPosition.y) / headPosition.z;

				cam.transform.position = new Vector3(headPosition.x, headPosition.y, -headPosition.z);
				cam.transform.LookAt(new Vector3(headPosition.x, headPosition.y, 0));

				Matrix4x4 m = PerspectiveOffCenter(left, right, bottom, top, cam.nearClipPlane, cam.farClipPlane);
				cam.projectionMatrix = m;
			}
        }

        /// <summary>
        /// Calculates the camera projection matrix
        /// </summary>
        /// <returns>The off center.</returns>
        /// <param name="left">Left.</param>
        /// <param name="right">Right.</param>
        /// <param name="bottom">Bottom.</param>
        /// <param name="top">Top.</param>
        /// <param name="near">Near.</param>
        /// <param name="far">Far.</param>
        static Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom, float top, float near, float far)
        {
            float x = 2.0F * near / (right - left);
            float y = 2.0F * near / (top - bottom);
            float a = (right + left) / (right - left);
            float b = (top + bottom) / (top - bottom);
            float c = -(far + near) / (far - near);
            float d = -(2.0F * far * near) / (far - near);
            float e = -1.0F;

            Matrix4x4 m = new Matrix4x4();
            m[0, 0] = x;
            m[0, 1] = 0;
            m[0, 2] = a;
            m[0, 3] = 0;
            m[1, 0] = 0;
            m[1, 1] = y;
            m[1, 2] = b;
            m[1, 3] = 0;
            m[2, 0] = 0;
            m[2, 1] = 0;
            m[2, 2] = c;
            m[2, 3] = d;
            m[3, 0] = 0;
            m[3, 1] = 0;
            m[3, 2] = e;
            m[3, 3] = 0;

            return m;
        }
    }
}
