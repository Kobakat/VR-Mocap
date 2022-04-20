using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using APS.Automation;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

//Create a Concurrent class so as to avoid using System.Collections.Concurrent which might conflict with vrchat sdk
public class Concurrent
{
	  public class ConcurrentQueue<T> : IEnumerable<T>, IEnumerable, ICollection, ISerializable, IDeserializationCallback
  {
    private ConcurrentQueue<T>.Node head = new ConcurrentQueue<T>.Node();
    private object syncRoot = new object();
    private ConcurrentQueue<T>.Node tail;
    private int count;

    public ConcurrentQueue()
    {
      this.tail = this.head;
    }

    public ConcurrentQueue(IEnumerable<T> enumerable)
      : this()
    {
      foreach (T obj in enumerable)
        this.Enqueue(obj);
    }

    protected ConcurrentQueue(SerializationInfo info, StreamingContext context)
    {
      throw new NotImplementedException();
    }

    public void Enqueue(T item)
    {
      Interlocked.Increment(ref this.count);
      ConcurrentQueue<T>.Node node = new ConcurrentQueue<T>.Node();
      node.Value = item;
      ConcurrentQueue<T>.Node comparand = (ConcurrentQueue<T>.Node) null;
      bool flag = false;
      while (!flag)
      {
        comparand = this.tail;
        ConcurrentQueue<T>.Node next = comparand.Next;
        if (this.tail == comparand)
        {
          if (next == null)
            flag = Interlocked.CompareExchange<ConcurrentQueue<T>.Node>(ref this.tail.Next, node, (ConcurrentQueue<T>.Node) null) == null;
          else
            Interlocked.CompareExchange<ConcurrentQueue<T>.Node>(ref this.tail, next, comparand);
        }
      }
      Interlocked.CompareExchange<ConcurrentQueue<T>.Node>(ref this.tail, node, comparand);
    }

    public bool TryDequeue(out T value)
    {
      value = default (T);
      bool flag = false;
      while (!flag)
      {
        ConcurrentQueue<T>.Node head = this.head;
        ConcurrentQueue<T>.Node tail = this.tail;
        ConcurrentQueue<T>.Node next = head.Next;
        if (head == this.head)
        {
          if (head == tail)
          {
            if (next != null)
              Interlocked.CompareExchange<ConcurrentQueue<T>.Node>(ref this.tail, next, tail);
            value = default (T);
            return false;
          }
          value = next.Value;
          flag = Interlocked.CompareExchange<ConcurrentQueue<T>.Node>(ref this.head, next, head) == head;
        }
      }
      Interlocked.Decrement(ref this.count);
      return true;
    }

    public bool TryPeek(out T value)
    {
      if (this.IsEmpty)
      {
        value = default (T);
        return false;
      }
      ConcurrentQueue<T>.Node next = this.head.Next;
      value = next.Value;
      return true;
    }

    internal void Clear()
    {
      this.count = 0;
      this.tail = this.head = new ConcurrentQueue<T>.Node();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return (IEnumerator) this.InternalGetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
      return this.InternalGetEnumerator();
    }

    public IEnumerator<T> GetEnumerator()
    {
      return this.InternalGetEnumerator();
    }

    private IEnumerator<T> InternalGetEnumerator()
    {
      ConcurrentQueue<T>.Node my_head = this.head;
      while ((my_head = my_head.Next) != null)
        yield return my_head.Value;
    }

    void ICollection.CopyTo(Array array, int index)
    {
      T[] dest = array as T[];
      if (dest == null)
        return;
      this.CopyTo(dest, index);
    }

    public void CopyTo(T[] dest, int index)
    {
      IEnumerator<T> enumerator = this.InternalGetEnumerator();
      int num = index;
      while (enumerator.MoveNext())
        dest[num++] = enumerator.Current;
    }

    public T[] ToArray()
    {
      T[] dest = new T[this.count];
      this.CopyTo(dest, 0);
      return dest;
    }

    protected virtual void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      throw new NotImplementedException();
    }

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
      this.GetObjectData(info, context);
    }

    bool ICollection.IsSynchronized
    {
      get
      {
        return true;
      }
    }

    protected virtual void OnDeserialization(object sender)
    {
      throw new NotImplementedException();
    }

    void IDeserializationCallback.OnDeserialization(object sender)
    {
      this.OnDeserialization(sender);
    }

    object ICollection.SyncRoot
    {
      get
      {
        return this.syncRoot;
      }
    }

    public int Count
    {
      get
      {
        return this.count;
      }
    }

    public bool IsEmpty
    {
      get
      {
        return this.count == 0;
      }
    }

    private class Node
    {
      public T Value;
      public ConcurrentQueue<T>.Node Next;
    }
  }
}



[CustomEditor(typeof(EmotionBuilderBase))]
public class EmotionBuilderBaseEditor : Editor
{

	void OnEnable()
	{
		EmotionBuilderBase builder = (EmotionBuilderBase) target;

		var linker = builder.transform.root.GetComponentInChildren<ArmatureLinker>();
		builder.LoadDefaultEmotions(linker);

		m_linkerArmature = builder.transform.root.GetComponentInChildren<ArmatureLinker>();
#if UNITY_EDITOR
		EditorApplication.update += OnEditorUpdate;
#endif
	}

	protected virtual void OnDisable()
	{
#if UNITY_EDITOR
		EditorApplication.update -= OnEditorUpdate;
#endif
	}

	
	private ArmatureLinker m_linkerArmature;

	private static Concurrent.ConcurrentQueue<Action> m_runOnMainThread = new Concurrent.ConcurrentQueue<Action>();

	protected virtual void OnEditorUpdate()
	{
		if (!m_runOnMainThread.IsEmpty)
		{
			Action action;
			m_runOnMainThread.TryDequeue(out action);
			action.Invoke();
		}
	}

	private static int jobCountStart = 0; //the total number of jobs queued for calculating progress percentage  

	public override void OnInspectorGUI()
	{
		EmotionBuilderBase builder = (EmotionBuilderBase) target;

		if (m_runOnMainThread.IsEmpty)
		{

			GUILayout.Space(5);
			GUILayout.Label("Eye Blinking Animation:");

			GUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Blink(L) Shape:", EditorStyles.boldLabel, GUILayout.Width(92));
			builder.blinkLeftShapeIdx = EditorGUILayout.IntField(builder.blinkLeftShapeIdx, GUILayout.Width(32));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Blink(R) Shape:", EditorStyles.boldLabel, GUILayout.Width(92));
			builder.blinkRightShapeIdx = EditorGUILayout.IntField(builder.blinkRightShapeIdx, GUILayout.Width(32));
			GUILayout.EndHorizontal();

			if (GUILayout.Button("Generate Blink Clips", GUILayout.Width(128)))
			{
				if (builder.blinkAnimation != null &&
				    builder.blinkAnimation.GetComponent<BlinkingAnimationRandomizer>())
				{
					if (EditorUtility.DisplayDialog("Blink Shapes Already Generated",
						"The blink blendshapes appear to have already been generated. Would you like to generate them again?",
						"Generate", "Cancel"))
						CreateBlinkAnimation(builder);
				}
				else
					CreateBlinkAnimation(builder);
			}

			if (builder.blinkAnimation != null)
			{
				GUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Blink Mesh:", EditorStyles.boldLabel, GUILayout.Width(70));
				EditorGUILayout.ObjectField(builder.blinkAnimation, typeof(Animation), GUILayout.Width(54));
				GUILayout.EndHorizontal();
			}

			GUILayout.Space(10);
			GuiLine();
			GUILayout.Space(10);

			if (GUILayout.Button("Delete All Emotions", GUILayout.Width(128)))
			{
				m_runOnMainThread = new Concurrent.ConcurrentQueue<Action>(); //stop any current jobs from processing

				var allEmotionBuilder = builder.GetComponents<EmotionBuilder>();

				if (allEmotionBuilder.Length == 0)
					return;

				if (EditorUtility.DisplayDialog("Delete All Emotions?",
					"Are you sure you want to remove all emotions builders?", "Delete", "Cancel"))
					foreach (var emotionBuilder in builder.GetComponents<EmotionBuilder>())
					{
						DestroyImmediate(emotionBuilder);
					}
			}

			switch (m_linkerArmature.characterType)
			{
				case ArmatureLinker.CharacterType.MIXAMO:
				case ArmatureLinker.CharacterType.MAKEHUMAN:
				case ArmatureLinker.CharacterType.CC3:
					if (GUILayout.Button("Generate Emotions", GUILayout.Width(128)))
					{
						if (builder.templatesNames == null || builder.templatesNames.Length == 0)
						{
							if (EditorUtility.DisplayDialog("No Blendshapes Found!",
								string.Format("No blendshapes found for avatar:\n{0}.",
									m_linkerArmature.transform.root.name), "OK"))
							{
							}

							return;
						}

						//set the initial parameters manually (for debugging)
/*
						m_linkerArmature.iJawRotation = m_linkerArmature.jaw.localRotation;
						m_linkerArmature.iJawPosition = m_linkerArmature.jaw.localPosition;
*/						

						if (EditorUtility.DisplayDialog("Generate Default Emotions?",
							String.Format(
								"Attempt to auto generate {0} new emotions from default templates for this avatar?\n\nThis may take a few seconds, please be patient and allow all emotionBuilders to initialize and generate thumbnails.",
								builder.templatesNames.Length), "Generate", "Cancel"))
						{
							m_linkerArmature.ResetJaw();
							
							m_runOnMainThread =
								new Concurrent.ConcurrentQueue<Action>(); //stop any current jobs from processing
							jobCountStart = builder.templatesNames.Length * 3;

							//var linkerArmature = builder.transform.root.GetComponentInChildren<ArmatureLinker>();
							for (int i = 1; i < builder.templatesNames.Length; i++)
							{
								var idx = i;
								var emotionBuilder = builder.gameObject.AddComponent<EmotionBuilder>();
								emotionBuilder.emotionName = builder.templatesNames[idx];
								EmotionBuilderBase.SetupFromTemplate(m_linkerArmature, emotionBuilder, builder.templates[idx - 1]);

								//Unity can not update blendshapes or skinnedmeshrenders multiple times in a single frame, so must queue these jobs and run them in a callback so portrait pictures will update correctly - hackey I know, but Unity editor is very limited..
								m_runOnMainThread.Enqueue(() => { SetWeights(emotionBuilder); });
								m_runOnMainThread.Enqueue(delegate { return; });
								m_runOnMainThread.Enqueue(() => { SaveChanges(emotionBuilder); });

							}

						}
					}

					break;
			}

			GUILayout.Space(10);
			GuiLine();
			GUILayout.Space(10);

			if (GUILayout.Button("Refresh Thumbnails", GUILayout.Width(128)))
			{
				m_runOnMainThread = new Concurrent.ConcurrentQueue<Action>(); //stop any current jobs from processing

				var emotionBuilders = builder.GetComponents<EmotionBuilder>();
				jobCountStart = emotionBuilders.Length * 2;

				foreach (var emotionBuilder in emotionBuilders)
				{
					//Unity can not update blendshapes or skinnedmeshrenders multiple times in a single frame, so must queue these jobs and run them in a callback so portrait pictures will update correctly - hackey I know..
					m_runOnMainThread.Enqueue(() => { SetWeights(emotionBuilder); });
					m_runOnMainThread.Enqueue(delegate { return; });
					m_runOnMainThread.Enqueue(() => { SaveChanges(emotionBuilder); });
				}
			}

			if (GUILayout.Button("Reset Weights", GUILayout.Width(128)))
			{
				ResetWeights();
			}
			
			if (GUILayout.Button("Reset Jaw", GUILayout.Width(128)))
			{
				ResetJaw();
			}

			builder.key = (EmotionBuilderBase.RawKey) EditorGUILayout.EnumPopup("Reset Hotkey:", builder.key);
			GUILayout.Space(15);
		}
		else
		{

			GUIStyle customLabel;

			customLabel = new GUIStyle("Label");
			customLabel.alignment = TextAnchor.MiddleCenter;
			customLabel.fontSize = 14;
			customLabel.normal.textColor = Color.blue;
			customLabel.fontStyle = FontStyle.Normal;

			GUILayout.Label("GENERATING THUMBNAILS:", customLabel);

			customLabel = new GUIStyle("Label");
			customLabel.alignment = TextAnchor.MiddleCenter;
			customLabel.fontSize = 18;
			customLabel.normal.textColor = Color.blue;
			customLabel.fontStyle = FontStyle.Bold;

			var progress = ((float) jobCountStart - (float) m_runOnMainThread.Count) / (float) jobCountStart;
			
#if !UNITY_2018 && UNITY_2018_4_OR_NEWER
			EditorGUILayout.Space(5);
#else
			EditorGUILayout.Space();
#endif
			
			var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
			EditorGUI.ProgressBar(rect, progress, String.Format("{0:00}%", progress * 100));

#if !UNITY_2018 && UNITY_2018_4_OR_NEWER
			EditorGUILayout.Space(15);
#else
			EditorGUILayout.Space();
#endif

			var oldColor = GUI.backgroundColor;

			var style = new GUIStyle(GUI.skin.button);
			style.alignment = TextAnchor.MiddleCenter;
			style.hover.textColor = style.normal.textColor = Color.yellow;
			style.fontStyle = FontStyle.Bold;

			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			GUI.backgroundColor = Color.red;
			if (GUILayout.Button("STOP JOBS", style, GUILayout.Width(128)))
			{
				m_runOnMainThread = new Concurrent.ConcurrentQueue<Action>(); //stop any current jobs from processing
			}

			GUI.backgroundColor = oldColor;

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();

#if !UNITY_2018 && UNITY_2018_4_OR_NEWER
			EditorGUILayout.Space(15);
#else
			EditorGUILayout.Space();
#endif
		}


		serializedObject.ApplyModifiedProperties();

		//DrawDefaultInspector();
	}

	void GuiLine(int i_height = 1)
	{
		Rect rect = EditorGUILayout.GetControlRect(false, i_height);

		rect.height = i_height;

		EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
	}

	public void ResetJaw()
	{
		/*var j = m_linkerArmature.restPose.transform.FindDeepChild("RestPoseRig_" + m_linkerArmature.jaw.name);
		Debug.Log(m_linkerArmature + " RestPoseRig_" + m_linkerArmature.jaw.name);
		m_linkerArmature.iJawRotation = m_linkerArmature.jaw.localRotation = j.localRotation;
		m_linkerArmature.iJawPosition = m_linkerArmature.jaw.localPosition = j.localPosition;*/
		
		m_linkerArmature.jaw.localRotation = m_linkerArmature.iJawRotation;
		m_linkerArmature.jaw.localPosition = m_linkerArmature.iJawPosition;
	}
	
	public void ResetWeights()
	{
		for (int i = 0; i < m_linkerArmature.faceRenderer.sharedMesh.blendShapeCount; ++i)
			m_linkerArmature.faceRenderer.SetBlendShapeWeight(i, 0);

		if (m_linkerArmature.jaw)
		{
			m_linkerArmature.jaw.localRotation = m_linkerArmature.iJawRotation;
			m_linkerArmature.jaw.localPosition = m_linkerArmature.iJawPosition;
		}
	}

	public static void SetWeights(EmotionBuilder builder, bool includeJaw = true)
	{
		var linkerArmature = builder.transform.root.GetComponentInChildren<ArmatureLinker>();
		for (int i = 0; i < builder.blendShapeWeights.Length; ++i)
		{
			var shapeWeight = builder.blendShapeWeights[i];
			linkerArmature.faceRenderer.SetBlendShapeWeight(i, shapeWeight * 100);
		}

	//	Debug.Log(builder.name + " -- " + builder.jawRotation.z);

		if (includeJaw && linkerArmature.jaw)
		{
			linkerArmature.jaw.localRotation = linkerArmature.iJawRotation;
			linkerArmature.jaw.localPosition = linkerArmature.iJawPosition;

			linkerArmature.jaw.Rotate(-builder.jawRotation, Space.Self);
			linkerArmature.jaw.localPosition += builder.jawPosition;
		}
	}

	public static void SaveChanges(EmotionBuilder builder)
	{
		var thumbnail = builder.GetComponentInChildren<EmotionThumbnail>();

		var avatarAsset =
			builder.GetComponentInParent<Animator>()
				.avatar; //get the avatar because it is usually always found in the actual processing folder

		string avatarPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(avatarAsset));

		var folder = Path.Combine(avatarPath, "Emotions");

		if (!Directory.Exists(folder))
		{
			Directory.CreateDirectory(folder);
		}

		var linkerArmature = builder.transform.root.GetComponentInChildren<ArmatureLinker>();

		if (linkerArmature.jaw) {
			var userRotation = Quaternion.Inverse(linkerArmature.transform.rotation) * linkerArmature.jaw.rotation;
			var restRotation = Quaternion.Inverse(linkerArmature.transform.rotation) * linkerArmature.jaw.parent.rotation * linkerArmature.iJawRotation;
			var deltaRotation = Quaternion.Inverse(userRotation) * restRotation;
			
			builder.jawRotation = deltaRotation.eulerAngles;
			builder.jawPosition = linkerArmature.jaw.localPosition - linkerArmature.iJawPosition; // - (linkerArmature.jaw.localPosition));
		}

		
		//use a unique directory within the processing folder in case there are avatar variants and will prevent overwriting thumbnails if they have the same name
		var m = Regex.Match(linkerArmature.transform.root.name,
			@"[{(]?[0-9A-Fa-f]{8}[-]?(?:[0-9A-Fa-f]{4}[-]?){3}[0-9A-Fa-f]{12}[)}]?"); //extract the unique GUID from the prefab root name 
		var match = m.Groups[0].Value;

		//now get a path to store the emotion thumbnails
		var path = Path.Combine(folder,
			string.Concat(builder.emotionName, ".png")); //a default folder in case something goes wrong
		if (!string.IsNullOrEmpty(match))
			path = Path.Combine(folder,
				Path.Combine(match,
					string.Concat(builder.emotionName,
						".png"))); //the unique folder within the avatar's processing folder

		var rootObjs = new List<GameObject>();
		foreach (var item in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
		{
			if (item.activeSelf
			) //only store active go's, so that we may disable them during portrait pictures, them re-enable them
				rootObjs.Add(item);
		}

		foreach (GameObject go in rootObjs)
		{
			//ensure there are no other objects visible in the scene prior to taking portraits
			if (builder.transform.IsChildOf(go.transform))
				continue;
			go.SetActive(false);
		}

		try
		{
			thumbnail.CamCapture(path);
		}
		finally
		{
			foreach (GameObject go in rootObjs)
				go.SetActive(true);
		}

		var myGUITexture = (Texture2D) AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
		builder.thumbnail = myGUITexture;
	}

	public static void CreateBlinkAnimation(EmotionBuilderBase builder, bool verbose = true)
	{
		//EmotionBuilderBase builder = (EmotionBuilderBase)target;

		if (builder.blinkLeftShapeIdx == -1 || builder.blinkRightShapeIdx == -1)
		{
			if (verbose)
				EditorUtility.DisplayDialog("Blendshapes Not Set!",
					"Neither of the blendshapes may be set to \"-1\". Please ensure to set both the left and right blink shape to the corresponding indexes of avatar's left and right eye blink blendshapes.'",
					"Ok");

			return;
		}

		var blinkShapeMesh = builder.transform.root.GetComponentInChildren<ArmatureLinker>().faceRenderer;
		if (blinkShapeMesh.GetComponent<Animation>())
			DestroyImmediate(blinkShapeMesh.GetComponent<Animation>());

		builder.blinkAnimation = blinkShapeMesh.gameObject.AddComponent<Animation>();
		var randomizer = builder.blinkAnimation.gameObject.GetComponent<BlinkingAnimationRandomizer>();
		if (randomizer == null)
			randomizer = builder.blinkAnimation.gameObject.AddComponent<BlinkingAnimationRandomizer>();
		randomizer.animation = builder.blinkAnimation;

		//Load all blink animation templates
		var allTemplateClips = Resources.LoadAll("Animations/Blinking", typeof(AnimationClip));
		foreach (AnimationClip templateClip in allTemplateClips)
		{
			// create a new AnimationClip
			AnimationClip clip = new AnimationClip();
			clip.legacy = true;
			clip.wrapMode = WrapMode.Loop;

			foreach (var binding in AnimationUtility.GetCurveBindings(templateClip))
			{
				AnimationCurve templateCurve = AnimationUtility.GetEditorCurve(templateClip, binding);

				//Debug.Log(binding.path + "/" + binding.propertyName + ", Keys: " + templateCurve.keys.Length);

				var blinkLShapeName = blinkShapeMesh.sharedMesh.GetBlendShapeName(builder.blinkLeftShapeIdx);
				clip.SetCurve("", typeof(SkinnedMeshRenderer), String.Format("blendShape.{0}", blinkLShapeName),
					templateCurve);

				var blinkRShapeName = blinkShapeMesh.sharedMesh.GetBlendShapeName(builder.blinkRightShapeIdx);
				clip.SetCurve("", typeof(SkinnedMeshRenderer), String.Format("blendShape.{0}", blinkRShapeName),
					templateCurve);
			}

			var avatarAsset =
				builder.GetComponentInParent<Animator>()
					.avatar; //get the avatar because it is usually always found in the actual processing folder

			string avatarPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(avatarAsset));

			var folder = Path.Combine(avatarPath, "Emotions");

			if (!Directory.Exists(folder))
			{
				Directory.CreateDirectory(folder);
			}

			var linkerArmature = builder.transform.root.GetComponentInChildren<ArmatureLinker>();
			//use a unique directory within the processing folder in case there are avatar variants and will prevent overwriting animations if they have the same name
			var m = Regex.Match(linkerArmature.transform.root.name,
				@"[{(]?[0-9A-Fa-f]{8}[-]?(?:[0-9A-Fa-f]{4}[-]?){3}[0-9A-Fa-f]{12}[)}]?"); //extract the unique GUID from the prefab root name 
			var match = m.Groups[0].Value;

			var saveName = String.Format("Avatar_{0}.anim", templateClip.name);
			var path = Path.Combine(folder, saveName); //a default folder in case something goes wrong
			if (!string.IsNullOrEmpty(match))
				path = Path.Combine(folder,
					Path.Combine(match, saveName)); //the unique folder within the avatar's processing folder

			var destDir = Path.GetDirectoryName(path);
			if (!Directory.Exists(destDir))
			{
				Directory.CreateDirectory(destDir);
			}

			AssetDatabase.CreateAsset(clip, path);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			clip = (AnimationClip) AssetDatabase.LoadMainAssetAtPath(path);

			if (builder.blinkAnimation.clip == null)
				builder.blinkAnimation.clip = clip;

			builder.blinkAnimation.AddClip(clip, clip.name);
		}
	}

}
#endif

[DisallowMultipleComponent]
public class EmotionBuilderBase : MonoBehaviour
{
	public int blinkLeftShapeIdx = -1;
	public int blinkRightShapeIdx = -1;

	public Animation blinkAnimation;

	[HideInInspector] public RawKey key = (RawKey) 0x00; //Defaults to None

	//Same as UnityRawInput bindings
	public enum RawKey : ushort
	{
		None = 0x00,
		LeftButton = 0x01,
		RightButton = 0x02,
		Cancel = 0x03,
		MiddleButton = 0x04,
		ExtraButton1 = 0x05,
		ExtraButton2 = 0x06,
		Back = 0x08,
		Tab = 0x09,
		Clear = 0x0C,
		Return = 0x0D,
		Shift = 0x10,
		Control = 0x11,
		Menu = 0x12,
		Pause = 0x13,
		CapsLock = 0x14,
		Kana = 0x15,
		Hangeul = 0x15,
		Hangul = 0x15,
		Junja = 0x17,
		Final = 0x18,
		Hanja = 0x19,
		Kanji = 0x19,
		Escape = 0x1B,
		Convert = 0x1C,
		NonConvert = 0x1D,
		Accept = 0x1E,
		ModeChange = 0x1F,
		Space = 0x20,
		Prior = 0x21,
		Next = 0x22,
		End = 0x23,
		Home = 0x24,
		Left = 0x25,
		Up = 0x26,
		Right = 0x27,
		Down = 0x28,
		Select = 0x29,
		Print = 0x2A,
		Execute = 0x2B,
		Snapshot = 0x2C,
		Insert = 0x2D,
		Delete = 0x2E,
		Help = 0x2F,
		N0 = 0x30,
		N1 = 0x31,
		N2 = 0x32,
		N3 = 0x33,
		N4 = 0x34,
		N5 = 0x35,
		N6 = 0x36,
		N7 = 0x37,
		N8 = 0x38,
		N9 = 0x39,
		A = 0x41,
		B = 0x42,
		C = 0x43,
		D = 0x44,
		E = 0x45,
		F = 0x46,
		G = 0x47,
		H = 0x48,
		I = 0x49,
		J = 0x4A,
		K = 0x4B,
		L = 0x4C,
		M = 0x4D,
		N = 0x4E,
		O = 0x4F,
		P = 0x50,
		Q = 0x51,
		R = 0x52,
		S = 0x53,
		T = 0x54,
		U = 0x55,
		V = 0x56,
		W = 0x57,
		X = 0x58,
		Y = 0x59,
		Z = 0x5A,
		LeftWindows = 0x5B,
		RightWindows = 0x5C,
		Application = 0x5D,
		Sleep = 0x5F,
		Numpad0 = 0x60,
		Numpad1 = 0x61,
		Numpad2 = 0x62,
		Numpad3 = 0x63,
		Numpad4 = 0x64,
		Numpad5 = 0x65,
		Numpad6 = 0x66,
		Numpad7 = 0x67,
		Numpad8 = 0x68,
		Numpad9 = 0x69,
		Multiply = 0x6A,
		Add = 0x6B,
		Separator = 0x6C,
		Subtract = 0x6D,
		Decimal = 0x6E,
		Divide = 0x6F,
		F1 = 0x70,
		F2 = 0x71,
		F3 = 0x72,
		F4 = 0x73,
		F5 = 0x74,
		F6 = 0x75,
		F7 = 0x76,
		F8 = 0x77,
		F9 = 0x78,
		F10 = 0x79,
		F11 = 0x7A,
		F12 = 0x7B,
		F13 = 0x7C,
		F14 = 0x7D,
		F15 = 0x7E,
		F16 = 0x7F,
		F17 = 0x80,
		F18 = 0x81,
		F19 = 0x82,
		F20 = 0x83,
		F21 = 0x84,
		F22 = 0x85,
		F23 = 0x86,
		F24 = 0x87,
		NumLock = 0x90,
		ScrollLock = 0x91,
		NEC_Equal = 0x92,
		Fujitsu_Jisho = 0x92,
		Fujitsu_Masshou = 0x93,
		Fujitsu_Touroku = 0x94,
		Fujitsu_Loya = 0x95,
		Fujitsu_Roya = 0x96,
		LeftShift = 0xA0,
		RightShift = 0xA1,
		LeftControl = 0xA2,
		RightControl = 0xA3,
		LeftMenu = 0xA4,
		RightMenu = 0xA5,
		BrowserBack = 0xA6,
		BrowserForward = 0xA7,
		BrowserRefresh = 0xA8,
		BrowserStop = 0xA9,
		BrowserSearch = 0xAA,
		BrowserFavorites = 0xAB,
		BrowserHome = 0xAC,
		VolumeMute = 0xAD,
		VolumeDown = 0xAE,
		VolumeUp = 0xAF,
		MediaNextTrack = 0xB0,
		MediaPrevTrack = 0xB1,
		MediaStop = 0xB2,
		MediaPlayPause = 0xB3,
		LaunchMail = 0xB4,
		LaunchMediaSelect = 0xB5,
		LaunchApplication1 = 0xB6,
		LaunchApplication2 = 0xB7,
		OEM1 = 0xBA,
		OEMPlus = 0xBB,
		OEMComma = 0xBC,
		OEMMinus = 0xBD,
		OEMPeriod = 0xBE,
		OEM2 = 0xBF,
		OEM3 = 0xC0,
		OEM4 = 0xDB,
		OEM5 = 0xDC,
		OEM6 = 0xDD,
		OEM7 = 0xDE,
		OEM8 = 0xDF,
		OEMAX = 0xE1,
		OEM102 = 0xE2,
		ICOHelp = 0xE3,
		ICO00 = 0xE4,
		ProcessKey = 0xE5,
		ICOClear = 0xE6,
		Packet = 0xE7,
		OEMReset = 0xE9,
		OEMJump = 0xEA,
		OEMPA1 = 0xEB,
		OEMPA2 = 0xEC,
		OEMPA3 = 0xED,
		OEMWSCtrl = 0xEE,
		OEMCUSel = 0xEF,
		OEMATTN = 0xF0,
		OEMFinish = 0xF1,
		OEMCopy = 0xF2,
		OEMAuto = 0xF3,
		OEMENLW = 0xF4,
		OEMBackTab = 0xF5,
		ATTN = 0xF6,
		CRSel = 0xF7,
		EXSel = 0xF8,
		EREOF = 0xF9,
		Play = 0xFA,
		Zoom = 0xFB,
		Noname = 0xFC,
		PA1 = 0xFD,
		OEMClear = 0xFE
	}

	[SerializeField] private TextAsset[] m_templates;

	public TextAsset[] templates
	{
		get { return m_templates; }
	}

	[SerializeField] private String[] m_templatesNames;

	public String[] templatesNames
	{
		get { return m_templatesNames; }
	}

	[System.Serializable]
	public class ExpressionTemplateJsonUnit
	{
		public string name;
		public float val;
	}

	[System.Serializable]
	public class ExpressionTemplateJsonObject
	{
		public string templateName;
		public ExpressionTemplateJsonUnit[] template;
		public Vector3 jawRotation;
	}

	public void LoadDefaultEmotions(ArmatureLinker linker)
	{
		if (linker.faceRenderer == null)
			return;

		String templateFolder = "";
		switch (linker.characterType)
		{
			case ArmatureLinker.CharacterType.MANUAL:
			case ArmatureLinker.CharacterType.RIGIFY:
			case ArmatureLinker.CharacterType.DAZ3D_G3:
			case ArmatureLinker.CharacterType.DAZ3D_G2:
				break;
			case ArmatureLinker.CharacterType.MAKEHUMAN:
				templateFolder = "ExpressionsMakehuman";
				break;
			case ArmatureLinker.CharacterType.CC3:
				templateFolder = "ExpressionsCC3";
				break;
			case ArmatureLinker.CharacterType.MIXAMO:
				templateFolder = "ExpressionMixamo";
				break;
		}

		if (!String.IsNullOrEmpty(templateFolder))
		{
			m_templates = Resources.LoadAll<TextAsset>(templateFolder + '/');

			var templateList = new List<string>();
			templateList.Add("None"); //default to None
			templateList.AddRange(m_templates.Select(s => s.name).ToArray());
			m_templatesNames = templateList.ToArray();
		}
	}

	public static void SetupFromTemplate(ArmatureLinker linker, EmotionBuilder emotionBuilder, TextAsset template)
	{
		ExpressionTemplateJsonObject expressionTemplate =
			JsonUtility.FromJson<ExpressionTemplateJsonObject>(template.text);

		emotionBuilder.emotionName = template.name;
		emotionBuilder.blendShapeWeights = new float[linker.faceRenderer.sharedMesh.blendShapeCount];

		foreach (var unit in expressionTemplate.template)
		{
			var shapeIdx = linker.faceRenderer.sharedMesh.GetBlendShapeIndex(unit.name);
			if (shapeIdx != -1)
			{
				emotionBuilder.blendShapeWeights[shapeIdx] = unit.val;
				linker.faceRenderer.SetBlendShapeWeight(shapeIdx, unit.val * 100);
			}
		}

		if (linker.jaw)
			emotionBuilder.jawRotation = expressionTemplate.jawRotation;
	
		//EmotionBuilderEditor.SetWeights(emotionBuilder);
		//EmotionBuilderEditor.SaveChanges(emotionBuilder);
	}

}
