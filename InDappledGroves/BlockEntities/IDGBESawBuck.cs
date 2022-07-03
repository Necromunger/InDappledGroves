using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using static InDappledGroves.Util.IDGRecipeNames;

namespace InDappledGroves.BlockEntities
{
	class IDGBESawBuck : BlockEntityDisplay
	{
		public override InventoryBase Inventory { get; }
		public override string InventoryClassName => "sawbuck";
		public override string AttributeTransformCode => "idgSawBuckTransform";

		//static List<SawingRecipe> sawingRecipes = IDGRecipeRegistry.Loaded.SawingRecipes;

		//public SawingRecipe recipe;

		public IDGBESawBuck()
		{
			Inventory = new InventoryGeneric(1, "sawbuck-slot", null, null);
			meshes = new MeshData[0];
		}

		public ItemSlot InputSlot
		{
			get { return Inventory[0]; }
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

			if (activeHotbarSlot.Empty)
			{
				return this.TryTake(byPlayer);
			}

			if (!activeHotbarSlot.Empty && !Inventory.Empty) return true;

			//Get Collectible Object and Attributes from the Collectible Object
			//Then check to see if attributes is null, or if chopblock is false or absent

			CollectibleObject collectible = activeHotbarSlot.Itemstack.Collectible;
			JsonObject attributes = collectible.Attributes;
			if (attributes == null || !collectible.Attributes["woodworkingProps"]["idgSawBuckProps"]["sawable"].AsBool(false))
			{
				return false;
			}

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
					assetLocation = (sounds?.Place);
				}
			}
			AssetLocation assetLocation2 = assetLocation;
			if (this.TryPut(activeHotbarSlot))
			{
				this.Api.World.PlaySoundAt(assetLocation2 ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16f, 1f);
				updateMeshes();
				return true;
			}
			return false;
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

		protected override MeshData genMesh(ItemStack stack)
		{
			MeshData meshData;
			if (stack.Collectible is IContainedMeshSource containedMeshSource)
			{
				meshData = containedMeshSource.GenMesh(stack, this.capi.BlockTextureAtlas, this.Pos);
				meshData.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, base.Block.Shape.rotateY * 0.017453292f, 0f);
			}
			else
			{
				this.nowTesselatingObj = stack.Collectible;
				this.nowTesselatingShape = capi.TesselatorManager.GetCachedShape(stack.Collectible is Block ? (stack.Block.ShapeInventory?.Base == null ? stack.Block.Shape.Base : stack.Block.ShapeInventory.Base) : stack.Item.Shape.Base);
				if (stack.Collectible is Block)
				{
					capi.Tesselator.TesselateShape(stack.Collectible, nowTesselatingShape, out meshData, null, null, null);
				}
				else
				{
					capi.Tesselator.TesselateItem(stack.Item, out meshData, this);
				}


			}
			ModelTransform transform = stack.Collectible.Attributes[this.AttributeTransformCode].AsObject<ModelTransform>();

			transform.EnsureDefaultValues();

			transform.Rotation.Y = (Block.Shape.rotateY + 90f);

			meshData.ModelTransform(transform);

			return meshData;
		}

		public override void updateMeshes()
		{
			for (int i = 0; i < this.meshes.Length; i++)
			{
				this.updateMesh(i);
			}
			base.updateMeshes();
		}

		protected override void updateMesh(int index) { 
			if (this.Api == null || this.Api.Side == EnumAppSide.Server)
			{
				return;
			}
			if (this.Inventory[index].Empty)
			{
				this.meshes[index] = null;
				return;
			}
			MeshData meshData = this.genMesh(this.Inventory[index].Itemstack);
			this.TranslateMesh(meshData, index);
			this.meshes[index] = meshData;
		}

		private readonly Matrixf mat = new();
	}
}