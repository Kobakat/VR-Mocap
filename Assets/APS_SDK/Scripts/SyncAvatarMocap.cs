﻿
using System.Text;
#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using UnityEngine;
using System.Threading;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class SyncAvatarMocap : MonoBehaviour
{
    /// <summary>
    /// APS Avatar Mocap Sync (Threaded HttpClient) V3.1.1
    ///
    /// This script will sync the mocap transforms of your main APS avatar into the realtime Unity editor.
    /// This allows you to see/use your avatars in the editor moving exactly as the main avatar in APS runtime.
    /// This script is compatible with AnimationPrepStudio V2.4.0 and up.
    /// 
    /// How To Use:
    /// First press the "Import Avatar Model" button and build an avatar into the Custom Avatar Builder (if you have not done so already).
    /// Then place this script on the gameobject which contains the animator component (eg. avatar$nnnnnnnn...nnn$my_model_name),
    /// The avatar will then sync with AnimaitonPrepStudio over the local network once you press play. Be sure AnimationPrepStudio is running first!!
    /// Also ensure that windows firewall settings allow AnimationPrepStudio and this editor to communicate with each other.
    ///
    /// Attribution license:
    /// You may use this script for your own purposes but please give attribution to AnimationPrepStudio if you use this script in a production game. 
    /// 
    /// Copyright - Grant Olsen - Animation Prep Studio 2020
    /// </summary>
    
    private HumanPose humanPose;
    private HumanPoseHandler humanPoseHandler;
    
    private Animator m_animator;

    private Transform hip;
    private Transform linker;
    
    AnimationCurve[] curves;
    private string[] muscleName;

    private Quaternion initialHipRotation;

    public string ipAddress = "127.0.0.1";
    const string ipPort = "8080";
    private string urlAddress;
    
    public bool updateEveryFrame = true; //enabling this can prevent jitter when dynamic bones are being used on the avatar.

    void Start()
    {
        m_animator = GetComponent<Animator>();
        m_animator.enabled = false;
        
        humanPose = new HumanPose();
        humanPoseHandler = new HumanPoseHandler(m_animator.avatar, transform);
        
        muscleName = HumanTrait.MuscleName;
        
        curves = new AnimationCurve[muscleName.Length];
        for (int i = 0; i < curves.Length; i++)
            curves[i] = new AnimationCurve();
        
        hip = m_animator.GetBoneTransform(HumanBodyBones.Hips);
        linker = GetComponentInChildren<ArmatureLinker>().transform;
        
        urlAddress = String.Format("http://{0}:{1}/data/muscles/", ipAddress, ipPort);
        
        _threadController = new ThreadController {ShouldExecute = true};
        tAPI = new Thread(RunThreadAPI);
        tAPI.Start(_threadController);

        initialHipRotation = Quaternion.Inverse(hip.parent.rotation);
    }

    void OnDisable()
    {
        if (_threadController != null)
            _threadController.ShouldExecute = false;
        _threadController = null;
    }

    private Animator animatorComponent = null;

    private string[] headerNames = null;
    private float[] headerValues = null;

    public static readonly Concurrent.ConcurrentQueue<Action> RunOnMainThread = new Concurrent.ConcurrentQueue<Action>();
    
    private ThreadController _threadController;
    class ThreadController
    {
        public bool ShouldExecute { get; set; }
    }
    
    private Thread tAPI;
    private HttpClient client;

    void RunThreadAPI(object data)
    {
        var tc = (ThreadController)data;

        using(var client = new HttpClient())
            while (tc.ShouldExecute)
                if (RunOnMainThread.IsEmpty)
                {
                    var text = client.GetStringAsync(new Uri(urlAddress)).Result;
                    if (string.IsNullOrEmpty(text))
                        continue;
                    
                    byte[] bytes = Convert.FromBase64String(text);
             
                    // create a second float array and copy the bytes into it...
                    var mocapData = new float[bytes.Length / sizeof(float)];
                    Buffer.BlockCopy(bytes, 0, mocapData, 0, bytes.Length);

                    RunOnMainThread.Enqueue(() =>
                    {
                        humanPoseHandler.GetHumanPose(ref humanPose);

                        int i;
                        for (i = 0; i < humanPose.muscles.Length; i++)
                            humanPose.muscles[i] = mocapData[i];

                        humanPoseHandler.SetHumanPose(ref humanPose);

                        hip.position = transform.root.TransformPoint(new Vector3(
                            mocapData[i + 0],
                            mocapData[i + 1],
                            mocapData[i + 2]
                        ));

                        hip.localRotation = initialHipRotation * new Quaternion(
                            mocapData[i + 3],
                            mocapData[i + 4],
                            mocapData[i + 5],
                            mocapData[i + 6]
                        );
                        
                        hip.parent.localScale = Vector3.one * mocapData[i + 7];
                    });
                    
                    //Thread.Sleep(50);
                }
                else
                    Thread.Sleep(1);
    }

    Action actionLast;
    void LateUpdate()
    {
        Action action;
        if (updateEveryFrame)
        {
            if(!RunOnMainThread.IsEmpty)
            {
                while (RunOnMainThread.TryDequeue(out action))
                {
                    actionLast = (Action) action.Clone();
                    action.Invoke();
                }
                return;
            }
            if (actionLast != null)
                actionLast.Invoke();
        }
        else
            if(!RunOnMainThread.IsEmpty)
                while (RunOnMainThread.TryDequeue(out action))
                    action.Invoke();
    }
    
    protected static float[] ParseLineData(string line)
    {
        return Array.ConvertAll(line.Trim(',').Split(','), float.Parse);
    }

}
#endif