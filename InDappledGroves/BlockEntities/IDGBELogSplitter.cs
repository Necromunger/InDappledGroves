using InDappledGroves.Blocks;
using InDappledGroves.CollectibleBehaviors;
using InDappledGroves.Util.Handlers;
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
    class IDGBELogSplitter : IDGBEWorkstation
    {
        public override InventoryBase Inventory { get; }
        public override string InventoryClassName => "logsplitter";
		public override string AttributeTransformCode => "logSplitterTransform";

		static List<LogSplitterRecipe> recipes = IDGRecipeRegistry.Loaded.LogSplitterRecipes;

		public LogSplitterRecipe recipe { get; set; }

        public bool recipecomplete { get; set; } = false;

		public float currentMiningDamage { get; set; }

        public ItemSlot BladeSlot { get; set; }

        public IDGBELogSplitter()
		{

            Inventory = new InventoryDisplayed(this, 2, "logsplitter-slot", null, null);
			BladeSlot = Inventory[0];
			InputSlot = Inventory[1];
            
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            //recipeHandler = new RecipeHandler(api);
        }

        public override bool OnInteract(IPlayer byPlayer)
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
				if (InputSlot.Empty && GetMatchingRecipes(Api.World, activeHotbarSlot))
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
                } else {
					return false;
				}
			}

			if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack != null && byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible.HasBehavior<BehaviorIDGTool>())
			{
				return true;
			}
			return false;
		}


		//TODO: Test to see if the addition of the following methods corrects the syncing issue.
		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
			if (recipeHandler != null)
			{
				recipeHandler.currentMiningDamage = tree.GetFloat("currentMiningDamage");
			}
			else
			{
                currentMiningDamage = tree.GetFloat("currentMiningDamage");
            }
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
				tree.SetFloat("currentMiningDamage", recipeHandler.currentMiningDamage); 
            }
        }

		public LogSplitterRecipe GetMatchingRecipe(IWorldAccessor world, ItemSlot slots, String bladeType, string toolmode)
		{
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
						if (index == 0)
						{
							tfMatrices[index] = new Matrixf().Values;
						} else
						{
						   
                           tfMatrices[index] = new Matrixf().Values;
                        }

                    }
				}

			}
			return tfMatrices;
		}

		internal bool handleRecipe(CollectibleObject heldCollectible, float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
			string curTMode = heldCollectible.GetBehavior<BehaviorIDGTool>().GetToolModeName(slot.Itemstack);
            recipe = GetMatchingRecipe(world, InputSlot, BladeSlot.Itemstack.Collectible.Variant["style"].ToString(), curTMode);
            if (recipe == null)
            {
                return false;
            }
            
            ItemStack itemstack = slot.Itemstack;
            BlockPos position = blockSel.Position;
			if (recipeHandler.recipeValues == null)
			{
				recipeHandler.recipeValues = new RecipeValues(Inventory[1].Itemstack, recipe.IngredientMaterial, recipe.Output.ResolvedItemstack, recipe.ReturnStack.ResolvedItemstack, recipe.BaseToolDmg);
			}

			bool recipeComplete = recipeHandler.processRecipe(heldCollectible, curTMode, slot, byPlayer.Entity, blockSel.Position, secondsUsed);
            
			if (recipeComplete)
			{
				recipeComplete = UpdateInventory(Api, byPlayer, recipe.ReturnStack.ResolvedItemstack, this);
                
            }

            updateMeshes();
            base.MarkDirty(true, null);
            return !recipeComplete;
			
        }

		public override bool UpdateInventory(ICoreAPI api, IPlayer byPlayer, ItemStack returnStack, IDGBEWorkstation workstation)
		{
            BladeSlot.Itemstack.Collectible.DamageItem(api.World, byPlayer.Entity, BladeSlot);
			return base.UpdateInventory(api, byPlayer, returnStack, workstation);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
			if(Api.Side.IsClient()){
				dsc.AppendLine(Lang.GetMatching("indappledgroves:BladeHeldInSplitter") + ": " + (Inventory[0].Empty ? Lang.GetMatching("indappledgroves:Empty") : Inventory[0].Itemstack.GetName()));
				if (!Inventory[0].Empty) dsc.AppendLine(Lang.GetMatching("indappledgroves:BladeDurability") + ": " + Inventory[0].Itemstack.Attributes["durability"]);
				base.GetBlockInfo(forPlayer, dsc);
			};
        }
    }
}
