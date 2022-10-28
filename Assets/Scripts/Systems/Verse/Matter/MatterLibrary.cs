using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using System.Linq;

namespace Verse
{
	public class MatterLibrary : MonoSingleton<MatterLibrary>
	{
		private static readonly List<Entity> matters = new();
		private static readonly Dictionary<string, int> stringIds = new();

		public static IEnumerable<Entity> Matters => matters.AsEnumerable();

		private static int StringIdToId(string stringId)
		{
			if (stringIds.TryGetValue(stringId, out int id))
				return id;
			Debug.LogWarning($"Matter {stringId} not found");
			return 0;
		}

		public static bool Add(string stringId, Entity matter, out int id)
		{
			if (stringIds.ContainsKey(stringId))
			{
				Debug.LogWarning($"Tried to add existing matter: {stringId}");
				id = 0;

				return false;
			}

			id = matters.Count + 1;
			stringIds.Add(stringId, id);

			matters.Add(matter);

			return true;
		}

		public static Entity Get(string stringId) => Get(StringIdToId(stringId));
		public static Entity Get(int id)
		{
			if (id <= 0 || id >= matters.Count())
			{
				Debug.LogWarning($"Tried to access non-existing matter id {id}");
				return Entity.Null;
			}

			return matters[id];
		}

		public class Baker : Baker<MatterLibrary>
		{
			public override void Bake(MatterLibrary authoring)
			{
				// This simply converts all the prefabs into entities for MatterLibrarySystem
				foreach (Object matterObject in Resources.LoadAll("Matter"))
					GetEntity(matterObject as GameObject);
			}
		}
	}
}
