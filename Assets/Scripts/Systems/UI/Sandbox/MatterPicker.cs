using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Verse;

namespace Apes.UI
{
	public class MatterPicker : MonoEcs
	{
		[SerializeField]
		private MatterButton buttonPrefab;

		[SerializeField]
		private MatterGroup groupPrefab;

		private Dictionary<string, MatterGroup> groups = new();

		private void Start()
		{
			EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

			foreach (KeyValuePair<string, Entity> pair in MatterLibrary.Pairs)
				AddMatter(entityManager, pair.Key, pair.Value);
		}

		private void AddMatter(EntityManager entityManager, string id, Entity matter)
		{
			string groupId = entityManager.GetComponentData<MatterData.Group>(matter).group.ToString();

			if (!groups.TryGetValue(groupId, out MatterGroup group))
			{
				group = Instantiate(groupPrefab, transform);
				groups.Add(groupId, group);
			}

			MatterButton button = Instantiate(buttonPrefab, group.transform);
			button.AssignMatter(matter, id);
		}
	}
}
