using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Verse;

namespace Apes.UI
{
	public class MatterPicker : MonoBehaviour
	{
		[SerializeField]
		private MatterButton buttonPrefab;

		[SerializeField]
		private MatterGroup groupPrefab;

		private Dictionary<string, MatterGroup> groups = new();

		private void Start()
		{
			EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

			foreach (Entity matter in MatterLibrary.Matters)
				AddMatter(entityManager, matter);
		}

		private void AddMatter(EntityManager entityManager, Entity matter)
		{
			string groupId = entityManager.GetComponentData<Matter.Group>(matter).groupName.ToString();

			if (!groups.TryGetValue(groupId, out MatterGroup group))
			{
				group = Instantiate(groupPrefab, transform);
				groups.Add(groupId, group);
			}

			MatterButton button = Instantiate(buttonPrefab, group.transform);
			button.AssignMatter(matter, entityManager.GetComponentData<Matter.StringId>(matter).value.ToString());
		}
	}
}
