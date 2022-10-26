//using Unity.Entities;

//using Unity.Jobs;
//using Unity.Collections;
//using Unity.Burst;

//using Unity.Mathematics;
//using System.Runtime.CompilerServices;
//using UnityEngine;
//using static Verse.Chunk;
//using static Verse.Particle;
//using static Verse.AtomPhysics;

//namespace Verse
//{
//	[UpdateInGroup(typeof(VerseTickSystemGroup))]
//	public partial class ParticlePhysicsSystem : SystemBase
//	{

//		public partial struct BresenhamParticleJob : IJobEntity
//		{
//			[ReadOnly]
//			public int tick;
//			[ReadOnly]
//			public Entity space;
//			[ReadOnly]
//			public Space.Bounds bounds;

//			[ReadOnly]
//			public DynamicBuffer<Space.RegionBufferElement> regions;
//			[ReadOnly]
//			public ComponentLookup<Region.SpatialIndex> regionIndexes;
//			[ReadOnly]
//			public BufferLookup<Region.ChunkBufferElement> chunkBuffers;
			
//			[ReadOnly]
//			public ComponentLookup<Atom.Matter> matters;
//			[ReadOnly]
//			public ComponentLookup<Matter.AtomState> states;
//			[ReadOnly]
//			public ComponentLookup<Matter.PhysicProperties> physicProperties;

//			[NativeDisableParallelForRestriction]
//			public BufferLookup<AtomBufferElement> atomBuffers;
//			[NativeDisableParallelForRestriction]
//			public ComponentLookup<DirtyArea> dirtyAreas;


//			public void Execute(ref Velocity vel, ref Position pos, in OriginalAtom original)
//			{
//				Coord spaceCoord = pos;
//				if (!Space.GetRegionAtSpaceCoord(space, spaceCoord, bounds, regions, regionIndexes, out Entity region, out int2 regionIndex))
//				{
//					Debug.LogWarning($"Somehow, there was a particle at {pos} with no region instantiated.");
//					return;
//				}

//				Entity chunk = chunkBuffers[region].GetChunkAtSpaceCoord(spaceCoord, regionIndex);
//				var atoms = atomBuffers[chunk];

//				bool moved = false;
//				Coord lastLineCoord = atomCoord;

//				int yDir = vel.y > 0 ? 1 : -1;
//				int xDir = vel.x > 0 ? 1 : -1;

//				Coord lastLineCoordSwappable = atomCoord;
//				DynamicBuffer<AtomBufferElement> lastLineBuffer = atoms;

//				float2 absVel = math.abs(vel);
//				if (absVel.x > absVel.y)
//				{
//					float yShift = vel.y / absVel.x;
//					int toX = Mathf.CeilToInt(absVel.x);
//				}
//				else
//				{
//					float xShift = vel.x / absVel.y;
//					int toY = Mathf.CeilToInt(absVel.y);

//					for (int deltaY = 1; deltaY <= toY; deltaY++)
//					{
//						Coord nextCoord = atomCoord + new int2(Mathf.RoundToInt(xShift * deltaY), deltaY * yDir);

//						if (!atoms.GetAtomNeighbourFallback(
//				   atomBuffers, neighbours, nextCoord,
//				   out Entity otherAtom,
//				   out DynamicBuffer<AtomBufferElement> otherAtoms,
//				   out Coord otherCoord
//				   ))
//							break;

//						if (!IsPassable(otherAtom, matter, atomProps))
//						{
//							if (atoms.GetAtomNeighbourFallback(
//					   atomBuffers, neighbours, new Coord(nextCoord.x - xDir, nextCoord.y),
//					   out Entity slopeAtom, out _, out _
//							) && IsPassable(slopeAtom, matter, atomProps))
//							{
//								vel = ReflectAgainst45(vel, -xDir, -yDir);
//								moved = true;
//							}
//							else
//							{
//								vel.y = 0;
//							}

//							break;
//						}

//						moved = true;
//						lastLineCoord = nextCoord;
//						lastLineCoordSwappable = otherCoord;
//						lastLineBuffer = otherAtoms;
//					}
//				}

//				if (moved)
//				{
//					AtomBufferExtention.SetAtom(atoms, atomCoord, lastLineBuffer, lastLineCoordSwappable);

//					CoordRect dirtyRect = CoordRect.CreateRectBetween(atomCoord, lastLineCoord, margin: 1);

//					dirtyArea.MarkDirty(dirtyRect, safe: true);
//					neighbours.MarkDirty(dirtyAreas, dirtyRect, safe: true);
//				}
//			}

//			private bool IsPassable(Entity otherAtom, Entity thisMatter, Matter.PhysicProperties physProps)
//			{
//				if (otherAtom == Entity.Null)
//					return true;

//				Entity otherMatter = matters[otherAtom].value;
//				if (thisMatter == otherMatter)
//					return false;
//				if (states[otherMatter].value == Matter.State.Solid)
//					return false;

//				return physProps.density <= physicProperties[otherMatter].density;
//			}
//		}
//	}
//}