using InDappledGroves.CollectibleBehaviors;
using InDappledGroves.Util.Handlers;
using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using static InDappledGroves.Util.RecipeTools.IDGRecipeNames;

namespace InDappledGroves.BlockEntities
{
    public class IDGBEWorkstation : BlockEntityDisplay
    {
		public override InventoryBase Inventory { get; }

        protected InventoryGeneric inventory;

        public override string InventoryClassName => Block==null?"workstation":Block.Attributes["inventoryclass"].AsString();
        public override string AttributeTransformCode => Block.Attributes["attributetransformcode"].AsString();

        public string workstationtype => Block.Attributes["workstationproperties"]["workstationtype"].ToString();

        public bool recipecomplete { get; set; } = false;

        public RecipeHandler recipeHandler { get; set; }

        public float currentMiningDamage { get; set; }
        public ItemSlot InputSlot { get { return Inventory[Block.Attributes["workstationproperties"]["slottypes"]["inputslot"].AsInt()];} }

        public ItemSlot ProcessModifierSlot { get { return Block.Attributes["workstationproperties"]["workstationtype"].ToString() == "complex" ? Inventory[Block.Attributes["workstationproperties"]["slottypes"]["processmodifier0"].AsInt()] : null; } }

        public IDGBEWorkstation()
		{
			//Must initialize inventory in derived classes
			Inventory = new InventoryDisplayed(this, 2, InventoryClassName + "-slot", null, null);
		}

		public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (recipeHandler == null)
            {
                recipeHandler = new RecipeHandler(api, this);
            }
            this.capi = (api as ICoreClientAPI);
        }

        public virtual bool OnInteract(IPlayer byPlayer)
        {
            
            ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            bool result = false;
            //If The Players Hand Is Empty
            if (workstationtype == "basic")
            {
                return OnBasicInteract(byPlayer);
            }
            else if (workstationtype == "complex")
            {
                return OnComplexInteract(byPlayer);
            }

            if (result)
            {
                updateMeshes();
                MarkDirty(true);
            }
			return false;
		}


        public virtual bool OnBasicInteract(IPlayer byPlayer)
        {
            ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

                if (activeHotbarSlot.Empty)
                {
                    return this.TryTake(byPlayer, InputSlot);
                }
                else if (recipeHandler.GetMatchingIngredient(Api.World, activeHotbarSlot, workstationtype))
                {
                    return this.TryPut(byPlayer, activeHotbarSlot, InputSlot);
                }

            return false;
        }

        public virtual bool OnComplexInteract(IPlayer byPlayer)
        {
            ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (activeHotbarSlot.Empty)
            {
                if (!InputSlot.Empty)
                {
                    return this.TryTake(byPlayer, InputSlot);
                }
                else
                {
                    return this.TryTake(byPlayer, ProcessModifierSlot);
                }
            }
            else if (recipeHandler.GetMatchingIngredient(Api.World, activeHotbarSlot, workstationtype))
            {
                return this.TryPut(byPlayer, activeHotbarSlot, InputSlot);
            }
            else if (recipeHandler.GetMatchingProcessModifier(Api.World, activeHotbarSlot, workstationtype))
            {
                return this.TryPut(byPlayer, activeHotbarSlot, ProcessModifierSlot);
            }

            return false;
        }

        public virtual bool TryPut(IPlayer byPlayer, ItemSlot slot, ItemSlot targetSlot)
        {
            if (targetSlot.Empty)
            {
                Block block = slot.Itemstack.Block;
                int num3 = slot.TryPutInto(this.Api.World, targetSlot, 1);
                if (num3 > 0)
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
            updateMeshes();
            MarkDirty(true);
            return false;
        }

        public virtual bool TryTake(IPlayer byPlayer,ItemSlot targetSlot)
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
                        recipeHandler.clearRecipe();
                }
					if (itemStack.StackSize > 0)
					{
						this.Api.World.SpawnItemEntity(itemStack, this.Pos.ToVec3d().Add(0.5, 0.5, 0.5), null);
					}
					base.MarkDirty(true, null);
					this.updateMeshes();
					return false;
				}
			return false;
		}

        internal bool handleRecipe(CollectibleObject heldCollectible, float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            
            bool recipeComplete = recipeHandler.processRecipe(heldCollectible, slot, byPlayer, blockSel.Position, this, secondsUsed);

            updateMeshes();
            base.MarkDirty(true, null);
            return !recipeComplete;

        }
       

        private string GetWorkStationType()
        {
            JsonObject attributes = Block.Attributes["workstationproperties"];
            if (attributes.Exists && attributes["workstationtype"].Exists)
            {
                return attributes["workstationtype"].ToString();
            } else
            {
                if (Api.Side.IsClient())
                {
                    capi.Logger.Debug(Lang.GetMatching("WorkstationTypeNotDesignated", Block.Class));
                }
                return null;
            }
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
                        if (index == Block.Attributes["workstationproperties"]["slottypes"]["inputslot"].AsInt())
                        {
                            string blocktype = "specialadjust" + Block.Code.FirstCodePart().ToString();

                            Matrixf matrix = new Matrixf();
                            if (itemstack.Block.Attributes[blocktype].Exists)
                            {
                                JsonObject specialadjust = itemstack.Collectible.Attributes[blocktype];
                                switch (Block.Variant["side"])
                                {
                                    case "east":
                                        matrix.Translate(specialadjust["east"].AsFloat(), 0, 0);
                                        break;
                                    case "west":
                                        matrix.Translate(specialadjust["west"].AsFloat(), 0, 0);
                                        break;
                                    case "north":
                                        matrix.Translate(0, 0, specialadjust["north"].AsFloat());
                                        break;
                                    case "south":
                                        matrix.Translate(0, 0, specialadjust["south"].AsFloat());
                                        break;
                                }
                            }
                            tfMatrices[index] = matrix.Translate(0, 0.0, 0).Translate(0.5, 0.5, 0.5)
                                .RotateYDeg(this.Block.Shape.rotateY)
                                .Translate(-0.5, -0.5, -0.5).Values;
                        } else {
                            tfMatrices[index] = new Matrixf().Translate(0.5, 0.5, 0.5).RotateYDeg(this.Block.Shape.rotateY).Translate(-0.5, -0.5, -0.5).Values;
                        }
                    }
                }
            }
            return tfMatrices;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {

            if (recipeHandler != null)
            {
                tree.SetFloat("lastsecondsused", recipeHandler.lastSecondsUsed);
                tree.SetFloat("recipeprogress", recipeHandler.recipeProgress);
                tree.SetFloat("playnextsound", recipeHandler.playNextSound);
                tree.SetFloat("currentminingdamage", recipeHandler.currentMiningDamage);
                tree.SetFloat("curdmgfromminingspeed", recipeHandler.curDmgFromMiningSpeed);
                tree.SetFloat("totalsecondsused", recipeHandler.totalSecondsUsed);
            }
            base.ToTreeAttributes(tree);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            if (recipeHandler != null)
            {
                recipeHandler.lastSecondsUsed = tree.GetFloat("lastsecondsused");
                recipeHandler.recipeProgress = tree.GetFloat("recipeprogress");
                recipeHandler.currentMiningDamage = tree.GetFloat("currentminingdamage");
                recipeHandler.curDmgFromMiningSpeed = tree.GetFloat("curdmgfromminingspeed");
                recipeHandler.totalSecondsUsed = tree.GetFloat("totalsecondsused");
                recipeHandler.playNextSound = tree.GetFloat("playnextsound");
            }
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            var primary = Block.Variant["primary"];
            var secondary = Block.Variant["secondary"];
            var materials = (Lang.Get("material-" + $"{primary}") + (secondary != null ? " and " + Lang.Get("material-" + $"{secondary}") : ""));
            ItemStack stack = forPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            string curToolMode = stack?.Collectible.GetBehavior<BehaviorIDGTool>()?.GetToolModeName(stack).ToString();
            dsc.AppendLine(Lang.GetMatching("indappledgroves:workstationWorkItem") + ": " + (InputSlot.Empty ? Lang.GetMatching("indappledgroves:Empty") : InputSlot.Itemstack.GetName()));
            if (workstationtype == "complex" && !ProcessModifierSlot.Empty)
            {
                dsc.AppendLine("Process Modifier Type:" + ProcessModifierSlot.Itemstack.GetName());
                dsc.AppendLine("Remaining Durability: " + ProcessModifierSlot.Itemstack.Attributes["durability"]);
            }
            if (ClientSettings.ExtendedDebugInfo) { 
                dsc.AppendLine("Recipe Progress: " + Math.Round((recipeHandler.recipeProgress) * 100) + "%");
                    
                    dsc.AppendLine(string.Format($"{materials}"));
                    dsc.AppendLine("Attribute Transform Code:" + AttributeTransformCode);
                    dsc.AppendLine("Inventory Class Name:" + InventoryClassName);
                    dsc.AppendLine("Current Tool Mode: " + curToolMode);
                if (recipeHandler.recipe != null)
                {
                    dsc.AppendLine("Recipe Output:" + recipeHandler.recipe.Output.ResolvedItemstack.ToString());
                    dsc.AppendLine("Recipe ToolMode:" + recipeHandler.recipe.ToolMode.ToString());
                    dsc.AppendLine("Recipe Workstation:" + recipeHandler.recipe.RequiredWorkstation.ToString());
                }
            }
        }
	}
}

