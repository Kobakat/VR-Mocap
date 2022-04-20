using UnityEngine;
using System;
using System.IO;
using System.Text.RegularExpressions;
using APS;
using APS.Automation;

#if UNITY_EDITOR
	using UnityEditor;

	[CustomEditor(typeof(ArmatureLinker))]
	[CanEditMultipleObjects]
	public class ArmatureLinkerEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			ArmatureLinker builder = (ArmatureLinker) target;
			if (GUILayout.Button("RESET JAW"))
			{
				builder.ResetJaw();
			}

			if (builder && builder.characterType == ArmatureLinker.CharacterType.DEFAULT)
				if (GUILayout.Button("Build Avatar Manually"))
				{
					builder.BuildAvatarManually();
				}

			DrawDefaultInspector();
		}
	}
#endif

	public static class TransformDeepChildExtension
	{
		//Breadth-first search
		public static Transform FindDeepChild(this Transform aParent, string aName)
		{
			var result = aParent.Find(aName);
			if (result != null)
			{
				return result;
			}

			foreach (Transform child in aParent)
			{
				result = child.FindDeepChild(aName);
				if (result != null)
				{
					return result;
				}
			}

			return null;
		}
	}


	public class ArmatureLinker : MonoBehaviour
	{
#if UNITY_EDITOR
		
		public void ResetJaw()
		{
			var j = restPose.transform.FindDeepChild("RestPoseRig_" + jaw.name);
			if (j)
			{
				iJawRotation = jaw.localRotation = j.localRotation;
				iJawPosition = jaw.localPosition = j.localPosition;
			}
		}
		
		public static void GenerateDefaultEmotions(ArmatureLinker linker)
		{
			if (linker.faceRenderer == null)
				return;

			String expressionsFolder = "";

			switch (linker.characterType)
			{
				case ArmatureLinker.CharacterType.MANUAL:
				case ArmatureLinker.CharacterType.RIGIFY:
				case ArmatureLinker.CharacterType.DAZ3D_G3:
				case ArmatureLinker.CharacterType.DAZ3D_G2:
					break;
				case ArmatureLinker.CharacterType.MAKEHUMAN:
					expressionsFolder = "ExpressionsMakehuman";
					break;
				case ArmatureLinker.CharacterType.CC3:
					expressionsFolder = "ExpressionsCC3";
					break;
				case ArmatureLinker.CharacterType.MIXAMO:
					expressionsFolder = "ExpressionMixamo";
					break;
			}

			if (!String.IsNullOrEmpty(expressionsFolder))
			{
				//var emotions = linker.emotionsPrefab.GetComponentInChildren<EmotionThumbnail>();

				var textFile = Resources.LoadAll<TextAsset>(expressionsFolder + '/');
				foreach (var asset in textFile)
				{
					ExpressionTemplateJsonObject expressionTemplate =
						JsonUtility.FromJson<ExpressionTemplateJsonObject>(asset.text);
					//expressionTemplate.templateName = asset.name;

					EmotionBuilder emotionBuilder = linker.emotionsPrefab.AddComponent<EmotionBuilder>();
					emotionBuilder.emotionName = asset.name;

					/*for (int i = 0; i < expressionTemplate.template.Length; i++)
					{
						var unit = expressionTemplate.template[i];
						Debug.Log(unit.name + " " + unit.val);
						
						//emotionBuilder.blendShapeWeights[i] = 
					}*/

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

					//EmotionBuilderEditor.SetWeights(emotionBuilder);
					EmotionBuilderBaseEditor.SaveChanges(emotionBuilder);
				}
			}

			AssetBuilder.ResetAllBlendshapesToZero();
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
			//public string templateName;
			public ExpressionTemplateJsonUnit[] template;
			public Vector3 jawRotation;
		}

		public static void LookAtObject(Transform lookObject, Transform lookAtPoint)
		{
			//make the lookObject look at the lookAtPoint (might be used for a self portrait camera)
			if (lookObject == null || lookAtPoint == null)
				return;

			lookObject.position = new Vector3( //just a rough estimate, but the user can adjust the camera later..
				lookObject.position.x,
				lookAtPoint.position.y,
				lookObject.position.z
			);
		}

		public static void RemoveMissingScripts(GameObject go)
		{
			int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
			if (count > 0)
			{
				// Edit: use undo record object, since undo destroy wont work with missing
				Undo.RegisterCompleteObjectUndo(go, "Remove missing scripts");
				GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
			}

			foreach (Transform childT in go.transform)
			{
				RemoveMissingScripts(childT.gameObject);
			}
		}

		public static GameObject AddEmotionsPrefab(Transform real, Transform headBone)
		{
			var emotionCameraPrefabPath = "Assets/APS_SDK/Prefabs/emotions.prefab";
			UnityEngine.Object emotionCameraPrefab =
				AssetDatabase.LoadAssetAtPath(emotionCameraPrefabPath, typeof(GameObject));
			GameObject emotionCameraObject = Instantiate(emotionCameraPrefab, real) as GameObject;
			emotionCameraObject.name = emotionCameraObject.name.ToLower().Replace("(clone)", "").Trim();

			var thumbnailCamera = emotionCameraObject.GetComponentInChildren<EmotionThumbnail>();
			if (thumbnailCamera)
			{
				LookAtObject(thumbnailCamera.transform, headBone);
			}

			return emotionCameraObject;
		}
		
		public void BuildAvatarManually()
		{
			ArmatureLinker builder = (ArmatureLinker) FindObjectOfType(typeof(ArmatureLinker));

			if (builder.characterType == (int) ArmatureLinker.CharacterType.DEFAULT)
				builder.characterType = ArmatureLinker.CharacterType.MANUAL;

			AssetBuilder.FixPlayerSettings();
			AssetBuilder.ResetAllBlendshapesToZero();

			var animator = builder.GetComponentInParent<Animator>();
			if (animator == null)
			{
				EditorUtility.DisplayDialog("No Animator Component", "please ensure to place the APS_SDK on the same object containing the Animator component and that you have selected \"Humanoid\" as the animation type for the .fbx.", "Ok");
				
				DestroyImmediate(this);
				return;
			}

			var prefab = animator.gameObject;

			prefab.name = prefab.name.Replace('.', '_'); //replace any periods with underscores to prevent export file naming corruption
			
			if (Regex.IsMatch(prefab.name,
				@"[{(]?[0-9A-Fa-f$]{8}[-]?(?:[0-9A-Fa-f]{4}[-]?){3}[0-9A-Fa-f]{12}[)}]?"))
			{
				//prevent names from getting very long if there is already a unique GUID then no need to add another one
				if (prefab.name.StartsWith(string.Format("{0}$", APS.Automation.AssetPostprocessor.AssetBundleVariant)))
				{
					return;
				}
			}

			GameObject real = Instantiate(prefab);

			real.transform.localScale = Vector3.one;

			RemoveMissingScripts(real);

			var _animator = real.GetComponent<Animator>();

			var defaultController = Resources.Load<RuntimeAnimatorController>("DefaultAnimationController");
			if (defaultController != null)
			{
				_animator.runtimeAnimatorController = defaultController;
			}

			BuildAvatar.CreateEmptyContainers(real, builder.characterType);

			var assetPath = AssetDatabase.GetAssetPath(PrefabUtility.GetPrefabParent(prefab));

			var restPose =
				APS.Automation.AssetPostprocessor.CreateRestPoseRig(real,
					assetPath); //this is a game object that we can re-arange and change parenting or objects, then save as the original prefab later on;
			real.GetComponentInChildren<ArmatureLinker>().restPose = restPose;

			foreach (var transform in restPose.GetComponentsInChildren<Transform>())
			{
				transform.name =
					"RestPoseRig_" +
					transform
						.name; //must rename each bone so the seriously bugged Unity animator does not try to use them instead of the actual character!! weird 
			}

			restPose.name = "RestPoseRig";


			//Resize the armature to a realistic height
			var linker = real.GetComponentInChildren<ArmatureLinker>();
			var headHeight = linker.head.position.y;
			const float idealHeadHeight = 1.5f;

			var deltaHeadHeight = headHeight / idealHeadHeight;

			var iDeltaHeight = 1f / deltaHeadHeight;

			var armature = linker.hip.parent;
			armature.localScale = armature.localScale * iDeltaHeight;
			
			builder.emotionsPrefab = AddEmotionsPrefab(real.transform, builder.head);
			if (builder.emotionsPrefab && builder.emotionsPrefab.GetComponent<EmotionBuilderBase>() == null)
				builder.emotionsPrefab.AddComponent<EmotionBuilderBase>();
			
			////GenerateDefaultEmotions(builder);

			/*
			var emotionCameraPrefabPath = "Assets/AnimPrep/Prefabs/emotions.prefab";
			UnityEngine.Object emotionCameraPrefab = AssetDatabase.LoadAssetAtPath(emotionCameraPrefabPath, typeof(GameObject));
			GameObject emotionCameraObject = Instantiate(emotionCameraPrefab, real.transform) as GameObject;
			emotionCameraObject.name = emotionCameraObject.name.ToLower().Replace("(clone)","").Trim();

			var thumbnailCamera = emotionCameraObject.GetComponentInChildren<EmotionThumbnail>();
			if (thumbnailCamera)
			{
				ArmatureLinker.LookAtObject(thumbnailCamera.transform, builder.head);
			}
			*/
			RendererShaderParams.StoreAllRenderers(real);

			Guid guid = Guid.NewGuid(); //make each "*.vap" unique by adding a unique guid id
			var assetName = APS.Automation.AssetPostprocessor.AssetBundleVariant + "$" + guid + "$" + prefab.name;

			var prefabPath = String.Concat(Path.Combine(APS.Automation.AssetPostprocessor.prefabsFolder, assetName),
				".prefab");

			PrefabUtility.SaveAsPrefabAssetAndConnect(real, prefabPath, InteractionMode.AutomatedAction);

			var assetImport = AssetImporter.GetAtPath(prefabPath);
			assetImport.SetAssetBundleNameAndVariant(assetName, APS.Automation.AssetPostprocessor.AssetBundleVariant);

			//real.transform.localScale = Vector3.one;



			string assetDestFolder = Path.Combine(APS.Automation.AssetPostprocessor.assetBundlesFolder, assetName.ToLower());

			var fbxFolder = Path.Combine(assetDestFolder, "model_data");
			if (Directory.Exists(Path.GetDirectoryName(fbxFolder)))
				Directory.Delete(fbxFolder);
			Directory.CreateDirectory(fbxFolder);

			//move any used textures into the fbx folder
			foreach (var renderer in real.GetComponentsInChildren<Renderer>())
			foreach (var material in renderer.sharedMaterials)
			{
				if (material == null)
					continue;
				
				Shader shader = material.shader;
				for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
				{
					if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
					{
						Texture texture = material.GetTexture(ShaderUtil.GetPropertyName(shader, i));

						var texturePath = AssetDatabase.GetAssetPath(texture);
						if (string.IsNullOrEmpty(texturePath))
							continue;

						var toTexture = Path.Combine(fbxFolder, Path.GetFileName(texturePath));
						if (File.Exists(toTexture))
							continue;

						FileUtil.CopyFileOrDirectory(texturePath, toTexture);
					}
				}
			}

			var fbxPath = AssetDatabase.GetAssetPath(animator.avatar);

			var to = Path.Combine(fbxFolder, assetName + Path.GetExtension(fbxPath));

			FileUtil.CopyFileOrDirectory(fbxPath, to);

			/*ZipFile.Compress (new FileInfo(to));
			
			if (File.Exists (to)) 
				File.Delete (to);*/

			DestroyImmediate(prefab);
			DestroyImmediate(real);

			try
			{
				AssetDatabase.Refresh();
			}
			catch{}
			BuildScript.BuildAssetBundles();
			try
			{
				AssetDatabase.Refresh();
			}
			catch{}


			UnityEngine.Object prefab2 = AssetDatabase.LoadMainAssetAtPath(prefabPath);
			PrefabUtility.InstantiatePrefab(prefab2 as GameObject); //show the new prefab in the scene

			AssetBuilder.ShowAssetBundlesExplorer();

			Debug.Log(string.Format("Created new avatar: {0}", assetName.ToLower()));

			if (_animator)
				_animator.enabled = true;
		}
#endif
		
		[HideInInspector] public GameObject emotionsPrefab;

		public enum CharacterType
		{
			DEFAULT,
			CC3,
			MAKEHUMAN,
			MIXAMO,
			DAZ3D_G2,
			DAZ3D_G3,
			MANUAL,
			RIGIFY,
		};

		[HideInInspector] public CharacterType characterType = CharacterType.DEFAULT;

		[Header("_Armature_")]
		string
			defaultModel =
				"Not_Yet_Set"; //a reference to an actual assetBundle so it's animations can be saved and loaded (Note if the default (non-assetbundle) model is used THIS WILL HAVE TO BE SET in the inspector field manually).

		
		[Header("Facial Expressions:")]
		public SkinnedMeshRenderer faceRenderer; //the renderer that has the eyelid blend shapes



		//public Transform root;
		[Header("_Torso_")] public Transform hip;
		public Transform spine;
		public Transform chest;

		[Header("_Upper_")] public Transform neck;
		public Transform head;

		//[Header("_Breasts_")]
		//public Transform breastL;
		//public Transform breastR;
		//public Rigidbody chestRb;

		[Header("_Face Rig (Optional)_")] public Transform eyeL;
		public Transform eyelidL;
		public Transform eyeR;
		public Transform eyelidR;
		public Transform jaw;

		[Header("_Right Arm_")] public Transform shoulderR;
		public Transform upper_armR;
		public Transform forearmR;


		[Header("_Left Arm_")] public Transform shoulderL;
		public Transform upper_armL;
		public Transform forearmL;


		[Header("_Right Leg_")] public Transform thighR;
		public Transform shinR;
		public Transform footR;
		public Transform toeR;

		[Header("_Left Leg_")] public Transform thighL;
		public Transform shinL;
		public Transform footL;
		public Transform toeL;




		[Header("_Right Hand_")] public Transform handR;
		[Header("_Right Fingers_")] public Transform indexR;
		public Transform middleR;
		public Transform ringR;
		public Transform pinkyR;
		public Transform thumbR;

		[Header("_Left Hand_")] public Transform handL;
		[Header("_Left Fingers_")] public Transform indexL;
		public Transform middleL;
		public Transform ringL;
		public Transform pinkyL;
		public Transform thumbL;

		//[HideInInspector]
		public GameObject restPose;

		//Makehuman facial expressions only
		[System.Serializable]
		public struct Driver
		{
			public float c; //constant
			public string v; //variable
		}

		[System.Serializable]
		public struct DriverAxis //scripted expressions from mhx blender driver
		{
			public Driver[] x;
			public Driver[] y;
			public Driver[] z;
		}


		[System.Serializable]
		public struct ExpessionsJson
		{
			public Transform bone;
			public string bone_name; //facial rig bone
			public DriverAxis drivers;
		}
		
		[HideInInspector]
		[Space(30)] public ExpessionsJson[] expressionsData;
		
		
		Transform GetBoneTransform(HumanBodyBones humanBone)
		{
			return GetComponentInParent<Animator>().GetBoneTransform(humanBone);
			//return transform.FindDeepChild(MakehumanMappings.mappings.human [System.Array.IndexOf (MakehumanMappings.mappings.boneType, humanBone)].boneName);
		}
		
		[Serializable]
		public struct BlendShapeParams
		{
			public string
				shapeName; //only here (and public) so as to expose this field to the inspector when creating a list of template objects.

			public float shapeWeight;
		}
		
		[HideInInspector]
		public BlendShapeParams[] faceRenderersParams;

		public Quaternion iJawRotation = Quaternion.identity;
		public Vector3 iJawPosition = Vector3.zero;

		public void PopulateFields(GameObject avatarMesh, CharacterType _characterType)
		{
			characterType = _characterType;

			hip = GetBoneTransform(HumanBodyBones.Hips); // this.transform;//
			chest = GetBoneTransform(HumanBodyBones.Chest);
			spine = GetBoneTransform(HumanBodyBones.Spine);

			neck = GetBoneTransform(HumanBodyBones.Neck);
			head = GetBoneTransform(HumanBodyBones.Head);

			jaw = GetBoneTransform(HumanBodyBones.Jaw);
			eyeL = GetBoneTransform(HumanBodyBones.LeftEye);
			eyeR = GetBoneTransform(HumanBodyBones.RightEye);

			shoulderL = GetBoneTransform(HumanBodyBones.LeftShoulder);
			upper_armL = GetBoneTransform(HumanBodyBones.LeftUpperArm);
			forearmL = GetBoneTransform(HumanBodyBones.LeftLowerArm);
			handL = GetBoneTransform(HumanBodyBones.LeftHand);

			shoulderR = GetBoneTransform(HumanBodyBones.RightShoulder);
			upper_armR = GetBoneTransform(HumanBodyBones.RightUpperArm);
			forearmR = GetBoneTransform(HumanBodyBones.RightLowerArm);
			handR = GetBoneTransform(HumanBodyBones.RightHand);

			thighL = GetBoneTransform(HumanBodyBones.LeftUpperLeg);
			shinL = GetBoneTransform(HumanBodyBones.LeftLowerLeg);
			footL = GetBoneTransform(HumanBodyBones.LeftFoot);
			toeL = GetBoneTransform(HumanBodyBones.LeftToes);

			thighR = GetBoneTransform(HumanBodyBones.RightUpperLeg);
			shinR = GetBoneTransform(HumanBodyBones.RightLowerLeg);
			footR = GetBoneTransform(HumanBodyBones.RightFoot);
			toeR = GetBoneTransform(HumanBodyBones.RightToes);

			indexL = GetBoneTransform(HumanBodyBones.LeftIndexProximal);
			middleL = GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
			ringL = GetBoneTransform(HumanBodyBones.LeftRingProximal);
			pinkyL = GetBoneTransform(HumanBodyBones.LeftLittleProximal);
			thumbL = GetBoneTransform(HumanBodyBones.LeftThumbProximal);

			indexR = GetBoneTransform(HumanBodyBones.RightIndexProximal);
			middleR = GetBoneTransform(HumanBodyBones.RightMiddleProximal);
			ringR = GetBoneTransform(HumanBodyBones.RightRingProximal);
			pinkyR = GetBoneTransform(HumanBodyBones.RightLittleProximal);
			thumbR = GetBoneTransform(HumanBodyBones.RightThumbProximal);

			switch (characterType)
			{
				case CharacterType.DAZ3D_G2:
				case CharacterType.DAZ3D_G3:
					faceRenderersParams = faceRenderersParams_Daz3D;
					break;
				case CharacterType.MANUAL:
					faceRenderersParams = faceRenderersParams_CC3;
					break;
				case CharacterType.MIXAMO:
					faceRenderersParams = faceRenderersParams_Mixamo;
					break;
				case CharacterType.CC3:
					faceRenderersParams = faceRenderersParams_CC3;
					break;
				case CharacterType.MAKEHUMAN:
					if (eyeL != null)
					{
						//Must be a makehuman character for this to work.
						var _eyelidL = eyeL.parent.Find("orbicularis03.L"); //From makehuman default skeleton
						if (_eyelidL != null)
						{
							eyelidL = _eyelidL;
						}
					}

					if (eyeR != null)
					{
						//Must be a makehuman character for this to work.
						var _eyelidR = eyeR.parent.Find("orbicularis03.R"); //From makehuman default skeleton
						if (_eyelidR != null)
						{
							eyelidR = _eyelidR;
						}
					}

					faceRenderersParams = faceRenderersParams_MakeHuman;
					break;
				case CharacterType.RIGIFY:
				default:
					faceRenderersParams = null;
					Debug.LogError("Non-templateType detected - unable to determine which blendshapes naming to use!");
					return;
			}

			if (jaw != null)
			{
				iJawRotation = jaw.localRotation;
				iJawPosition = jaw.localPosition;
			}


			ApplyFaceController(avatarMesh, faceRenderersParams);

		}

		//DAZ facerig (and weights are for vocalizer expressions)
		public static BlendShapeParams[] faceRenderersParams_Daz3D = new BlendShapeParams[]
		{

		};


		//Mixamo facerig (and weights are for vocalizer expressions)
		public static BlendShapeParams[] faceRenderersParams_Mixamo = new BlendShapeParams[]
		{
			new BlendShapeParams() {shapeName = "BrowsDown_Left", shapeWeight = 2.5f},
			new BlendShapeParams() {shapeName = "BrowsDown_Right", shapeWeight = 2.5f},

			new BlendShapeParams() {shapeName = "Squint_Left", shapeWeight = 2.0f},
			new BlendShapeParams() {shapeName = "Squint_Right", shapeWeight = 2.0f},

			new BlendShapeParams() {shapeName = "CheekPuff_Left", shapeWeight = 0.8f},
			new BlendShapeParams() {shapeName = "CheekPuff_Right", shapeWeight = 0.8f},

			new BlendShapeParams() {shapeName = "Smile_Left", shapeWeight = 5.0f},
			new BlendShapeParams() {shapeName = "Smile_Right", shapeWeight = 5.0f},

			new BlendShapeParams() {shapeName = "MouthWhistle_NarrowAdjust_Left", shapeWeight = 2.0f},
			new BlendShapeParams() {shapeName = "MouthWhistle_NarrowAdjust_Right", shapeWeight = 2.0f},

			new BlendShapeParams() {shapeName = "LowerLipDown_Left", shapeWeight = 2.0f},
			new BlendShapeParams() {shapeName = "LowerLipDown_Right", shapeWeight = 2.0f},

			new BlendShapeParams() {shapeName = "UpperLipUp_Left", shapeWeight = 1.0f},
			new BlendShapeParams() {shapeName = "UpperLipUp_Right", shapeWeight = 1.0f},
		};

		//CC3 facerig (and weights are for vocalizer expressions)
		public static BlendShapeParams[] faceRenderersParams_CC3 = new BlendShapeParams[]
		{
			new BlendShapeParams() {shapeName = "Brow_Drop_L", shapeWeight = 2.5f},
			new BlendShapeParams() {shapeName = "Brow_Drop_R", shapeWeight = 2.5f},

			new BlendShapeParams() {shapeName = "Eye_Squint_L", shapeWeight = 2.0f},
			new BlendShapeParams() {shapeName = "Eye_Squint_R", shapeWeight = 2.0f},

			new BlendShapeParams() {shapeName = "Cheek_Blow_L", shapeWeight = 0.8f},
			new BlendShapeParams() {shapeName = "Cheek_Blow_R", shapeWeight = 0.8f},

			new BlendShapeParams() {shapeName = "Cheek_Raise_L", shapeWeight = 1.0f},
			new BlendShapeParams() {shapeName = "Cheek_Raise_R", shapeWeight = 1.0f},

			new BlendShapeParams() {shapeName = "Mouth_Smile_L", shapeWeight = 5.0f},
			new BlendShapeParams() {shapeName = "Mouth_Smile_R", shapeWeight = 5.0f},

			new BlendShapeParams() {shapeName = "Lip_Open", shapeWeight = 7.5f},
			new BlendShapeParams() {shapeName = "Mouth_Top_Lip_Up", shapeWeight = 5.0f},
		};


		//MakeHuman facerig (and weights are for vocalizer expressions)
		public static BlendShapeParams[] faceRenderersParams_MakeHuman = new BlendShapeParams[]
		{
			new BlendShapeParams() {shapeName = "brow_mid_down_left", shapeWeight = 2.5f},
			new BlendShapeParams() {shapeName = "brow_mid_down_right", shapeWeight = 2.5f},

			new BlendShapeParams() {shapeName = "cheek_squint_left", shapeWeight = 2.0f},
			new BlendShapeParams() {shapeName = "cheek_squint_right", shapeWeight = 2.0f},

			new BlendShapeParams() {shapeName = "cheek_balloon_left", shapeWeight = 0.8f},
			new BlendShapeParams() {shapeName = "cheek_balloon_right", shapeWeight = 0.8f},

			new BlendShapeParams() {shapeName = "cheek_up_left", shapeWeight = 1.0f},
			new BlendShapeParams() {shapeName = "cheek_up_right", shapeWeight = 1.0f},

			new BlendShapeParams() {shapeName = "mouth_corner_in_left", shapeWeight = 1.0f},
			new BlendShapeParams() {shapeName = "mouth_corner_in_right", shapeWeight = 1.0f},

			new BlendShapeParams() {shapeName = "mouth_corner_up_left", shapeWeight = 5.0f},
			new BlendShapeParams() {shapeName = "mouth_corner_up_right", shapeWeight = 5.0f},

			new BlendShapeParams() {shapeName = "mouth_wide_left", shapeWeight = 15.0f},
			new BlendShapeParams() {shapeName = "mouth_wide_right", shapeWeight = 15.0f},

			new BlendShapeParams() {shapeName = "lips_part", shapeWeight = 7.5f},
			new BlendShapeParams() {shapeName = "lips_upper_in", shapeWeight = 5.0f},
		};


		public void ApplyFaceController(GameObject armatureMesh, BlendShapeParams[] faceRenderersParams)
		{
			//iterate through all blendshapes in each mesh until a mesh with all blendshapes is found that match the BlendShapeParams array from the template (which is the mesh for face renderer)
			if (armatureMesh == null)
				return;
			
			foreach (Transform t in armatureMesh.transform)
			{
				SkinnedMeshRenderer mesh = t.GetComponent<SkinnedMeshRenderer>();
				if (mesh == null)
				{
					continue;
				}

				bool state = mesh.sharedMesh.blendShapeCount > 0;
				foreach (BlendShapeParams blendShape in faceRenderersParams)
				{
					state = state && (mesh.sharedMesh.GetBlendShapeIndex(blendShape.shapeName) != -1);
					//Debug.Log("STATE " + state + " --- " + blendShape.shapeName);
					if (!state)
					{
						break;
					}
				}

				if (state == true)
				{
					//if all blendshapes were present
					faceRenderer = mesh;
					break;
				}
			}
		}

	}
