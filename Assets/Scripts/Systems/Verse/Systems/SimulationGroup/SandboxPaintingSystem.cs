using UnityEngine;
using Unity.Entities;
using Apes.Input;
using Apes.UI;
using Unity.Collections;
using Unity.Jobs;
using static Verse.Chunk;
using Unity.Burst;
using Unity.Mathematics;

namespace Verse
{
	public partial class SandboxPaintingSystem : SystemBase
	{
		public InputActions Actions => PlayerInput.Actions;

		private EntityQuery chunkQueery;
		
		private EndSimulationEntityCommandBufferSystem commandBufferSystem;

		protected override void OnCreate()
		{
			base.OnCreate();

			RequireForUpdate<Space.Tag>();
			RequireForUpdate<Sandbox.Painting.Brush>();
			RequireForUpdate<Sandbox.Painting.Matter>();

			Actions.Sandbox.BrushSize.performed += (ctx) => InputBrushSize(ctx.ReadValue<float>());

			chunkQueery = GetEntityQuery(ComponentType.ReadWrite<Chunk.DirtyArea>());

			commandBufferSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
		}

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			SetSingleton(new Sandbox.Painting.Matter { matter = MatterLibrary.Get("water") });
		}

		protected override void OnUpdate()
		{
			Entity matter = Entity.Null;

			bool painting = Actions.Sandbox.Paint.ReadValue<float>() > 0f && (matter = GetSingleton<Sandbox.Painting.Matter>().matter) != Entity.Null;
			bool erasing = Actions.Sandbox.Clear.ReadValue<float>() > 0f;

			if (!painting && !erasing)
				return;

			EntityCommandBuffer.ParallelWriter commandBuffer =
				GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged).AsParallelWriter();

			Coord spaceCoord = SpaceCursorSystem.Coord;

			Sandbox.Painting.Brush brush = GetSingleton<Sandbox.Painting.Brush>();
			int inflatedSize = brush.size + 1;
			CoordRect brushRect = new(
				spaceCoord.x - inflatedSize,
				spaceCoord.y - inflatedSize,
				spaceCoord.x + inflatedSize,
				spaceCoord.y + inflatedSize
			);

			JobHandle handle;
			if (erasing)
			{
				handle = new EraseJob
				{
					brushSize = brush.size,
					spaceBrushRect = brushRect,
					spaceCoord = spaceCoord,
					ecb = commandBuffer
				}.ScheduleParallel(chunkQueery, Dependency);
			}
			else if (painting)
			{
				handle = new PaintJob
				{
					brush = brush,
					spaceBrushRect = brushRect,
					spaceCoord = spaceCoord,
					atomArchetype = Archetypes.Atom,

					tick = TickerSystem.CurrentTick,

					matter = matter,
					creationDatas = GetComponentLookup<Matter.Creation>(),
					matterColors = GetBufferLookup<Matter.ColorBufferElement>(),
					atomMatters = GetComponentLookup<Atom.Matter>(),

					ecb = commandBuffer
				}.Schedule(chunkQueery, Dependency);
			}
			else
			{
				return;
			}

			handle.Complete();

			commandBufferSystem.AddJobHandleForProducer(handle);
		}

		private void InputBrushSize(float inputValue)
		{
			int size = GetSingleton<Sandbox.Painting.Brush>().size;
			size = Mathf.Clamp(size + (int)Mathf.Sign(inputValue), 0, Space.chunkSize);
			SetSingleton(new Sandbox.Painting.Brush { size = size });
		}

		[BurstCompile]
		public partial struct EraseJob : IJobEntity
		{
			[ReadOnly]
			public CoordRect spaceBrushRect;
			[ReadOnly]
			public Coord spaceCoord;
			[ReadOnly]
			public int brushSize;

			public EntityCommandBuffer.ParallelWriter ecb;

			public void Execute(
				in SpatialIndex spatialIndex, ref DirtyArea dirtyArea,
				DynamicBuffer<AtomBufferElement> atoms, [EntityInQueryIndex] int entityInQueryIndex)
			{
				CoordRect brushRect = spaceBrushRect - spatialIndex.origin;
				if (!brushRect.IntersectWith(Space.chunkBounds))
					return;

				Coord chunkCoord = spaceCoord - spatialIndex.origin;

				if (brushSize == 0)
				{
					DestroyAtom(chunkCoord, atoms, sortKey: entityInQueryIndex);
				}
				else
				{
					for (int x = -brushSize; x <= brushSize; x++)
					{
						int height = Mathf.FloorToInt(Mathf.Sqrt(brushSize * brushSize - x * x));
						for (int y = -height; y <= height; y++)
							DestroyAtom(chunkCoord + new Coord(x, y), atoms, sortKey: entityInQueryIndex);
					}
				}

				dirtyArea.MarkDirty(brushRect, safe: false);
			}

			public void DestroyAtom(Coord coord, DynamicBuffer<AtomBufferElement> atoms, int sortKey)
			{
				if (!Space.chunkBounds.Contains(coord))
					return;

				Entity oldAtom = atoms.GetAtom(coord);

				if (oldAtom == Entity.Null)
					return;

				ecb.DestroyEntity(sortKey, oldAtom);
				atoms.SetAtom(coord, Entity.Null);
			}
		}

		[BurstCompile]
		public partial struct PaintJob : IJobEntity
		{
			[ReadOnly]
			public CoordRect spaceBrushRect;
			[ReadOnly]
			public Coord spaceCoord;
			[ReadOnly]
			public Sandbox.Painting.Brush brush;
			[ReadOnly]
			public Entity matter;
			[ReadOnly]
			public EntityArchetype atomArchetype;

			[ReadOnly]
			public int tick;

			[ReadOnly]
			public ComponentLookup<Atom.Matter> atomMatters;
			[ReadOnly]
			public ComponentLookup<Matter.Creation> creationDatas;
			[ReadOnly]
			public BufferLookup<Matter.ColorBufferElement> matterColors;

			public EntityCommandBuffer.ParallelWriter ecb;

			public void Execute(
				in Entity chunk, in SpatialIndex spatialIndex, ref DirtyArea dirtyArea,
				DynamicBuffer<AtomBufferElement> atoms, [EntityInQueryIndex] int entityInQueryIndex
			)
			{
				CoordRect brushRect = spaceBrushRect - spatialIndex.origin;
				if (!brushRect.IntersectWith(Space.chunkBounds))
					return;

				Coord chunkCoord = spaceCoord - spatialIndex.origin;
				DynamicBuffer<AtomBufferElement> newBuffer = ecb.CloneBuffer(entityInQueryIndex, chunk, atoms);

				int brushSize = brush.size;
				if (brushSize == 0)
				{
					CreateAtom(chunkCoord, atoms, newBuffer, sortKey: entityInQueryIndex);
				}
				else
				{
					for (int x = -brushSize; x <= brushSize; x++)
					{
						int height = Mathf.FloorToInt(Mathf.Sqrt(brushSize * brushSize - x * x));
						for (int y = -height; y <= height; y++)
							CreateAtom(chunkCoord + new Coord(x, y), atoms, newBuffer, sortKey: entityInQueryIndex);
					}
				}

				dirtyArea.MarkDirty(brushRect, safe: false);
			}

			public void CreateAtom(Coord coord, DynamicBuffer<AtomBufferElement> atoms, DynamicBuffer<AtomBufferElement> newAtoms, int sortKey)
			{
				if (!Space.chunkBounds.Contains(coord))
					return;

				Entity oldAtom = atoms.GetAtom(coord);
				if (oldAtom != Entity.Null)
				{
					Entity oldMatter = atomMatters[oldAtom].value;
					if (oldMatter == matter)
						return;

					ecb.DestroyEntity(sortKey, oldAtom);
				}

				Entity newAtom = ecb.CreateEntity(sortKey, atomArchetype);

				ecb.SetComponent(sortKey, newAtom, new Atom.Matter { value = matter });

				Matter.Creation creationData = creationDatas[matter];
				ecb.SetComponent(sortKey, newAtom, new Atom.Temperature { value = creationData.temperature });
				ecb.SetComponent<Atom.Color>(sortKey, newAtom, Utils.Pick(matterColors[matter], tick + coord.y*Space.chunkSize + coord.x));

				if (brush.spinkle > 0f)
				{
					float xVel = math.sin(tick / math.PI) * brush.spinkle;
					ecb.SetComponent(sortKey, newAtom, new Atom.Dynamics(new float2(xVel, 0f)));
				}

				newAtoms.SetAtom(coord, newAtom);
			}
		}
	}
}
