using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class ReverseUIMask : MonoBehaviour, IMaterialModifier
{
    private Image targetImage;
    private Material reverseMaterial;

    private void OnEnable()
    {
        targetImage = GetComponent<Image>();
        targetImage.SetMaterialDirty();
    }

    private void OnDisable()
    {
        ReleaseMaterial();
        if (targetImage != null) targetImage.SetMaterialDirty();
    }

    private void OnDestroy()
    {
        ReleaseMaterial();
    }

    public Material GetModifiedMaterial(Material baseMaterial)
    {
        if (!isActiveAndEnabled || baseMaterial == null) return baseMaterial;

        if (targetImage == null) targetImage = GetComponent<Image>();

        if (!targetImage.maskable)
        {
            ReleaseMaterial();
            return baseMaterial;
        }

        Transform rootCanvas = MaskUtilities.FindRootSortOverrideCanvas(transform);
        int stencilDepth = MaskUtilities.GetStencilDepth(transform, rootCanvas);

        if (stencilDepth <= 0 || stencilDepth > 8)
        {
            ReleaseMaterial();
            return baseMaterial;
        }

        int readMask = (1 << stencilDepth) - 1;
        int stencilReference;
        CompareFunction comparison;

        if (stencilDepth == 1)
        {
            stencilReference = 1;
            comparison = CompareFunction.NotEqual;
        }
        else
        {
            stencilReference = (1 << (stencilDepth - 1)) - 1;
            comparison = CompareFunction.Equal;
        }

        Material newMaterial = StencilMaterial.Add(baseMaterial, stencilReference, StencilOp.Keep, comparison, ColorWriteMask.All, readMask, 0);

        ReleaseMaterial();
        reverseMaterial = newMaterial;

        return reverseMaterial;
    }

    private void ReleaseMaterial()
    {
        if (reverseMaterial == null) return;
        StencilMaterial.Remove(reverseMaterial);
        reverseMaterial = null;
    }
}