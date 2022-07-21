using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace InDappledGroves.Items
{
    class IDGPlank : Item
    {
		public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
		{

			if (blockSel == null) return;
			BlockPos position = blockSel.Position;
			Block block = byEntity.World.BlockAccessor.GetBlock(position);

			if (!byEntity.Controls.Sneak && !byEntity.Controls.Sprint && block is not BlockPlankPile)
			{

				string failurecode = "";

				Block targetBlock = api.World.BlockAccessor.GetBlock(blockSel.Position.AddCopy(blockSel.Face));
				ItemStack stackWood = new ItemStack(api.World.BlockAccessor.GetBlock(new AssetLocation("indappledgroves:idgboard-" + slot.Itemstack.Collectible.Variant["wood"] + "-" + SuggestedHVOrientation(((EntityPlayer)byEntity).Player, blockSel).GetValue(0).ToString())));
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
				handling = EnumHandHandling.PreventDefault;
			}
			
		}

		public static BlockFacing[] SuggestedHVOrientation(IPlayer byPlayer, BlockSelection blockSel)
		{
			BlockPos blockPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
			double num = byPlayer.Entity.Pos.X + byPlayer.Entity.LocalEyePos.X - ((double)blockPos.X + blockSel.HitPosition.X);
			double num2 = byPlayer.Entity.Pos.Y + byPlayer.Entity.LocalEyePos.Y - ((double)blockPos.Y + blockSel.HitPosition.Y);
			double num3 = byPlayer.Entity.Pos.Z + byPlayer.Entity.LocalEyePos.Z - ((double)blockPos.Z + blockSel.HitPosition.Z);
			float radiant = (float)Math.Atan2(num, num3) + 1.5707964f;
			double y = num2;
			float num4 = (float)Math.Sqrt(num * num + num3 * num3);
			float num5 = (float)Math.Atan2(y, (double)num4);
			BlockFacing blockFacing = ((double)num5 < -0.7853981633974483) ? BlockFacing.DOWN : (((double)num5 > 0.7853981633974483) ? BlockFacing.UP : null);
			BlockFacing blockFacing2 = BlockFacing.HorizontalFromAngle(radiant);
			return new BlockFacing[]
			{
				blockFacing2,
				blockFacing
			};
		}
	}
}
