using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlinkingAnimationRandomizer : MonoBehaviour
{
    [SerializeField] private Animation m_animation;

    public Animation animation
    {
        set { m_animation = value; }
    }

    void OnEnable()
    {
        if (m_animation == null)
        {
            Debug.LogError("No animator component connected. " + transform.GetComponentInParent<ArmatureLinker>());
            return;
        }

        m_animation.wrapMode = WrapMode.Once;

        List<string> names = new List<string>();
        foreach (AnimationState a in m_animation)
            names.Add(a.name);

        StartCoroutine(PlayRandomAnimations(names.ToArray()));
    }

    IEnumerator PlayRandomAnimations(params string[] names)
    {
        while (true)
        {
            var next = Random.Range(0, names.Length);
            m_animation.PlayQueued(names[next], QueueMode.PlayNow);
            while (m_animation.isPlaying)
                yield return null;
        }
    }
}