using System.Collections.Generic;
using UnityEngine;

namespace APS.Automation
{
#if UNITY_EDITOR
	using System;
	using UnityEditor;

	public class BuildAvatar : MonoBehaviour
	{

		public static void CreateEmptyContainers(GameObject real, ArmatureLinker.CharacterType characterType)
		{

			GameObject metarig = null;
			GameObject avatarMesh = null;

			real.SetActive(true);

			if (!real.transform.Find("skinned_mesh") && !real.transform.GetComponent<ArmatureLinker>())
			{

				avatarMesh = new GameObject();

				//var avatarMesh = model.AddComponent<GameObject> ();
				avatarMesh.name = "skinned_mesh";
				avatarMesh.transform.parent = real.transform;

				//If there are any "_keep" bones they must be the last siblings so that they will not interfere with the hierarchy matching the template avatar!! This is very important for animation controller too!!  
				foreach (var subChild in real.GetComponentsInChildren<Transform>())
					if (subChild.name.ToLower().Contains("_keep"))
						subChild.SetAsLastSibling();

				List<Transform> allChildren = new List<Transform>();
				for (int i = 0; i < real.transform.childCount; i++)
				{
					var child = real.transform.GetChild(i);
					allChildren.Add(child);
				}

				string rootBoneName;

				switch (characterType)
				{
					case ArmatureLinker.CharacterType.DAZ3D_G3:
					case ArmatureLinker.CharacterType.DAZ3D_G2:
						rootBoneName = "hip";
						break;
					case ArmatureLinker.CharacterType.MANUAL:
					case ArmatureLinker.CharacterType.MIXAMO:
						rootBoneName = "Hips";
						break;
					case ArmatureLinker.CharacterType.CC3:
						rootBoneName = "Hip"; //"boneroot";
						break;
					case ArmatureLinker.CharacterType.MAKEHUMAN:
					case ArmatureLinker.CharacterType.DEFAULT:
						rootBoneName = "root";
						break;
					case ArmatureLinker.CharacterType.RIGIFY:
						rootBoneName = "spine";
						break;
					default:
						Debug.LogError(
							"Non-templateType detected - unable to determine what to use for root bone naming!");
						return;
				}

				bool wasActiveAnimation = false;

				foreach (Transform child in allChildren)
				{
					if (child.GetComponent<SkinnedMeshRenderer>())
					{
						child.transform.parent = avatarMesh.transform;
					}
					else
					{
						if (child.childCount > 0 && child.GetChild(0).name.ToLower().Equals(rootBoneName.ToLower()))
						{
							//boneroot")) {
							metarig = child.gameObject; //.GetChild (0).gameObject;// child.gameObject;
							//metarig.transform.parent = transform;
							//GameObject.DestroyImmediate (child.gameObject);
						}
					}

					foreach (var skinnedAnimator in child.GetComponentsInChildren<Animator>())
					{
						if (skinnedAnimator.enabled)
							wasActiveAnimation = true;

						skinnedAnimator.enabled = false;
					}

					foreach (var skinnedAnimation in child.GetComponentsInChildren<Animation>())
					{
						if (skinnedAnimation.enabled)
							wasActiveAnimation = true;

						skinnedAnimation.enabled = false;
					}
				}

				if (wasActiveAnimation)
					EditorUtility.DisplayDialog("There Were Animators!",
						"Animator components were found on the skinned mesh!\n\nI did not know what to do so I turned them all off.\n\nYou can use external animators.\n\nBut please do not animate eyes, blinking, facial expressions or the mouth because they will be controlled by APS during recordings.",
						"Ok");
			}

			var linker = real.GetComponentInChildren<ArmatureLinker>();

			if (metarig != null)
			{
				if (linker == null)
				{
					linker = metarig.AddComponent<ArmatureLinker>();
					SetLayerRecursively(linker.gameObject, LayerMask.NameToLayer("Default"));
				}
			}

			if (avatarMesh != null)
			{
				if (real.transform.Find("skinned_mesh") == null)
				{
					Transform t = new GameObject("skinned_mesh").transform;
					t.parent = real.transform;
					t.localPosition = Vector3.zero;
					t.localRotation = Quaternion.identity;

					avatarMesh.transform.parent = t;
				}
			}

			//UpdateMyFormatedName (real, real.name);

			linker.PopulateFields(avatarMesh, characterType);

			/*	
				//Resize the armature to a realistic height
				var headHeight = linker.head.position.y;
				const float idealHeadHeight = 1.5f;
		
				var deltaHeadHeight = headHeight / idealHeadHeight;
		
				var iDeltaHeight = 1f / deltaHeadHeight;
		
				var armature = linker.hip.parent;
				armature.localScale = armature.localScale * iDeltaHeight;
		*/
/*
		if (linker.faceRenderer)
		{
			var faceCapLinker = linker.gameObject.AddComponent<FacecapLinker>();
			FacecapLinkerEditor.GuessMappings(linker, faceCapLinker);

			if (linker.emotionsPrefab)
			{
				var emotionBuilder = linker.emotionsPrefab.gameObject.AddComponent<EmotionBuilderBase>();
				
				switch (characterType)
				{
					case ArmatureLinker.CharacterType.DAZ3D_G2:
					case ArmatureLinker.CharacterType.DAZ3D_G3:
					case ArmatureLinker.CharacterType.RIGIFY:
					case ArmatureLinker.CharacterType.MANUAL:
						break;
					case ArmatureLinker.CharacterType.MIXAMO:
						emotionBuilder.blinkLeftShapeIdx = linker.faceRenderer.sharedMesh.GetBlendShapeIndex("Blink_Left");
						emotionBuilder.blinkRightShapeIdx = linker.faceRenderer.sharedMesh.GetBlendShapeIndex("Blink_Right");
						EmotionBuilderBaseEditor.CreateBlinkAnimation(emotionBuilder, false);//no verbose, if it fails just skip
						break;
					case ArmatureLinker.CharacterType.CC3:
						emotionBuilder.blinkLeftShapeIdx = linker.faceRenderer.sharedMesh.GetBlendShapeIndex("Eye_Blink_L");
						emotionBuilder.blinkRightShapeIdx = linker.faceRenderer.sharedMesh.GetBlendShapeIndex("Eye_Blink_R");
						EmotionBuilderBaseEditor.CreateBlinkAnimation(emotionBuilder, false);//no verbose, if it fails just skip
						break;
				}
			}
			
		}*/
		}



		public static void SetLayerRecursively(GameObject go, int layerNumber)
		{
			foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
			{
				trans.gameObject.layer = layerNumber;
			}
		}

	}
#endif
}