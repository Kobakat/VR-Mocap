using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(FacecapLinker))]
[CanEditMultipleObjects]
public class FacecapLinkerEditor : Editor
{
	private int[] sizes;

	public static string[] facecapNames = new string[]
	{
		"None",
		"browInnerUp",
		"browDown_L",
		"browDown_R",
		"browOuterUp_L",
		"browOuterUp_R",
		"eyeLookUp_L",
		"eyeLookUp_R",
		"eyeLookDown_L",
		"eyeLookDown_R",
		"eyeLookIn_L",
		"eyeLookIn_R",
		"eyeLookOut_L",
		"eyeLookOut_R",
		"eyeBlink_L",
		"eyeBlink_R",
		"eyeSquint_L",
		"eyeSquint_R",
		"eyeWide_L",
		"eyeWide_R",
		"cheekPuff",
		"cheekSquint_L",
		"cheekSquint_R",
		"noseSneer_L",
		"noseSneer_R",
		"jawOpen",
		"jawForward",
		"jawLeft",
		"jawRight",
		"mouthFunnel",
		"mouthPucker",
		"mouthLeft",
		"mouthRight",
		"mouthRollUpper",
		"mouthRollLower",
		"mouthShrugUpper",
		"mouthShrugLower",
		"mouthClose",
		"mouthSmile_L",
		"mouthSmile_R",
		"mouthFrown_L",
		"mouthFrown_R",
		"mouthDimple_L",
		"mouthDimple_R",
		"mouthUpperUp_L",
		"mouthUpperUp_R",
		"mouthLowerDown_L",
		"mouthLowerDown_R",
		"mouthPress_L",
		"mouthPress_R",
		"mouthStretch_L",
		"mouthStretch_R",
		"tongueOut"
	};

	public static Dictionary<string, string> faceCapBlendShapeMap_Mixamo = new Dictionary<string, string>()
	{

		{"Blink_Left", "eyeBlink_L"},
		{"Blink_Right", "eyeBlink_R"},
		{"BrowsDown_Left", "browDown_L"},
		{"BrowsDown_Right", "browDown_R"},
		{"BrowsIn_Left", null},
		{"BrowsIn_Right", null},
		{"BrowsOuterLower_Left", "browInnerUp"},
		{"BrowsOuterLower_Right", "browInnerUp"},
		{"BrowsUp_Left", "browOuterUp_L"},
		{"BrowsUp_Right", "browOuterUp_R"},
		{"CheekPuff_Left", "cheekPuff"},
		{"CheekPuff_Right", "cheekPuff"},
		{"EyesWide_Left", "eyeWide_L"},
		{"EyesWide_Right", "eyeWide_R"},
		{"Frown_Left", "mouthFrown_L"},
		{"Frown_Right", "mouthFrown_R"},
		{"JawBackward", null},
		{"JawForeward", "jawForward"},
		{"JawRotateY_Left", null},
		{"JawRotateY_Right", null},
		{"JawRotateZ_Left", null},
		{"JawRotateZ_Right", null},
		{"Jaw_Down", "jawOpen"},
		{"Jaw_Left", "jawLeft"},
		{"Jaw_Right", "jawRight"},
		{"Jaw_Up", "mouthClose"},
		{"LowerLipDown_Left", "mouthLowerDown_L"},
		{"LowerLipDown_Right", "mouthLowerDown_R"},
		{"LowerLipIn", "mouthRollLower"},
		{"LowerLipOut", "mouthShrugLower"},
		{"Midmouth_Left", "mouthPucker"},
		{"Midmouth_Right", "mouthPucker"},
		{"MouthDown", null},
		{"MouthNarrow_Left", "mouthPress_L"},
		{"MouthNarrow_Right", "mouthPress_R"},
		{"MouthOpen", null},
		{"MouthUp", null},
		{"MouthWhistle_NarrowAdjust_Left", "mouthFunnel"},
		{"MouthWhistle_NarrowAdjust_Right", "mouthFunnel"},
		{"NoseScrunch_Left", "noseSneer_L"},
		{"NoseScrunch_Right", "noseSneer_R"},
		{"Smile_Left", "mouthSmile_L"},
		{"Smile_Right", "mouthSmile_R"},
		{"Squint_Left", "eyeSquint_L"},
		{"Squint_Right", "eyeSquint_R"},
		{"TongueUp", "tongueOut"},
		{"UpperLipIn", "mouthRollUpper"},
		{"UpperLipOut", "mouthShrugUpper"},
		{"UpperLipUp_Left", "mouthUpperUp_L"},
		{"UpperLipUp_Right", "mouthUpperUp_R"},
	};

	public static Dictionary<string, string> faceCapBlendShapeMap_CC3 = new Dictionary<string, string>()
	{
		{"Mouth_L", "mouthLeft"},
		{"Mouth_Snarl_Lower_L", "mouthLowerDown_L"},
		{"Mouth_Smile_R", "mouthSmile_R"},
		{"Mouth_Up", "mouthClose"},
		{"Brow_Raise_Outer_R", "browOuterUp_R"},
		{"Mouth_Lips_Tuck", null},
		{"Mouth_Dimple_L", "mouthDimple_L"},
		{"Mouth_Bottom_Lip_Trans", "jawForward"},
		{"Mouth_Plosive", "mouthShrugLower"},
		{"Eye_Wide_R", "eyeWide_R"},
		{"Brow_Raise_Inner_L", "browInnerUp"},
		{"Mouth_Frown", null},
		{"Eye_Squint_L", "eyeSquint_L"},
		{"Mouth_Bottom_Lip_Down", null},
		{"Mouth_Snarl_Lower_R", "mouthLowerDown_R"},
		{"Dental_Lip", null},
		{"Mouth_Frown_R", "mouthFrown_R"},
		{"Mouth_Lips_Part", null},
		{"Brow_Drop_L", "browDown_L"},
		{"Mouth_Skewer", null},
		{"Tight-O", null},
		{"Eye_Blink_L", "eyeBlink_L"},
		{"Brow_Raise_Inner_R", "browInnerUp"},
		{"Cheek_Blow_R", "cheekPuff"},
		{"Nose_Nostrils_Flare", null},
		{"Cheek_Blow_L", "cheekPuff"},
		{"Cheek_Suck", null},
		{"Mouth_Top_Lip_Under", "mouthRollUpper"},
		{"Mouth_Lips_Tight", null},
		{"Mouth_Open", null},
		{"Brow_Raise_Outer_L", "browOuterUp_L"},
		{"Nose_Flank_Raise_L", "noseSneer_L"},
		{"Open", "jawOpen"},
		{"Mouth_Bottom_Lip_Under", "mouthRollLower"},
		{"Mouth_Snarl_Upper_L", "mouthUpperUp_L"},
		{"Brow_Drop_R", "browDown_R"},
		{"Nose_Scrunch", null},
		{"Nose_Flanks_Raise", null},
		{"Cheek_Raise_L", "cheekSquint_L"},
		{"Eye_Blink_R", "eyeBlink_R"},
		{"Brow_Raise_L", null},
		{"Cheek_Raise_R", "cheekSquint_R"},
		{"Mouth_Pucker_Open", null},
		{"Lip_Open", null},
		{"Mouth_Lips_Jaw_Adjust", null},
		{"Mouth_Frown_L", "mouthFrown_L"},
		{"Mouth_Lips_Open", null},
		{"Mouth_Widen", null},
		{"Mouth_Snarl_Upper_R", "mouthUpperUp_R"},
		{"Eye_Wide_L", "eyeWide_L"},
		{"Wide", null},
		{"Mouth_Smile_L", "mouthSmile_L"},
		{"Eye_Squint_R", "eyeSquint_R"},
		{"Mouth_Blow", null},
		{"Mouth_Pucker", "mouthPucker"},
		{"Explosive", null},
		{"Mouth_Bottom_Lip_Bite", "mouthShrugLower"},
		{"Mouth_R", "mouthRight"},
		{"Tight", null},
		{"Mouth_Widen_Sides", "mouthStretch_L"},
		{"Brow_Raise_R", null},
		{"Affricate", "mouthFunnel"},
		{"Mouth_Smile", null},
		{"Mouth_Down", null},
		{"Mouth_Dimple_R", "mouthDimple_R"},
		{"Eye_Blink", null},
		{"Mouth_Top_Lip_Up", "mouthShrugUpper"},
		{"Nose_Flank_Raise_R", "noseSneer_R"},
		{"Tongue_Curl-D", null},
		{"Tongue_up", "tongueOut"},
		{"Tongue_Narrow", null},
		{"Tongue_Out", "tongueOut"},
		{"Tongue_Lower", null},
		{"Tongue_Raise", null},
		{"Tongue_Curl-U", null},
	};

	void OnEnable()
	{
		FacecapLinker linkerVisemes = (FacecapLinker) target;
		ArmatureLinker linkerArmature = linkerVisemes.GetComponentInChildren<ArmatureLinker>();

		var blendShapCount = GetMeshBlendNames(linkerArmature).Length;
		if (linkerVisemes.facecapIndexes.Length != blendShapCount)
			linkerVisemes.facecapIndexes = new int[GetMeshBlendNames(linkerArmature).Length];

		sizes = Enumerable.Range(0, facecapNames.Length).ToArray();
	}

	private static string[] GetMeshBlendNames(ArmatureLinker linker)
	{
		if (linker.faceRenderer == null)
		{
			return null;
		}

		var mesh = linker.faceRenderer.sharedMesh;
		var blendshapeCount = mesh.blendShapeCount;
		var blendNames = new string[blendshapeCount];
		for (int i = 0; i < mesh.blendShapeCount; ++i)
		{
			blendNames[i] = mesh.GetBlendShapeName(i);
		}

		return blendNames;
	}


	public override void OnInspectorGUI()
	{
		FacecapLinker linker = (FacecapLinker) target;
		ArmatureLinker linkerArmature = linker.GetComponentInChildren<ArmatureLinker>();

		serializedObject.Update();
		if (linkerArmature.faceRenderer != null)
		{
			var blendNames = GetMeshBlendNames(linkerArmature);

			if (GUILayout.Button("Guess Links"))
			{
				if (EditorUtility.DisplayDialog("Guess Facecap Mappings?",
					"This will overwrite any current mappings.", "Ok", "Cancel"))
				{
					GuessMappings(linkerArmature, linker);
				}
			}

			GUILayout.Space(10);

			EditorGUI.indentLevel++;
			for (int i = 0; i < blendNames.Length; ++i)
				BlendNameProperty(linkerArmature, blendNames[i], blendNames, linker, i);
			EditorGUI.indentLevel--;

		}
		else
			EditorGUILayout.LabelField("No \"faceRenderer\" currently set for ArmatureLinker.");

		serializedObject.ApplyModifiedProperties();

		//DrawDefaultInspector();
	}

	public static void GuessMappings(ArmatureLinker linkerArmature, FacecapLinker faceCapLinker)
	{
		if (linkerArmature.faceRenderer)
		{
			faceCapLinker.facecapIndexes = new int[linkerArmature.faceRenderer.sharedMesh.blendShapeCount];

			Dictionary<string, string> facecapBlendShapeMap = null;
			switch (linkerArmature.characterType)
			{
				case ArmatureLinker.CharacterType.DAZ3D_G2:
				case ArmatureLinker.CharacterType.DAZ3D_G3:
				case ArmatureLinker.CharacterType.MANUAL:
				case ArmatureLinker.CharacterType.MAKEHUMAN:
					var blendNames = GetMeshBlendNames(linkerArmature);

					for (int i = 0; i < faceCapLinker.facecapIndexes.Length; ++i)
						faceCapLinker.facecapIndexes[i] = 0;
					for (int i = 0; i < blendNames.Length; ++i)
						for (int n = 0; n < facecapNames.Length; ++n)
							if (blendNames[i].Contains(facecapNames[n]))
								faceCapLinker.facecapIndexes[i] = n;
					break;
				case ArmatureLinker.CharacterType.MIXAMO:
					facecapBlendShapeMap = faceCapBlendShapeMap_Mixamo;
					break;
				case ArmatureLinker.CharacterType.CC3:
					facecapBlendShapeMap = faceCapBlendShapeMap_CC3;
					break;
			}

			if (facecapBlendShapeMap != null)
				foreach (var faceCapBlendShape in facecapBlendShapeMap)
				{
					var skinnedMeshShapeIndex =
						linkerArmature.faceRenderer.sharedMesh.GetBlendShapeIndex(faceCapBlendShape.Key);
					var faceCapIndex = Array.IndexOf(facecapNames, faceCapBlendShape.Value);

					if (faceCapIndex == -1)
						faceCapIndex = 0; //change -1 to 0 so the selection will be 'none'

					if (skinnedMeshShapeIndex == -1)
						continue;

					faceCapLinker.facecapIndexes[skinnedMeshShapeIndex] = faceCapIndex;
				}
		}
	}

	private void BlendNameProperty(ArmatureLinker linker, string name, string[] blendNames,
		FacecapLinker linkerVisemes, int i)
	{

		var shapeWeight = linker.faceRenderer.GetBlendShapeWeight(i);

		EditorGUILayout.BeginHorizontal();
		GUIContent content = new GUIContent()
		{
			text = String.Format("{0}%", shapeWeight),
			tooltip = "Test facecap blendshape."
		};

		if (GUILayout.Button(content, GUILayout.ExpandWidth(false)))
		{
			var isOn = shapeWeight != 0;
			linker.faceRenderer.SetBlendShapeWeight(i, isOn ? 0 : 100);
		}

		linkerVisemes.facecapIndexes[i] =
			EditorGUILayout.IntPopup(name, linkerVisemes.facecapIndexes[i], facecapNames, sizes);

		EditorGUILayout.EndHorizontal();
	}

}
#endif

public class FacecapLinker : MonoBehaviour
{
	public int[] facecapIndexes = new int[] { };
}
