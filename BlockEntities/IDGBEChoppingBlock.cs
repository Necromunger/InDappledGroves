using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace InDappledGroves.BlockEntities
{
    class IDGBEChoppingBlock : BlockEntityDisplay
    {
		public override InventoryBase Inventory { get; }
		public override string InventoryClassName => "choppingblock";
        public override string AttributeTransformCode => "idgChoppingBlockTransform";
        public IDGBEChoppingBlock()
		{
			Inventory = new InventoryGeneric(1, "choppingblock-slot", null, null);
			meshes = new MeshData[1];

		}

		public override void Initialize(ICoreAPI api)
		{
			base.Initialize(api);
			this.capi = (api as ICoreClientAPI);
			if (this.capi != null)
			{
				this.updateMeshes();
			}
		}

		internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
		{
			ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
			if (activeHotbarSlot.Empty || activeHotbarSlot.Itemstack.Collectible.HasBehavior<BehaviorWoodSplitter>())
			{
				return this.TryTake(byPlayer, blockSel);
			}

			//Get Collectible Object and Attributes from the Collectible Object
			//Then check to see if attributes is null, or if chopblock is false or absent
			
			CollectibleObject collectible = activeHotbarSlot.Itemstack.Collectible;
			JsonObject attributes = collectible.Attributes;
			if (attributes == null || !collectible.Attributes["idgchopblock"]["cuttable"].AsBool(false))
			{
				return false;
			}

            //JsonObject attributes2 = attributes["chopblock"];
            //JsonObject attributes3 = attributes2["output"];
            //ItemStack itemstack1 = new ItemStack(Api.World.GetItem(new AssetLocation(attributes3["code"].AsString())), attributes3["quantity"].AsInt());

            ItemStack itemstack = activeHotbarSlot.Itemstack;
			AssetLocation assetLocation;
			if (itemstack == null)
			{
				assetLocation = null;
			}
			else
			{
				Block block = itemstack.Block;
				if (block == null)
				{
					assetLocation = null;
				}
				else
				{
					BlockSounds sounds = block.Sounds;
					assetLocation = ((sounds != null) ? sounds.Place : null);
				}
			}
			AssetLocation assetLocation2 = assetLocation;
			if (this.TryPut(activeHotbarSlot, blockSel))
			{
				this.Api.World.PlaySoundAt((assetLocation2 != null) ? assetLocation2 : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16f, 1f);
				this.updateMeshes();
				return true;
			}
			return false;
		}

		private bool TryPut(ItemSlot slot, BlockSelection blockSel)
		{
			for (int i = 0; i < Inventory.Count; i++)
			{
                if (this.Inventory[i].Empty)
                {
                    int num3 = slot.TryPutInto(this.Api.World, this.Inventory[i], 1);
                    this.updateMeshes();
                    base.MarkDirty(true, null);
                    return num3 > 0;
                }
            }
			return false;
		}

		private bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
		{
			for (int i = 0; i < Inventory.Count; i++)
			{
				if (!this.Inventory[i].Empty)
				{
					ItemStack itemStack = this.Inventory[i].TakeOut(1);
					if (byPlayer.InventoryManager.TryGiveItemstack(itemStack, false))
					{
						Block block = itemStack.Block;
						AssetLocation assetLocation;
						if (block == null)
						{
							assetLocation = null;
						}
						else
						{
							BlockSounds sounds = block.Sounds;
							assetLocation = ((sounds != null) ? sounds.Place : null);
						}
						AssetLocation assetLocation2 = assetLocation;
						this.Api.World.PlaySoundAt((assetLocation2 != null) ? assetLocation2 : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16f, 1f);
					}
					if (itemStack.StackSize > 0)
					{
						this.Api.World.SpawnItemEntity(itemStack, this.Pos.ToVec3d().Add(0.5, 0.5, 0.5), null);
					}
					base.MarkDirty(true, null);
					this.updateMeshes();
					return true;
				}
			}
			return false;
		}

		private Matrixf mat = new Matrixf();
	}
}
