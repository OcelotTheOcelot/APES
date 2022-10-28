using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using Verse;
using Unity.Entities;

namespace Apes.UI
{
	public class MatterButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		[SerializeField]
		private TMPro.TextMeshProUGUI text;

		[SerializeField]
		private Color32 unhoveredColor = Color.black;

		[SerializeField]
		private Color32 hoveredColor = new(30, 144, 255, 255);

		[SerializeField]
		private Image image;

		private Entity matter;

		private SandboxPaintingSystem paintingSystem;

		protected void Awake()
		{
			paintingSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<SandboxPaintingSystem>();
		}

		public void AssignMatter(Entity matter, string id)
		{
			this.matter = matter;

			text.text = id;
			image.color = paintingSystem.EntityManager.GetBuffer<Matter.ColorBufferElement>(matter)[0];
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			text.outlineColor = hoveredColor;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			text.outlineColor = unhoveredColor;
		}

		public void PickMatter()
		{
			paintingSystem.SetSingleton(new Sandbox.Painting.Matter { matter = matter });
		}
	}
}