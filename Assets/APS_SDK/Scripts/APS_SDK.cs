using UnityEngine;

namespace APS
{

#if UNITY_EDITOR
	using UnityEditor;

	[CustomEditor(typeof(APS_SDK))]
	public class APS_SDK_Editor : Editor
	{
		
		public override void OnInspectorGUI()
		{
			APS_SDK sdk = (APS_SDK) FindObjectOfType(typeof(APS_SDK));

			EditorGUILayout.LabelField("Configure this avatar for APS:");
			GUIStyle customLabel = new GUIStyle("Button");
			

			customLabel.fixedWidth = 100;
			customLabel = new GUIStyle("Button");
			customLabel.alignment = TextAnchor.MiddleCenter;
			customLabel.fontSize = 14;
			customLabel.normal.textColor = new Color(0.2f, 0.7f, 1.0f);
			customLabel.fontStyle = FontStyle.Bold;
			
			if (GUILayout.Button(
				new GUIContent("Build Avatar Manually",
					"This adds the ArmatureLinker and attempts to populate all fields.\n\nPlease check that the Unity humanoid config for the avatar's .fbx looks correct prior to building.\n\nImportant Note: Whenever changes are made you will need to press \"Re-Build Assetbundles\" to update the asset prior to copying to APS's VR_MocapAssets folder."),
				customLabel))
			{
				ArmatureLinker armatureLinker = sdk.gameObject.GetComponentInChildren<ArmatureLinker>();
				if (armatureLinker == null)
				{
					armatureLinker = sdk.gameObject.AddComponent<ArmatureLinker>();
					armatureLinker.characterType = ArmatureLinker.CharacterType.MANUAL; //create a new avatar manually.
					armatureLinker.BuildAvatarManually();
				}
				else
					Debug.Log("This avatar already has an ArmatureLinker component.");
			}

			//DrawDefaultInspector();
		}

	}
#endif
	[DisallowMultipleComponent]
	public class APS_SDK : MonoBehaviour
	{
		//Placeholder
	}
}