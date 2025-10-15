using Refractions.Attributes;

using UnityEngine;

namespace NatsunekoLaboratory.CollapsibleBlendShapes.Interfaces
{
    public interface ISkinnedMeshRendererEditor_Styles
    {
        [Public]
        [Static]
        [Field]
        GUIContent legacyClampBlendShapeWeightsInfo { get; }
    }
}
