using Apes;
using Input;
using UnityEngine;

namespace Apes.Input
{
	public static class PlayerInput
	{
		public static InputActions Actions { get; private set; }

		public static bool WorldInputEnabled
		{
			get => Actions.World.enabled;
			set
			{
				if (value)
					Actions.World.Enable();
				else
					Actions.World.Disable();
			}
		}

		public static bool UiInputEnabled
		{
			get => Actions.UI.enabled;
			set
			{
				if (value)
					Actions.UI.Enable();
				else
					Actions.UI.Disable();
			}
		}

		static PlayerInput()
		{
			Actions = new InputActions();

			Actions.Global.Enable();

			Actions.Sandbox.Enable();
			Actions.UI.Disable();
			Actions.World.Disable();
		}

		public static void UpdateInput()
		{
			WorldInputEnabled = !Game.Paused;
			UiInputEnabled = Game.Paused;
		}
	}
}
