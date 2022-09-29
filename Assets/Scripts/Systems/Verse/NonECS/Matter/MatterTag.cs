using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Verse
{
	[CreateAssetMenu(fileName = "MatterTag", menuName = "APEZ/MatterTag", order = 2)]
	public class MatterTag : ScriptableObject
	{
		[field: SerializeField]
		public string Id { get; private set; } = "";

		[field: SerializeField]
		public MatterTag[] ParentTags { get; private set; } = new MatterTag[0];

		public bool Is(MatterTag tag) => Id.Equals(tag) || ParentTags.Any((MatterTag t) => t.Is(tag));
	}
}