using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Verse
{
	public class AtomDataAuthoring : MonoBehaviour
	{
		public class Baker : Baker<AtomDataAuthoring>
		{
			public override void Bake(AtomDataAuthoring authoring)
			{
				AddComponent<Atom.Matter>();
				AddComponent<Atom.Color>();
				AddComponent<Atom.Temperature>();
			}
		}
	}
}