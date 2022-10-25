using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using static InDappledGroves.Util.IDGRecipeNames;

namespace InDappledGroves.BlockEntities
{
    class IDGBEChoppingBlock : BlockEntityDisplay
    {
		public override InventoryBase Inventory { get; }
		public override string InventoryClassName => "choppingblock";
        public override string AttributeTransformCode => "onDisplayTransform";

		static List<ChoppingBlockRecipe> choppingBlockrecipes = IDGRecipeRegistry.Loaded.ChoppingBlockRecipes;

		public IDGBEChoppingBlock()
		{
			Inventory = new InventoryDisplayed(this, 1, "choppingblock-slot", null, null);
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
		public ItemSlot InputSlot
		{
			get { return Inventory[0]; }
		}

		internal bool OnInteract(IPlayer byPlayer)
		{
			ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

			//If The Players Hand Is Empty
			if (activeHotbarSlot.Empty)
			{
				return this.TryTake(byPlayer);
			}

			CollectibleObject collectible = activeHotbarSlot.Itemstack.Collectible;	

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

			if (DoesSlotMatchRecipe(Api.World, activeHotbarSlot) && this.TryPut(activeHotbarSlot))
			{	 
				this.Api.World.PlaySoundAt(assetLocation2 ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16f, 1f);
                updateMeshes();
			}
			return true;
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
			String side = Block.Variant["side"];
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

			ModelTransform transform = stack.Collectible.Attributes["woodWorkingProps"]["idgChoppingBlockProps"]["idgChoppingBlockTransform"].Exists? stack.Collectible.Attributes["woodWorkingProps"]["idgChoppingBlockProps"]["idgChoppingBlockTransform"].AsObject<ModelTransform>(): null;

			if(transform == null)
            {
				transform = new ModelTransform
				{
					Translation = new Vec3f(),
					Rotation = new Vec3f(0f, -45f, 0f),
					Origin = new Vec3f(0.5f, 0f, 0.5f),
					Scale = 0.95f
				};
			}
			transform.EnsureDefaultValues();

			
			meshData.ModelTransform(ProcessTransform(transform, side));
			return meshData;
		}

		private ModelTransform ProcessTransform(ModelTransform transform, String side)
		{
            transform.Rotation.X += AddRotate(side + "x");
            transform.Rotation.Y = (Block.Shape.rotateY) + AddRotate(side + "y");
            transform.Rotation.Z += AddRotate(side + "z");
            transform.Translation.X += AddTranslate(side + "x");
            transform.Translation.Y += AddTranslate(side + "y");
            transform.Translation.Z += AddTranslate(side + "z");
            transform.Scale = AddScale(side);
			return transform;
		}

		public float AddRotate(string sideAxis)
		{
			JsonObject transforms = this.Inventory[0].Itemstack.Collectible.Attributes["woodWorkingProps"]["idgChoppingBlockProps"]["idgChoppingBlockTransform"];
			return transforms["rotation"][sideAxis].Exists ? transforms["rotation"][sideAxis].AsFloat() : 0f;
		}

		public float AddTranslate(string sideAxis)
		{
			JsonObject transforms = this.Inventory[0].Itemstack.Collectible.Attributes["woodWorkingProps"]["idgChoppingBlockProps"]["idgChoppingBlockTransform"];
			return transforms["translation"][sideAxis].Exists ? transforms["translation"][sideAxis].AsFloat() : 0f;
		}

		public float AddScale(string side)
		{
			JsonObject transforms = this.Inventory[0].Itemstack.Collectible.Attributes["woodWorkingProps"]["idgChoppingBlockProps"]["idgChoppingBlockTransform"];
			return transforms["scale"][side].Exists ? transforms["scale"][side].AsFloat() : 0.95f;
		}

		public bool DoesSlotMatchRecipe(IWorldAccessor world, ItemSlot slots)
		{
			List<ChoppingBlockRecipe> recipes = IDGRecipeRegistry.Loaded.ChoppingBlockRecipes;
			if (recipes == null) return false;

			for (int j = 0; j < recipes.Count; j++)
			{
				if (recipes[j].Matches(Api.World, slots))
				{
					return true;
				}
			}

			return false;
		}

		public ChoppingBlockRecipe GetMatchingChoppingBlockRecipe(IWorldAccessor world, ItemSlot slots, string toolmode)
		{
			List<ChoppingBlockRecipe> recipes = IDGRecipeRegistry.Loaded.ChoppingBlockRecipes;
			if (recipes == null) return null;

			for (int j = 0; j < recipes.Count; j++)
			{
				if (recipes[j].Matches(Api.World, slots) && (recipes[j].ToolMode == toolmode))
				{
					return recipes[j];
				}
			}

			return null;
		}

		public void SpawnOutput(ChoppingBlockRecipe recipe, EntityAgent byEntity, BlockPos pos)
		{
			int j = recipe.Output.StackSize;
			for (int i = j; i > 0; i--)
			{
				Api.World.SpawnItemEntity(new ItemStack(recipe.Output.ResolvedItemstack.Collectible, 1), pos.ToVec3d(), new Vec3d(0.05f, 0.1f, 0.05f));
			}
		}

		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
		{
			//Alter this code to produce an output based on the recipe that results from the held tool and its current mode.
			//If no tool is held, return only contents
		}
		public override void updateMeshes()
        {
			base.updateMeshes();
        }

		private readonly Matrixf mat = new();
	}
}
