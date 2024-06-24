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
			BlockPos pos = blockSel.Position;
			Block block = byEntity.World.BlockAccessor.GetBlock(pos, 0);
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		}

		public string GetType(CollectibleObject colObj)
		{
			if (colObj.Variant["type"] != null)
			{
				return colObj.Variant["type"];
			}
			return "smooth";
		}

		public string GetState(CollectibleObject colObj)
        {
			if(colObj.Variant["state"] != null)
            {
				return colObj.Variant["state"];
			}
			return "cured";
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
