using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;

namespace Verse
{
	public class MatterLibrary : MonoSingleton<MatterLibrary>
	{
		private static Dictionary<string, Entity> dictionary = new();

		[SerializeField]
		private List<GameObject> matters;

		public static IEnumerable<KeyValuePair<string, Entity>> Pairs => dictionary;

		public static void Add(string id, Entity entity) => dictionary.Add(id, entity);
		public static Entity Get(string id)
		{
			if (dictionary.TryGetValue(id, out Entity matter))
				return matter;

			Debug.LogWarning($"Tried to access non-existing matter id {id}");
			return Entity.Null;
		}
	}
}
