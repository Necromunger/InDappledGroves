using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace InDappledGroves.BlockEntities
{
    class IDGBESawBuck : BlockEntityDisplay
	{
		public override InventoryBase Inventory { get; }
		public override string InventoryClassName => "sawbuck";

		//public override string AttributeTransformCode => "onDisplayTransform";
		public override string AttributeTransformCode => "idgSawBuckTransform";
		public IDGBESawBuck()
		{
			Inventory = new InventoryGeneric(1, "sawbuck-slot", null, null);
			meshes = new MeshData[0];
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

		internal bool OnInteract(IPlayer byPlayer)
		{
			ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

			//If player hand empty
			if (activeHotbarSlot.Empty)
			{
				return this.TryTake(byPlayer);
			}

			if (!activeHotbarSlot.Empty && !Inventory.Empty) return true;

			//Get Collectible Object and Attributes from the Collectible Object
			CollectibleObject collectible = activeHotbarSlot.Itemstack.Collectible;
			JsonObject attributes = collectible.Attributes;
			if (attributes == null || !collectible.Attributes["idgSawBuckProps"]["sawable"].AsBool(false))
			{
				return false;
			}

			if (this.TryPut(activeHotbarSlot))
			{
				this.Api.World.PlaySoundAt(GetSound(activeHotbarSlot) ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16f, 1f);
				updateMeshes();
				return true;
			}
			return false;
		}

		private AssetLocation GetSound(ItemSlot slot)
		{
			if (slot.Itemstack == null)
			{
				return null;
			}
			else
			{
				Block block = slot.Itemstack.Block;
				if (block == null)
				{
					return null;
				}
				else
				{
					BlockSounds sounds = block.Sounds;
					return (sounds?.Place);
				}
			}
		}

		private bool TryPut(ItemSlot slot)
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

		private bool TryTake(IPlayer byPlayer)
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
							assetLocation = (sounds?.Place);
						}
						AssetLocation assetLocation2 = assetLocation;
						this.Api.World.PlaySoundAt(assetLocation2 ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16f, 1f);
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

		public override void updateMeshes()
		{
			base.updateMeshes();
		}
	}
}
