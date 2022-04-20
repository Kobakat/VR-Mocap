using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using APS.Automation;

[CustomEditor(typeof(EmotionThumbnail))]
class EmotionThumbnailEditor : Editor
{
	public override void OnInspectorGUI()
	{
		EmotionThumbnail baseScript = (EmotionThumbnail) target;
		if (GUILayout.Button("Take Screenshot"))
		{
			var folder = Path.Combine(Application.dataPath, "Screenshots");
			if (!Directory.Exists(folder))
			{
				Directory.CreateDirectory(folder);
			}

			var path = Path.Combine(folder, "screenshot_emotion.png");
			Debug.Log(path);
			baseScript.CamCapture(path);
		}

		DrawDefaultInspector();
	}
}
#endif

namespace APS.Automation
{
	[ExecuteInEditMode]
	public class EmotionThumbnail : MonoBehaviour
	{

		public Transform flashLight;

		public Camera m_camera
		{
			get { return GetComponent<Camera>(); }
		}

		public void FixColorSpace(Texture2D image)
		{
			for (int y = 0; y < image.height; y++)
			{
				for (int x = 0; x < image.width; x++)
				{
					image.SetPixel(x, y, new Color(
						Mathf.Pow(image.GetPixel(x, y).r, 1f / 2.2f),
						Mathf.Pow(image.GetPixel(x, y).g, 1f / 2.2f),
						Mathf.Pow(image.GetPixel(x, y).b, 1f / 2.2f),
						Mathf.Pow(image.GetPixel(x, y).a, 1f / 2.2f)
					));
				}
			}
		}

		public Texture2D CamCapture()
		{
			RenderTexture.active = m_camera.targetTexture;

			if (flashLight)
				flashLight.gameObject.SetActive(true);

			m_camera.Render();

			if (flashLight)
				flashLight.gameObject.SetActive(false);

			Texture2D image = new Texture2D(m_camera.targetTexture.width, m_camera.targetTexture.height);
			image.ReadPixels(new Rect(0, 0, m_camera.targetTexture.width, m_camera.targetTexture.height), 0, 0);

			FixColorSpace(image);
			image.Apply();

			return image;
		}

		public void CamCapture(string destPath)
		{
			/*
			RenderTexture texture = new RenderTexture(
				m_camera.targetTexture.width,
				m_camera.targetTexture.height, 
				m_camera.targetTexture.depth,
				m_camera.targetTexture.format
				);
	
			m_camera.targetTexture = texture;
			*/

			//RenderTexture currentRT = RenderTexture.active;
			//RenderTexture.active = texture;//m_camera.targetTexture;

			Texture2D image = CamCapture();

			//RenderTexture.active = currentRT;



			//AssetDatabase.CreateAsset(texture, destPath);
			//AssetDatabase.SaveAssets();
			//AssetDatabase.Refresh();

			//return;
			var Bytes = image.EncodeToPNG();
			//DestroyImmediate(image);

			var destDir = Path.GetDirectoryName(destPath);
			if (!Directory.Exists(destDir))
			{
				Directory.CreateDirectory(destDir);
			}

			File.WriteAllBytes(destPath, Bytes);

			DestroyImmediate(image);

#if UNITY_EDITOR
			AssetDatabase.Refresh();
#endif
		}

	}

}