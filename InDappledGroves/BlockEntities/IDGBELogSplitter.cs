using InDappledGroves.CollectibleBehaviors;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using static InDappledGroves.Util.RecipeTools.IDGRecipeNames;
using static OpenTK.Graphics.OpenGL.GL;

namespace InDappledGroves.BlockEntities
{
    class IDGBELogSplitter : BlockEntityDisplay
    {
		public override InventoryBase Inventory { get; }
		//public override string InventoryClassName => "logsplitter";
		public override string InventoryClassName => Block.Attributes["inventoryclass"].AsString();
		public override string AttributeTransformCode => "logSplitterTransform";

		static List<LogSplitterRecipe> logSplitterrecipes = IDGRecipeRegistry.Loaded.LogSplitterRecipes;

        public IDGBELogSplitter()
		{
			//Updated LogSplitter Inventory Slots to 2
			Inventory = new InventoryDisplayed(this, 2, "logsplitter-slot", null, null);
		}

		public override void Initialize(ICoreAPI api)
		{
			base.Initialize(api);
			this.capi = (api as ICoreClientAPI);
        }

        public ItemSlot BladeSlot
        {
            get { return Inventory[0]; }
        }

        public ItemSlot InputSlot
		{
			get { return Inventory[1]; }
		}

		public bool OnInteract(IPlayer byPlayer)
		{
			ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
			if (!activeHotbarSlot.Empty)
			{
				if (BladeSlot.Empty)
				{
					if (activeHotbarSlot.Itemstack.Collectible.FirstCodePart() == "splitterblade")
					{
						return TryPut(byPlayer, activeHotbarSlot, BladeSlot);
						
					}
					if (byPlayer.Entity.Api is ICoreClientAPI capi)
					{
						capi.TriggerIngameError(this, "LogSplitterNoBladeError", "Log Splitter Lacks A Blade");
					}
					return false;
				}
				if (InputSlot.Empty && DoesSlotMatchRecipe(Api.World, activeHotbarSlot))
				{
					return TryPut(byPlayer, activeHotbarSlot, InputSlot);
				}
			}

			if (activeHotbarSlot.Empty)
			{
				if (!InputSlot.Empty)
				{
					return TryTake(byPlayer, InputSlot);
				} else if (!BladeSlot.Empty)
				{
					return TryTake(byPlayer, BladeSlot);
                }
			}

			if (byPlayer.Entity.Api is ICoreClientAPI capi2)
			{
				capi2.TriggerChatMessage("Reached End Of OnInteract");
			}

			if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack!= null && byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible.HasBehavior<BehaviorIDGTool>())
			{
				return true;
			}
			return false;
		}

		public void ReturnStackPut(ItemStack stack)
		{
			if (this.Inventory[0].Empty)
			{
				this.Inventory[0].Itemstack = stack;
			}
		}

		public bool TryPut(IPlayer byPlayer, ItemSlot slot, ItemSlot targetSlot)
		{
			for (int i = 0; i < Inventory.Count; i++)
			{
				if (targetSlot.Empty)
				{
					Block block = slot.Itemstack.Block;
					int num3 = slot.TryPutInto(this.Api.World, targetSlot, 1);
					if(num3 > 0)
					{
							
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
                    };
					
				}
			}
			return false;
		}

		//TODO: Test to see if the addition of the following methods corrects the syncing issue.
		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
			updateMeshes();
			MarkDirty(true);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            if (Inventory != null)
            {
                ITreeAttribute treeAttribute = new TreeAttribute();
                Inventory.ToTreeAttributes(treeAttribute);
                tree["inventory"] = treeAttribute;
            }
        }

        private bool TryTake(IPlayer byPlayer, ItemSlot targetSlot)
		{
				if (!targetSlot.Empty)
				{
					ItemStack itemStack = targetSlot.TakeOut(1);
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
                    return false;
                }

                    return false;
				}
            updateMeshes();
            Inventory.MarkSlotDirty(0);
            MarkDirty(true);
            return false;
		}

		#region ProcessTransform
		/// <summary>
		/// Processes the transform.
		/// </summary>
		/// <param name="transform">The transform.</param>
		/// <param name="side">The side.</param>
		/// <returns></returns>
		/// 

		public ModelTransform genTransform(ItemStack stack)
		{
			MeshData meshData;
			String side = Block.Variant["side"];
			if (stack != null && stack.Collectible is IContainedMeshSource containedMeshSource)
			{

				meshData = containedMeshSource.GenMesh(stack, this.capi.BlockTextureAtlas, this.Pos);
				meshData.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, base.Block.Shape.rotateY * 0.017453292f, 0f);
			}
			else if (capi != null)
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

			ModelTransform transform = null;
			if (transform == null)
			{
				transform = new ModelTransform
				{
					Translation = new Vec3f(),
					Rotation = new Vec3f(0f, 0f, 0f),
					Origin = new Vec3f(0.5f, 3f, 0.5f), 
					Scale = 0.75f
				};

                return transform;
            } else
			{
                transform.Scale = 2f;
            }

            

            transform.EnsureDefaultValues();

			if (stack != null) transform = ProcessTransform(transform, side);
			return transform;
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
			JsonObject transforms = this.Inventory[0].Itemstack.Collectible.Attributes["workStationTransforms"]["idgLogSplitterProps"]["idgLogSplitterTransform"];
			return transforms["rotation"][sideAxis].Exists ? transforms["rotation"][sideAxis].AsFloat() : 0f;
		}

		public float AddTranslate(string sideAxis)
		{
			JsonObject transforms = this.Inventory[0].Itemstack.Collectible.Attributes["workStationTransforms"]["idgLogSplitterProps"]["idgLogSplitterTransform"];
			return transforms["translation"][sideAxis].Exists ? transforms["translation"][sideAxis].AsFloat() : 0f;
		}

		public float AddScale(string side)
		{
			JsonObject transforms = this.Inventory[0].Itemstack.Collectible.Attributes["workStationTransforms"]["idgLogSplitterProps"]["idgLogSplitterTransform"];
			return transforms["scale"][side].Exists ? transforms["scale"][side].AsFloat() : 0.95f;
		}
		#endregion

		public bool DoesSlotMatchRecipe(IWorldAccessor world, ItemSlot slots)
		{
			List<LogSplitterRecipe> recipes = IDGRecipeRegistry.Loaded.LogSplitterRecipes;
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

		public LogSplitterRecipe GetMatchingLogSplitterRecipe(IWorldAccessor world, ItemSlot slots, String bladeType, string toolmode)
		{
			List<LogSplitterRecipe> recipes = IDGRecipeRegistry.Loaded.LogSplitterRecipes;
			if (recipes == null) return null;

			for (int j = 0; j < recipes.Count; j++)
			{
				if (recipes[j].Matches(Api.World, slots) && (recipes[j].BladeType == bladeType) 
					&& (recipes[j].ToolMode == toolmode))
				{
					return recipes[j];
				}
			}

			return null;
		}

		public void SpawnOutput(LogSplitterRecipe recipe, EntityAgent byEntity, BlockPos pos)
		{
			int j = recipe.Output.StackSize;
			for (int i = j; i > 0; i--)
			{
				Api.World.SpawnItemEntity(new ItemStack(recipe.Output.ResolvedItemstack.Collectible, 1), pos.ToVec3d(), new Vec3d(0.05f, 0.1f, 0.05f));
			}
			updateMeshes();
            base.MarkDirty(true, null);
        }

		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
		{
			
				dsc.AppendLine(Lang.GetMatching("indappledgroves:BladeHeldInSplitter") + ": " + (Inventory[0].Empty?Lang.GetMatching("indappledgroves:Empty") :Inventory[0].Itemstack.GetName()));
				if (!Inventory[0].Empty) dsc.AppendLine(Lang.GetMatching("indappledgroves:BladeDurability") + ": " + Inventory[0].Itemstack.Attributes["durability"]);
				dsc.AppendLine(Lang.GetMatching("indappledgroves:LogHeldInSplitter") + ": " + (Inventory[1].Empty ? Lang.GetMatching("indappledgroves:Empty") : Inventory[1].Itemstack.GetName()));
        }

		protected override float[][] genTransformationMatrices()
		{
			float[][] tfMatrices = new float[Inventory.Count][];
			for (int index = 0; index < Inventory.Count; index++)
			{

				ItemSlot itemSlot = this.Inventory[index];
				JsonObject jsonObject;
				if (itemSlot == null)
				{
					jsonObject = null;
				}
				else
				{
					ItemStack itemstack = itemSlot.Itemstack;
					if (itemstack == null)
					{
						jsonObject = null;
					}
					else
					{
						CollectibleObject collectible = itemstack.Collectible;
						jsonObject = ((collectible != null) ? collectible.Attributes : null);
						if(index == 0)
						{
                            tfMatrices[index] = new Matrixf().Set(genTransform(itemstack).AsMatrix).Values;
                        } else
						{
                            tfMatrices[index] = new Matrixf().Set(genTransform(itemstack).AsMatrix).Values;
                        }
						
					}
				}
				
			}
			return tfMatrices;
		}
	}
}
