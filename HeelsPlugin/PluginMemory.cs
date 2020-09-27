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
		private delegate void PlayerMovementDelegate(IntPtr player, float x, float y, float z);
		private readonly Hook<PlayerMovementDelegate> playerMovementHook;

		public PluginMemory(DalamudPluginInterface pluginInterface)
		{
			this.pi = pluginInterface;

			this.playerMovementFunc = this.pi.TargetModuleScanner.ScanText("40 53 48 83 EC 20 F3 0F 11 89 A0");
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

		private void PlayerMovementHook(IntPtr player, float x, float y, float z)
		{
			try
			{
				if (this.pi.ClientState.Actors.Length > 0 && this.pi.ClientState.Actors[0].Address == player)
					y += offset;
			}
			catch { }

			// Call the original function.
			this.playerMovementHook.Original(player, x, y, z);
		}
	}
}
