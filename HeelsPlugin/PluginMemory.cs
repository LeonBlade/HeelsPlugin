using Dalamud.Hooking;
using Dalamud.Plugin;
using System;
using System.Runtime.InteropServices;

namespace HeelsPlugin
{
	public class PluginMemory
	{
		private readonly DalamudPluginInterface pi;

		public float offset = 0;

		public IntPtr playerMovementFunc;
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void PlayerMovementDelegate(IntPtr player);
		public readonly Hook<PlayerMovementDelegate> playerMovementHook;

		public PluginMemory(DalamudPluginInterface pluginInterface)
		{
			this.pi = pluginInterface;

			this.playerMovementFunc = this.pi.TargetModuleScanner.ScanText("48 89 5C 24 08 55 48 8B EC 48 83 EC 70 83 79 7C 00");
			this.playerMovementHook = new Hook<PlayerMovementDelegate>(
				playerMovementFunc,
				new PlayerMovementDelegate(PlayerMovementHook)
			);

			this.playerMovementHook.Enable();
		}

		/// <summary>
		/// Dispose for the memory functions.
		/// </summary>
		public void Dispose()
		{
			try
			{
				// Kill the hook assuming it's not already dead.
				if (this.playerMovementHook != null)
				{
					this.playerMovementHook?.Disable();
					this.playerMovementHook?.Dispose();
				}
			}
			catch (Exception ex)
			{
				PluginLog.LogError(ex, "Error while calling PluginMemory.Dispose()");
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		struct Vector3
		{
			public float X;
			public float Y;
			public float Z;
		}

		private void PlayerMovementHook(IntPtr player)
		{
			// Call the original function.
			this.playerMovementHook.Original(player);

			try
			{
				if (this.pi.ClientState.Actors.Length > 0 && this.pi.ClientState.Actors[0].Address == player)
					this.SetPosition(offset, player.ToInt64());
			}
			catch { }
		}

		/// <summary>
		/// Sets the position of the Y for given actor.
		/// </summary>
		/// <param name="p_actor">Actor address</param>
		/// <param name="offset">Offset in the Y direction</param>
		public void SetPosition(float offset, long p_actor = 0)
		{
			try
			{
				var actor = IntPtr.Zero;
				if (p_actor == 0)
					actor = this.pi.ClientState.Actors[0].Address;
				else
					actor = new IntPtr(p_actor);

				if (actor != IntPtr.Zero)
				{
					var modelPtr = Marshal.ReadInt64(actor, 0xF0);
					if (modelPtr == 0)
						return;
					var positionPtr = new IntPtr(modelPtr + 0x50);
					var position = Marshal.PtrToStructure<Vector3>(positionPtr);

					// Offset the Y coordinate.
					position.Y += offset;

					Marshal.StructureToPtr(position, positionPtr, false);
				}
			}
			catch { }
		}
	}
}