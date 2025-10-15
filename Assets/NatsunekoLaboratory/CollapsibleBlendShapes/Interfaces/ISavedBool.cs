using Refractions.Attributes;

namespace NatsunekoLaboratory.CollapsibleBlendShapes.Interfaces
{
    internal interface ISavedBool
    {
        [Public]
        [Constructor]
        [Instance]
        void Constructor(string name, bool value);

        [Public]
        [Instance]
        bool value { get; set; }
    }
}
