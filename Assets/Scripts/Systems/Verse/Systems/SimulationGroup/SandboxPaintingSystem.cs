using UnityEngine;
using Unity.Entities;
using Apes.Input;
using Apes.UI;

namespace Verse
{
	public partial class SandboxPaintingSystem : SystemBase
	{
		public InputActions Actions => PlayerInput.Actions;

		private Entity space;

		private BufferLookup<Chunk.AtomBufferElement> atomBuffers;

		protected override void OnCreate()
		{
			base.OnCreate();

			Actions.Sandbox.BrushSize.performed += (ctx) => InputBrushSize(ctx.ReadValue<float>());

			atomBuffers = GetBufferLookup<Chunk.AtomBufferElement>();
		}

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			space = GetSingletonEntity<Space.Tag>();

			SetSingleton(new Sandbox.Painting.Matter { matter = MatterLibrary.Get("water") });
		}

		protected override void OnUpdate()
		{
			if (Actions.Sandbox.Paint.ReadValue<float>() > 0f)
			{
				Paint(SpaceCursorSystem.Coord, GetSingleton<Sandbox.Painting.Brush>().size, GetSingleton<Sandbox.Painting.Matter>().matter);
			}
			else if (Actions.Sandbox.Clear.ReadValue<float>() > 0f)
			{
				Erase(SpaceCursorSystem.Coord, GetSingleton<Sandbox.Painting.Brush>().size);
			}
		}

		public void Paint(Coord center, int brushSize, Entity matter)
		{
			if (matter == Entity.Null)
				return;

			foreach (Coord spaceCoord in Enumerators.GetCircle(center: center, radius: brushSize))
				Space.CreateAtom(EntityManager, space, matter, spaceCoord);

			UpdateBrushSquare(center, brushSize);
		}

		public void Erase(Coord center, int brushSize)
		{
			foreach (Coord spaceCoord in Enumerators.GetCircle(center: center, radius: brushSize))
				Space.RemoveAtom(EntityManager, space, spaceCoord);

			UpdateBrushSquare(center, brushSize);
		}

		private void UpdateBrushSquare(Coord center, int brushSize)
		{
			int inflatedSize = brushSize + 1;

			CoordRect rect = new(
				center.x - inflatedSize,
				center.y - inflatedSize,
				center.x + inflatedSize,
				center.y + inflatedSize
			);

			Space.MarkDirty(EntityManager, space, rect, safe: true);
		}

		private void InputBrushSize(float inputValue)
		{
			int size = GetSingleton<Sandbox.Painting.Brush>().size;
			size = Mathf.Clamp(size + (int)Mathf.Sign(inputValue), 0, Space.chunkSize);
			SetSingleton(new Sandbox.Painting.Brush { size = size });
		}

		public partial struct PaintJob : IJobEntity
		{
		}

		public partial struct EraseJob : IJobEntity
		{

		}
	}
}
