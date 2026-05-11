using System.Collections;
using UnityEngine;

public class HitFlashObject : MonoBehaviour
{
    [Header("Hit Object")]
    public GameObject hitObject;

    [Header("Settings")]
    public float showTime = 0.15f;

    private Coroutine flashRoutine;

    private void Awake()
    {
        if (hitObject != null)
            hitObject.SetActive(false);
    }

    public void Play()
    {
        if (hitObject == null)
        {
            Debug.LogWarning("[HitFlashObject] hitObject 沒有指定");
            return;
        }

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        hitObject.SetActive(true);

        yield return new WaitForSeconds(showTime);

        hitObject.SetActive(false);

        flashRoutine = null;
    }
}