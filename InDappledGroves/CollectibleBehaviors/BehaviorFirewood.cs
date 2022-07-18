using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace InDappledGroves.CollectibleBehaviors
{
    class BehaviorFirewood : CollectibleBehavior
	{
		ICoreAPI api;
		ICoreClientAPI capi;

		public BehaviorFirewood(CollectibleObject collObj) : base(collObj)
		{
			this.collObj = collObj;
		}

		public override void Initialize(JsonObject properties)
		{
			base.Initialize(properties);
		}

		public override void OnLoaded(ICoreAPI api)
		{
			this.api = api;
			this.capi = (api as ICoreClientAPI);
		}

		public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling) { 
			if (blockSel == null) return;
			BlockPos position = blockSel.Position;
			Block block = byEntity.World.BlockAccessor.GetBlock(position);

			if (block is BlockFirepit || block is BlockPitkiln || block is BlockClayOven)
			{
				return;
			}

			if (!byEntity.Controls.Sneak && !byEntity.Controls.Sprint && block is not BlockFirewoodPile)
			{

				string failurecode = "";

				Block targetBlock = api.World.BlockAccessor.GetBlock(blockSel.Position.AddCopy(blockSel.Face));
				ItemStack stackWood = new ItemStack(api.World.BlockAccessor.GetBlock(new AssetLocation("indappledgroves:idgfirewoodblock")));
				bool flag = false;
				if (targetBlock.Replaceable > 5000 && stackWood.Block.TryPlaceBlock(byEntity.World, ((EntityPlayer)byEntity).Player, stackWood, blockSel, ref failurecode))
				{
					flag = true;
					slot.TakeOut(1);
				}
				else
				{
					BlockSelection bs2 = blockSel;
					bs2.Position = bs2.Position.AddCopy(blockSel.Face);
					if (stackWood.Block.TryPlaceBlock(byEntity.World, ((EntityPlayer)byEntity).Player, stackWood, bs2, ref failurecode))
					{
						flag = true;
						slot.TakeOut(1);
					}
				}
				if (flag)
				{
					this.api.World.PlaySoundAt(new AssetLocation("sounds/player/build"), byEntity, ((EntityPlayer)byEntity).Player, true, 16f, 1f);
				}
			}
			handHandling = EnumHandHandling.PreventDefault;
			handling = EnumHandling.PreventDefault;

			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
		}
	}
}
