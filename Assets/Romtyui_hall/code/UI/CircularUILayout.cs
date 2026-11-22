using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class CircularUILayout : MonoBehaviour
{
    [SerializeField, Min(0f), Tooltip("圓形排列的半徑，單位為 UI RectTransform 的座標單位。")]
    private float radius = 300f;

    [SerializeField, Tooltip("排列中心相對於此物件中心的偏移。")]
    private Vector2 centerOffset = Vector2.zero;

    [SerializeField, Tooltip("第一個方塊的位置角度。90 為上方，0 為右方，180 為左方，270 為下方。")]
    private float startAngle = 90f;

    [SerializeField, Tooltip("勾選後，依照 Hierarchy 順序沿順時針排列。")]
    private bool clockwise = true;

    [SerializeField, Tooltip("讓方塊隨著圓形位置旋轉，使方塊頂端朝向圓外。")]
    private bool rotateOutward = true;

    [SerializeField, Tooltip("方塊額外旋轉的角度。")]
    private float rotationOffset = 0f;

    private bool layoutDirty = true;

    private void OnEnable()
    {
        layoutDirty = true;
    }

    private void OnValidate()
    {
        layoutDirty = true;
    }

    private void OnTransformChildrenChanged()
    {
        layoutDirty = true;
    }

    private void Update()
    {
        if (!layoutDirty) return;

        layoutDirty = false;
        Arrange();
    }

    [ContextMenu("重新排列方塊")]
    public void Arrange()
    {
        int count = 0;

        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i) is RectTransform) count++;
        }

        if (count == 0) return;

        float angleStep = 360f / count;
        float direction = clockwise ? -1f : 1f;
        int index = 0;

        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;
            if (child == null) continue;

            float angle = startAngle + angleStep * index * direction;
            float radians = angle * Mathf.Deg2Rad;

            child.anchorMin = new Vector2(0.5f, 0.5f);
            child.anchorMax = new Vector2(0.5f, 0.5f);
            child.pivot = new Vector2(0.5f, 0.5f);
            child.anchoredPosition = centerOffset + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
            child.localRotation = Quaternion.Euler(0f, 0f, (rotateOutward ? angle - 90f : 0f) + rotationOffset);

            index++;
        }
    }
}