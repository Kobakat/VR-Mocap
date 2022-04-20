using System.Collections;
using UnityEngine;

/// <summary>
/// Simple script that can be used to play random audio clips (eg. this was used on the geiger counter prop).
/// </summary>
public class RandomAudioClipPlayer : MonoBehaviour
{

    public float randomTimeLow = 1f;
    public float randomTimeHigh = 5f;

    [SerializeField] AudioSource m_audioSource;
    [SerializeField] AudioClip[] m_clips;

    void Start()
    {
        if (m_audioSource == null)
            m_audioSource = gameObject.GetComponentInChildren<AudioSource>();
        if (m_audioSource == null)
            m_audioSource = gameObject.AddComponent<AudioSource>();

        StartCoroutine(PlaySound());
    }

    IEnumerator PlaySound()
    {
        yield return new WaitForSeconds(Random.Range(randomTimeLow, randomTimeHigh));

        var clipIndex = Random.Range(0, m_clips.Length - 1);
        m_audioSource.PlayOneShot(m_clips[clipIndex], 1f);

        yield return new WaitForSeconds(m_clips[clipIndex].length);
        StartCoroutine(PlaySound());
    }
}
