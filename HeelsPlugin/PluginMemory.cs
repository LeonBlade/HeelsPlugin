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
		private IntPtr actor = IntPtr.Zero;

		public IntPtr playerMovementFunc;
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void PlayerMovementDelegate(IntPtr player);
		public readonly Hook<PlayerMovementDelegate> playerMovementHook;

		public IntPtr gposeActorFunc;
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate IntPtr GposeActorDelegate(IntPtr rcx, int rdx, int r8, IntPtr r9, long unknown);
		public readonly Hook<GposeActorDelegate> gposeActorHook;

		public PluginMemory(DalamudPluginInterface pluginInterface)
		{
			this.pi = pluginInterface;

			this.playerMovementFunc = this.pi.TargetModuleScanner.ScanText("48 89 5C 24 08 55 48 8B EC 48 83 EC 70 83 79 7C 00");
			this.playerMovementHook = new Hook<PlayerMovementDelegate>(
				this.playerMovementFunc,
				new PlayerMovementDelegate(this.PlayerMovementHook)
			);

			this.gposeActorFunc = this.pi.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 48 8B D8 48 85 DB 74 33");
			this.gposeActorHook = new Hook<GposeActorDelegate>(
				this.gposeActorFunc,
				new GposeActorDelegate(this.GposeActorHook)
			);

			this.playerMovementHook.Enable();
			this.gposeActorHook.Enable();
		}

		/// <summary>
		/// Dispose for the memory functions.
		/// </summary>
		public void Dispose()
		{
			try
			{
				if (this.playerMovementHook != null)
				{
					this.playerMovementHook?.Disable();
					this.playerMovementHook?.Dispose();
				}

				if (this.gposeActorHook != null)
				{
					this.gposeActorHook?.Disable();
					this.gposeActorHook?.Dispose();
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
				if (this.actor == player)
					this.SetPosition(offset, player.ToInt64());
			}
			catch { }
		}

		private IntPtr GposeActorHook(IntPtr rcx, int rdx, int r8, IntPtr r9, long unknown)
		{
			try
			{
				this.actor = new IntPtr(Marshal.ReadInt64(r9 + 8));
			}
			catch (Exception ex)
			{
				PluginLog.LogError(ex.Message);
			}

			return this.gposeActorHook.Original(rcx, rdx, r8, r9, unknown);
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