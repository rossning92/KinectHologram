using UnityEngine;
using System.Collections;

public class ForegroundBlender : MonoBehaviour 
{
	[Tooltip("Background texture that will be rendered over the foreground of detected users.")]
	public Texture backgroundTexture;


	private Material foregroundBlendMat;
	private KinectManager kinectManager;
	private BackgroundRemovalManager backManager;
	private long lastDepthFrameTime;


	void Start () 
	{
		kinectManager = KinectManager.Instance;

		if(kinectManager && kinectManager.IsInitialized() && backgroundTexture)
		{
			Shader foregoundBlendShader = Shader.Find("Custom/ForegroundBlendShader");

			if(foregoundBlendShader != null)
			{
				foregroundBlendMat = new Material(foregoundBlendShader);

				foregroundBlendMat.SetInt ("_ColorFlipH", 0);
				foregroundBlendMat.SetInt ("_ColorFlipV", 1);
			}
		}
	}

	void OnDestroy()
	{
	}

	void Update () 
	{
		if(foregroundBlendMat && backgroundTexture && 
			kinectManager && kinectManager.IsInitialized())
		{
			if (!backManager) 
			{
				backManager = BackgroundRemovalManager.Instance;
			}

			Texture alphaBodyTex = backManager ? backManager.GetAlphaBodyTex () : null;
			KinectInterop.SensorData sensorData = kinectManager.GetSensorData();

			if(backManager && backManager.IsBackgroundRemovalInitialized() && 
				alphaBodyTex && backgroundTexture && lastDepthFrameTime != sensorData.lastDepthFrameTime)
			{
				lastDepthFrameTime = sensorData.lastDepthFrameTime;
				foregroundBlendMat.SetTexture("_BodyTex", alphaBodyTex);
			}

			foregroundBlendMat.SetTexture("_ColorTex", backgroundTexture);
		}
	}

	void OnRenderImage (RenderTexture source, RenderTexture destination)
	{
		if(foregroundBlendMat != null)
		{
			Graphics.Blit(source, destination, foregroundBlendMat);
		}
	}

}
