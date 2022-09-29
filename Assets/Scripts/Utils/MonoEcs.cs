using UnityEngine;
using Unity.Entities;

public class MonoEcs : MonoBehaviour
{
	protected EntityManager EntityManager { get; private set; }
	
	protected virtual void Awake()
	{
		EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
	}
}
