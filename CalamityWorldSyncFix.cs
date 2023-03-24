using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader;

namespace CalamityWorldSyncFix
{
	public class CalamityWorldSyncFix : Mod
	{
		private Mod Calamity => ModLoader.GetMod("CalamityMod")!;

		private MethodInfo WorldSyncingSystem_NetSend = null!;
		private FieldInfo CalamityWorld_DraedonSummonCountdown = null!;
		private FieldInfo CalamityWorld_DraedonMechdusa = null!;

		private event ILContext.Manipulator ModifyNetSend
		{
			add
			{
				HookEndpointManager.Modify(WorldSyncingSystem_NetSend, value);
			}
			remove
            {
                HookEndpointManager.Unmodify(WorldSyncingSystem_NetSend, value);
            }
		}

		public override void Load()
		{
			if(Calamity.Version != new System.Version(2, 0, 2, 2))
			{
				throw new NotSupportedException("Only Calamity v2.0.2.2 is supported. If you have a newer version, it's probably already fixed and you don't need this mod. Your version: " + Calamity.Version);
			}

			var types = Calamity.GetType().Assembly
				.GetTypes();

            WorldSyncingSystem_NetSend = types.First(t => t.Name == "WorldSyncingSystem")!
				.GetMethods()
				.First(m => m.Name == "NetSend")!;

			var calamityWorldFields = types.First(t => t.Name == "CalamityWorld")!.GetFields();
			CalamityWorld_DraedonSummonCountdown = calamityWorldFields.First(f => f.Name == "DraedonSummonCountdown")!;
            CalamityWorld_DraedonMechdusa = calamityWorldFields.First(f => f.Name == "DraedonMechdusa")!;

            ModifyNetSend += NetSendPatcher;
		}

		public override void Unload()
		{
			ModifyNetSend -= NetSendPatcher;
		}

		private void NetSendPatcher(ILContext context)
		{
			var cursor = new ILCursor(context);

			cursor.GotoNext(i => i.MatchLdsfld(CalamityWorld_DraedonSummonCountdown));
			cursor.Index += 2;

			cursor.Emit(OpCodes.Ldarg_1);
			cursor.Emit(OpCodes.Ldsfld, CalamityWorld_DraedonMechdusa);
			cursor.Emit(OpCodes.Callvirt, typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(bool) })!);
		}
	}
}