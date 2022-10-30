using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Verse
{
    public static class RegionTexture
    {
        public readonly static AtomColor deferredColor = new(255, 0, 255, 255);
        public readonly static AtomColor invalidColor = new(255, 0, 0, 255);
        public readonly static AtomColor emptyColor = new(0, 0, 0, 0);

        public struct OwningRegion : IComponentData
        {
            public Entity region;
        }

        public struct Processing : ISharedComponentData
        {
            public bool active;

            public Processing(bool active) { this.active = active; }
        }

        public static AtomColor GetColorOf(this ComponentLookup<Atom.Color> atomColors, Entity atom)
        {
            if (atom == Entity.Null)
                return emptyColor;

            if (atom.Index < 0)
                return deferredColor;

            return atomColors[atom].value;
        }
    }
}