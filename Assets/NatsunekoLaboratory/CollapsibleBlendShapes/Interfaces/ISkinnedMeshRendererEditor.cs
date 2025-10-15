using Refractions.Attributes;

using UnityEditor;

using Object = UnityEngine.Object;

namespace NatsunekoLaboratory.CollapsibleBlendShapes.Interfaces
{
    public interface ISkinnedMeshRendererEditor
    {
        [Public]
        [Instance]
        Object target { get; }

        [NonPublic]
        [Instance]
        [Field]
        SerializedProperty m_BlendShapeWeights { get; }
    }
}
