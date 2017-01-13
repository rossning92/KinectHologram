using UnityEngine;
using System.Collections;
using System.IO;


public class ModelSelector : MonoBehaviour 
{
	[Tooltip("Index of the player, tracked by this component. 0 means the 1st player, 1 - the 2nd one, 2 - the 3rd one, etc.")]
	public int playerIndex = 0;

	[Tooltip("The model category. Used for model discovery and title of the model-grid window.")]
	public string modelCategory = "Clothing";

	[Tooltip("Total number of the available clothing models.")]
	public int numberOfModels = 3;

	[Tooltip("Screen x-position of the model selection window. Negative values are considered relative to the screen width.")]
	public int windowScreenX = -160;

	[Tooltip("Makes the initial model position relative to this camera, to be equal to the player's position, relative to the sensor.")]
	public Camera modelRelativeToCamera = null;

	[Tooltip("Camera that will be used to overlay the model over the background.")]
	public Camera foregroundCamera;

	[Tooltip("Whether to keep the selected model, when the model category gets changed.")]
	public bool keepSelectedModel = true;

	[Tooltip("Whether the scale is updated continuously or just after the calibration pose.")]
	public bool continuousScaling = true;

	[Tooltip("Full body scale factor (incl. height, arms and legs) that might be used for fine tuning of body-scale.")]
	[Range(0.5f, 1.5f)]
	public float bodyScaleFactor = 1.0f;

	[Tooltip("Body width scale factor that might be used for fine tuning of body-scale.")]
	[Range(0.5f, 1.5f)]
	public float bodyWidthFactor = 1.0f;

	[Tooltip("Additional scale factor for arms that might be used for fine tuning of arm-scale.")]
	[Range(0.5f, 1.5f)]
	public float armScaleFactor = 1.0f;

	[Tooltip("Additional scale factor for legs that might be used for fine tuning of leg-scale.")]
	[Range(0.5f, 1.5f)]
	public float legScaleFactor = 1.0f;

	[Tooltip("Vertical offset of the avatar with respect to the position of user's spine-base.")]
	[Range(-0.5f, 0.5f)]
	public float verticalOffset = 0f;

	[Tooltip("Forward (Z) offset of the avatar with respect to the position of user's spine-base.")]
	[Range(-0.5f, 0.5f)]
	public float forwardOffset = 0f;

	[Tooltip("Gender limitation for users in this model selector.")]
	public UserGender modelGender = UserGender.Unisex;

	[Tooltip("Minimum age for users of this model selector.")]
	public float minimumAge = 0;

	[Tooltip("Maximum age for users of this model selector.")]
	public float maximumAge = 1000;


	[HideInInspector]
	public bool activeSelector = false;

//	[Tooltip("GUI-Text to display the avatar-scaler debug messages.")]
//	public GUIText debugText;


	private Rect menuWindowRectangle;
	private string[] modelNames;
	private Texture2D[] modelThumbs;

	private Vector2 scroll;
	private int selected;
	private int prevSelected = -1;

	private GameObject selModel;

	private float curScaleFactor = 0f;
	private float curModelOffset = 0f;


	/// <summary>
	/// Sets the model selector to be active or inactive.
	/// </summary>
	/// <param name="bActive">If set to <c>true</c> b active.</param>
	public void SetActiveSelector(bool bActive)
	{
		activeSelector = bActive;

		if (!activeSelector && !keepSelectedModel) 
		{
			DestroySelectedModel ();
		}
	}


	/// <summary>
	/// Gets the selected model.
	/// </summary>
	/// <returns>The selected model.</returns>
	public GameObject GetSelectedModel()
	{
		return selModel;
	}


	/// <summary>
	/// Destroys the currently selected model.
	/// </summary>
	public void DestroySelectedModel()
	{
		if (selModel) 
		{
			AvatarController ac = selModel.GetComponent<AvatarController>();
			KinectManager km = KinectManager.Instance;

			if (ac != null && km != null) 
			{
				km.avatarControllers.Remove(ac);
			}

			GameObject.Destroy (selModel);
			selModel = null;
			prevSelected = -1;
		}
	}


	/// <summary>
	/// Selects the next model.
	/// </summary>
	public void SelectNextModel()
	{
		selected++;
		if (selected >= numberOfModels) 
			selected = 0;

		//LoadModel(modelNames[selected]);
	}

	/// <summary>
	/// Selects the previous model.
	/// </summary>
	public void SelectPrevModel()
	{
		selected--;
		if (selected < 0) 
			selected = numberOfModels - 1;

		//LoadModel(modelNames[selected]);
	}


	void Start()
	{
		modelNames = new string[numberOfModels];
		modelThumbs = new Texture2D[numberOfModels];
		
		for (int i = 0; i < numberOfModels; i++)
		{
			modelNames[i] = string.Format("{0:0000}", i);

			string previewPath = modelCategory + "/" + modelNames[i] + "/preview.jpg";
			TextAsset resPreview = Resources.Load(previewPath, typeof(TextAsset)) as TextAsset;

			if (resPreview == null) 
			{
				resPreview = Resources.Load("nopreview.jpg", typeof(TextAsset)) as TextAsset;
			}

			//if(resPreview != null)
			{
				modelThumbs[i] = LoadTexture(resPreview != null ? resPreview.bytes : null);
			}
		}

		// save current scale factors and model offsets
		curScaleFactor = bodyScaleFactor + bodyWidthFactor + armScaleFactor + legScaleFactor;
		curModelOffset = verticalOffset + forwardOffset;
	}

	void Update()
	{
		if (selModel != null) 
		{
			if (Mathf.Abs(curModelOffset - (verticalOffset + forwardOffset)) >= 0.001f) 
			{
				// update model offsets
				curModelOffset = verticalOffset + forwardOffset;

				AvatarController ac = selModel.GetComponent<AvatarController>();
				if (ac != null) 
				{
					ac.verticalOffset = verticalOffset;
					ac.forwardOffset = forwardOffset;
				}
			}

			if (Mathf.Abs(curScaleFactor - (bodyScaleFactor + bodyWidthFactor + armScaleFactor + legScaleFactor)) >= 0.001f) 
			{
				// update scale factors
				curScaleFactor = bodyScaleFactor + bodyWidthFactor + armScaleFactor + legScaleFactor;

				AvatarScaler scaler = selModel.GetComponent<AvatarScaler>();
				if (scaler != null) 
				{
					scaler.continuousScaling = continuousScaling;
					scaler.bodyScaleFactor = bodyScaleFactor;
					scaler.bodyWidthFactor = bodyWidthFactor;
					scaler.armScaleFactor = armScaleFactor;
					scaler.legScaleFactor = legScaleFactor;
				}
			}
		}
	}
	
	void OnGUI()
	{
		if (activeSelector) 
		{
			menuWindowRectangle = GUI.Window(playerIndex * 10, menuWindowRectangle, MenuWindow, modelCategory);
		}
	}
	
	void MenuWindow(int windowID)
	{
		int windowX = windowScreenX >= 0 ? windowScreenX : Screen.width + windowScreenX;
		menuWindowRectangle = new Rect(windowX, 40, 165, Screen.height - 60);
		
		if (modelThumbs != null)
		{
			GUI.skin.button.fixedWidth = 120;
			GUI.skin.button.fixedHeight = 163;
			
			scroll = GUILayout.BeginScrollView(scroll);
			selected = GUILayout.SelectionGrid(selected, modelThumbs, 1);
			
			if (selected >= 0 && selected < modelNames.Length && prevSelected != selected)
			{
				KinectManager kinectManager = KinectManager.Instance;

				if (kinectManager && kinectManager.IsInitialized () && kinectManager.IsUserDetected()) 
				{
					prevSelected = selected;
					LoadModel(modelNames[selected]);
				}
			}
			
			GUILayout.EndScrollView();
			
			GUI.skin.button.fixedWidth = 0;
			GUI.skin.button.fixedHeight = 0;
		}
	}
	
	private Texture2D LoadTexture(byte[] btImage)
	{
		Texture2D tex = new Texture2D(4, 4);
		//Texture2D tex = new Texture2D(100, 143);

		if(btImage != null)
			tex.LoadImage(btImage);
		
		return tex;
	}
	
	private void LoadModel(string modelDir)
	{
		string modelPath = modelCategory + "/" + modelDir + "/model";
		UnityEngine.Object modelPrefab = Resources.Load(modelPath, typeof(GameObject));
		if(modelPrefab == null)
			return;

		if(selModel != null) 
		{
			GameObject.Destroy(selModel);
		}

		selModel = (GameObject)GameObject.Instantiate(modelPrefab, Vector3.zero, Quaternion.Euler(0, 180f, 0));
		selModel.name = "Model" + modelDir;

		AvatarController ac = selModel.GetComponent<AvatarController>();
		if (ac == null) 
		{
			ac = selModel.AddComponent<AvatarController>();
			ac.playerIndex = playerIndex;

			ac.mirroredMovement = true;
			ac.verticalMovement = true;

			ac.verticalOffset = verticalOffset;
			ac.forwardOffset = forwardOffset;
			ac.smoothFactor = 0f;
		}

		ac.posRelativeToCamera = modelRelativeToCamera;
		ac.posRelOverlayColor = (foregroundCamera != null);

		KinectManager km = KinectManager.Instance;
		//ac.Awake();

		long userId = km.GetUserIdByIndex(playerIndex);
		if(userId != 0)
		{
			ac.SuccessfulCalibration(userId);
		}

		// locate the available avatar controllers
		MonoBehaviour[] monoScripts = FindObjectsOfType(typeof(MonoBehaviour)) as MonoBehaviour[];
		km.avatarControllers.Clear();

		foreach(MonoBehaviour monoScript in monoScripts)
		{
			if((monoScript is AvatarController) && monoScript.enabled)
			{
				AvatarController avatar = (AvatarController)monoScript;
				km.avatarControllers.Add(avatar);
			}
		}

		AvatarScaler scaler = selModel.GetComponent<AvatarScaler>();
		if (scaler == null) 
		{
			scaler = selModel.AddComponent<AvatarScaler>();
			scaler.playerIndex = playerIndex;
			scaler.mirroredAvatar = true;

			scaler.continuousScaling = continuousScaling;
			scaler.bodyScaleFactor = bodyScaleFactor;
			scaler.bodyWidthFactor = bodyWidthFactor;
			scaler.armScaleFactor = armScaleFactor;
			scaler.legScaleFactor = legScaleFactor;
		}

		scaler.foregroundCamera = foregroundCamera;
		//scaler.debugText = debugText;

		//scaler.Start();
	}

}
