using UnityEngine;


namespace Apes
{
	public abstract class Interaction : MonoBehaviour
	{
		public bool Initialized { get; private set; } = false;

		public DynamicObject Entity { get; private set; }

		public bool Selectable { get; set; } = true;

		protected virtual void Awake()
		{
			if (!Entity)
				Entity = GetComponent<DynamicObject>();

			Initialized = true;
		}

		/// <summary>
		/// Called when the mob uses the object.
		/// </summary>
		/// <param name="mob">The user of the object.</param>
		/// <returns>true if the object was used successfully, false otherwise.</returns>
		public abstract bool OnUse(Mob mob);

		/// <summary>
		/// Checks if the mob can use this object.
		/// </summary>
		/// <param name="mob">The mob to check the usage for.</param>
		/// <returns>true if the object can be used by the mob, false otherwise.</returns>
		public virtual bool CanBeUsedBy(Mob mob) => true;

		private void Update()
		{
			Tick(Time.deltaTime);
		}

		protected virtual void Tick(float delta)
		{
		}
	}

}
