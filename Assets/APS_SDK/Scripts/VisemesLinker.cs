using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
	using UnityEditor;
	using APS;

	[CustomEditor(typeof(VisemesLinker))]
	[CanEditMultipleObjects]
	public class VisemesLinkerEditor : Editor
	{
		SerializedProperty visemeToBlendTargets;
		SerializedProperty laughterBlendTarget;

		private static string[] visemeNames = new string[]
		{
			"sil", "PP", "FF", "TH",
			"DD", "kk", "CH", "SS",
			"nn", "RR", "aa", "E",
			"ih", "oh", "ou"
		};

		public static Dictionary<string, string> visemeBlendShapeMap_CC3 = new Dictionary<string, string>()
		{
			{"sil", null},
			{"PP", "Mouth_Plosive"},
			{"FF", "Mouth_Bottom_Lip_Bite"},
			{"TH", null},
			{"DD", null},
			{"kk", null},
			{"CH", "Explosive"},
			{"SS", "Lip_Open"},
			{"nn", null},
			{"RR", null},
			{"aa", null},
			{"E", "Mouth_Lips_Open"},
			{"ih", null},
			{"oh", "Affricate"},
			{"ou", "Tight-O"}
		};
				
		public static Dictionary<string, string> visemeBlendShapeMap_Mixamo = new Dictionary<string, string>()
		{
			{"sil", null},
			{"PP", null},
			{"FF", null},
			{"TH", null},
			{"DD", null},
			{"kk", null},
			{"CH", null},
			{"SS", null},
			{"nn", null},
			{"RR", null},
			{"aa", null},
			{"E",  null},
			{"ih", null},
			{"oh", null},
			{"ou", null}
		};
		void OnEnable()
		{
			visemeToBlendTargets = serializedObject.FindProperty("visemeToBlendTargets");
			laughterBlendTarget = serializedObject.FindProperty("laughterBlendTarget");
		}

		public static string[] GetMeshBlendNames(ArmatureLinker linker)
		{
			if (linker.faceRenderer == null)
				return null;

			var mesh = linker.faceRenderer.sharedMesh;
			var blendshapeCount = mesh.blendShapeCount;
			var blendNames = new string[blendshapeCount + 1];
			blendNames[0] = "Not Used";
			for (int i = 0; i < mesh.blendShapeCount; ++i)
				blendNames[i + 1] = mesh.GetBlendShapeName(i);
			return blendNames;
		}

		public override void OnInspectorGUI()
		{
			VisemesLinker linkerVisemes = (VisemesLinker) target;
			ArmatureLinker linkerArmature = linkerVisemes.GetComponentInChildren<ArmatureLinker>();

			if (GUILayout.Button("Guess Links"))
			{
				if (EditorUtility.DisplayDialog("Guess Facecap Mappings?", "This will overwrite any current mappings.",
					"Ok", "Cancel"))
				{
					GuessMappings(linkerArmature, linkerVisemes);
				}
			}

			serializedObject.Update();
			if (linkerArmature.faceRenderer != null)
			{
				var blendNames = GetMeshBlendNames(linkerArmature);
				//if (EditorGUILayout.PropertyField(visemeToBlendTargets))
				//{
				EditorGUI.indentLevel++;
				for (int i = 0; i < visemeNames.Length; ++i)
					BlendNameProperty(linkerArmature, visemeToBlendTargets.GetArrayElementAtIndex(i), visemeNames[i],
						blendNames, linkerVisemes.visemeToBlendTargets[i]);


				BlendNameProperty(linkerArmature, laughterBlendTarget, "Laughter", blendNames,
					linkerVisemes.laughterBlendTarget);
				EditorGUI.indentLevel--;
				//}
			}
			else
				EditorGUILayout.LabelField("No \"faceRenderer\" currently set for ArmatureLinker.");

			serializedObject.ApplyModifiedProperties();

			//DrawDefaultInspector();
		}

		
		/*public static void GuessMappings(ArmatureLinker linkerArmature, VisemesLinker linkerVisemes)
		{
			if (linkerArmature.faceRenderer)
			{
				for (int i = 0; i < linkerVisemes.visemeToBlendTargets.Length; ++i)
					linkerVisemes.visemeToBlendTargets[i] = 0;

				var blendNames = GetMeshBlendNames(linkerArmature);

				var visemesNames = Enum.GetNames(typeof(VisemesLinker.Viseme));

				for (int i = 0; i < visemesNames.Length; ++i)
				for (int n = 0; n < blendNames.Length; ++n)
					if (blendNames[n].ToLower().EndsWith("_" + visemesNames[i].ToLower()))
						linkerVisemes.visemeToBlendTargets[i] = n;
			}
		}*/

		public static void GuessMappings(ArmatureLinker linkerArmature, VisemesLinker visemeLinker)
		{
			if (linkerArmature.faceRenderer)
			{
				
				visemeLinker.visemeToBlendTargets = Enumerable.Repeat(0, visemeNames.Length).ToArray(); //clear the array

				Dictionary<string, string> visemesBlendShapeMap = null;
				switch (linkerArmature.characterType)
				{
					case ArmatureLinker.CharacterType.DAZ3D_G2:
					case ArmatureLinker.CharacterType.DAZ3D_G3:
					case ArmatureLinker.CharacterType.MANUAL:
					case ArmatureLinker.CharacterType.MAKEHUMAN:
						for (int i = 0; i < visemeLinker.visemeToBlendTargets.Length; ++i)
							visemeLinker.visemeToBlendTargets[i] = 0;

						var blendNames = GetMeshBlendNames(linkerArmature);

						var visemesNames = Enum.GetNames(typeof(VisemesLinker.Viseme));

						for (int i = 0; i < visemesNames.Length; ++i)
						for (int n = 0; n < blendNames.Length; ++n)
							if (blendNames[n].ToLower().EndsWith("_" + visemesNames[i].ToLower()))
								visemeLinker.visemeToBlendTargets[i] = n;
						break;
					case ArmatureLinker.CharacterType.MIXAMO:
						visemesBlendShapeMap = visemeBlendShapeMap_Mixamo;
						break;
					case ArmatureLinker.CharacterType.CC3:
						visemesBlendShapeMap = visemeBlendShapeMap_CC3;
						break;
				}

				if (visemesBlendShapeMap != null)
					foreach (var visemeBlendShape in visemesBlendShapeMap)
					{
						if (visemeBlendShape.Value == null)
							continue;
						
						var skinnedMeshShapeIndex = linkerArmature.faceRenderer.sharedMesh.GetBlendShapeIndex(visemeBlendShape.Value);

						var visemeIndex = Array.IndexOf(visemeNames, visemeBlendShape.Key);

						if (visemeIndex == -1)
							visemeIndex = 0; //change -1 to 0 so the selection will be 'Not Used'

						if (skinnedMeshShapeIndex == -1)
							continue;
		
						visemeLinker.visemeToBlendTargets[visemeIndex] = skinnedMeshShapeIndex + 1;
					}
			}
		}


		
		private void BlendNameProperty(ArmatureLinker linker, SerializedProperty prop, string name,
			string[] blendNames = null, int shapeArrayIndex = -1)
		{
			if (blendNames == null)
			{
				EditorGUILayout.PropertyField(prop, new GUIContent(name));
				return;
			}

			var values = new int[blendNames.Length + 1];
			var options = new GUIContent[blendNames.Length + 1];

			values[0] = -1;
			options[0] = new GUIContent("   ");
			for (int i = 0; i < blendNames.Length; ++i)
			{
				values[i + 1] = i;
				options[i + 1] = new GUIContent(string.IsNullOrEmpty(blendNames[i]) ? i.ToString() : blendNames[i]);
			}

			var notUsed = shapeArrayIndex == 0;
			var shapeWeight = linker.faceRenderer.GetBlendShapeWeight(shapeArrayIndex - 1);

			EditorGUILayout.BeginHorizontal();
			GUIContent content = new GUIContent()
			{
				text = notUsed ? "na" : String.Format("{0}%", shapeWeight),
				tooltip = notUsed ? "Viseme is not used." : "Test viseme blendshape."
			};

			if (GUILayout.Button(content, GUILayout.ExpandWidth(false)))
			{
				if (!notUsed)
				{
					var isOn = shapeWeight != 0;
					//linker = (ArmatureLinker) FindObjectOfType(typeof(ArmatureLinker));
					linker.faceRenderer.SetBlendShapeWeight(shapeArrayIndex - 1, isOn ? 0 : 100);
				}
			}

			EditorGUILayout.IntPopup(prop, options, values, new GUIContent(name), GUILayout.ExpandWidth(true));
			EditorGUILayout.EndHorizontal();
		}

	}
#endif


public class VisemesLinker : MonoBehaviour
{
	public enum Viseme
	{
		sil,
		PP,
		FF,
		TH,
		DD,
		kk,
		CH,
		SS,
		nn,
		RR,
		aa,
		E,
		ih,
		oh,
		ou
	};

	public static readonly int VisemeCount = Enum.GetNames(typeof(Viseme)).Length;

	// Set the blendshape index to go to (-1 means there is not one assigned)
	[Tooltip("Blendshape index to trigger for each viseme.")]
	public int[] visemeToBlendTargets = new int[VisemeCount];

	[Tooltip("Blendshape index to trigger for laughter")]
	public int laughterBlendTarget = 0;

}
