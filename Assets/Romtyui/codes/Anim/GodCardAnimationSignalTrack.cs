using UnityEngine;

public class GodCardAnimationSignalTrack : MonoBehaviour
{
    // =========================================================
    // References
    // =========================================================

    [Header("References")]

    [Tooltip("負責真正廣播訊號的 GodCardAnimationSignalEmitter。")]
    public GodCardAnimationSignalEmitter signalEmitter;


    // =========================================================
    // Animation Signal
    // =========================================================

    [Header("Animation Signal")]

    [Tooltip(
        "這個值是給 Animation Clip 動畫化使用的。\n" +
        "0 = 尚未觸發\n" +
        "1 = 到達 Transform Moment"
    )]
    [Range(0f, 1f)]
    public float transformSignal = 0f;


    // =========================================================
    // Runtime
    // =========================================================

    private float previousTransformSignal;

    private bool transformSent;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        if (signalEmitter == null)
        {
            signalEmitter =
                GetComponent<GodCardAnimationSignalEmitter>();
        }

        if (signalEmitter == null)
        {
            signalEmitter =
                GetComponentInParent<GodCardAnimationSignalEmitter>();
        }

        previousTransformSignal =
            transformSignal;

        transformSent = false;
    }


    // =========================================================
    // On Enable
    // =========================================================

    private void OnEnable()
    {
        /*
         * 每次這個動畫 Prefab 被生成 / 啟用，
         * 都重置一次。
         */

        previousTransformSignal =
            0f;

        transformSent =
            false;
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        /*
         * Animation Clip 會直接修改 transformSignal。
         *
         * 當值從：
         *
         * < 0.5
         *
         * 變成：
         *
         * >= 0.5
         *
         * 就視為到達 Transform Moment。
         */

        bool crossedTransformMoment =
            previousTransformSignal < 0.5f &&
            transformSignal >= 0.5f;


        if (crossedTransformMoment &&
            !transformSent)
        {
            transformSent =
                true;


            Debug.Log(
                $"[GodCardAnimationSignalTrack] " +
                $"到達 Transform Keyframe，" +
                $"Signal = {transformSignal:0.00}"
            );


            if (signalEmitter != null)
            {
                signalEmitter
                    .RaiseTransformMoment();
            }
            else
            {
                Debug.LogWarning(
                    "[GodCardAnimationSignalTrack] " +
                    "找不到 GodCardAnimationSignalEmitter"
                );
            }
        }


        previousTransformSignal =
            transformSignal;
    }


    // =========================================================
    // Reset Signal
    // =========================================================

    public void ResetSignal()
    {
        transformSignal =
            0f;

        previousTransformSignal =
            0f;

        transformSent =
            false;
    }
}