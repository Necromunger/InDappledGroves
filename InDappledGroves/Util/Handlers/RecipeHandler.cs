using InDappledGroves.BlockEntities;
using InDappledGroves.CollectibleBehaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OpenTK.Graphics.OpenGL.GL;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using static InDappledGroves.Util.RecipeTools.IDGRecipeNames;
using Vintagestory.API.MathTools;

namespace InDappledGroves.Util.Handlers
{
    public class RecipeHandler
    {
        public RecipeValues recipeValues;
        public float currentMiningDamage { get; set; }

        public float recipeProgress { get; set; }

        public float playNextSound { get; set; }
        public float resistance { get; set; }
        public float lastSecondsUsed { get; set; }

        public float totalSecondsUsed { get; set; }
        public float curDmgFromMiningSpeed { get; set; }
        
        public WorkstationRecipe recipe { get; set; }

        public float toolModeMod { get; set; }

        public ICoreAPI api;

        public IDGBEWorkstation beworkstation { get; set; }

        //public string curTMode { get; set; }

        internal static List<BasicWorkstationRecipe> bwsRecipes = IDGRecipeRegistry.Loaded.BasicWorkstationRecipes;

        internal static List<ComplexWorkstationRecipe> cwsRecipes = IDGRecipeRegistry.Loaded.ComplexWorkstationRecipes;

        public RecipeHandler(ICoreAPI Api, IDGBEWorkstation beworkstation) {
            this.api = Api;
            this.beworkstation = beworkstation;
        }

        public void clearRecipe(bool clearCurrentMiningDamage = true) 
        {
            recipe = null;
            recipeProgress = 0;
            recipeValues = null;
            playNextSound = 0.7f;
            resistance = 0;
            lastSecondsUsed = 0;
            curDmgFromMiningSpeed = 0;
            toolModeMod = 0;
            currentMiningDamage = 0;
            totalSecondsUsed = 0;
            beworkstation.MarkDirty();

        }

        public bool GetMatchingIngredient(IWorldAccessor world, ItemSlot slot, string workstationtype)
        {
            string processmodifiercheck = slot.Itemstack.Collectible.FirstCodePart() + "-" + slot.Itemstack.Collectible.FirstCodePart(1);
            if (workstationtype == "basic")
            {
                if (bwsRecipes != null)
                {
                    for (int j = 0; j < bwsRecipes.Count; j++)
                    {
                        if (bwsRecipes[j].Matches(api.World, slot))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            else if (workstationtype == "complex")
            {
                for (int j = 0; j < cwsRecipes.Count; j++)
                {
                    if (!beworkstation.ProcessModifierSlot.Empty && cwsRecipes[j].Matches(api.World, slot))
                    {
                        //Checks to see if player is holding a valid ingredient or valid processmodifier
                        return true;
                    }
                }
            }

            return false;
        }

        public bool GetMatchingProcessModifier(IWorldAccessor world, ItemSlot slot, string workstationtype)
        {
            string processmodifiercheck = slot.Itemstack.Collectible.FirstCodePart() + "-" + slot.Itemstack.Collectible.FirstCodePart(1);
            
                for (int j = 0; j < cwsRecipes.Count; j++)
                {
                    if (cwsRecipes[j].ProcessModifier == processmodifiercheck)
                    {
                        //Checks to see if player is holding a valid ingredient or valid processmodifier
                        return true;
                    }
                }
            return false;
        }

        public virtual bool GetMatchingRecipes(IWorldAccessor world, ItemSlot slots, string curTMode, string workstationname, string workstationtype, out WorkstationRecipe recipe)
        {
            
            recipe = null;
            if (workstationname == null || workstationtype == null) return false;

            if (workstationtype == "basic")
            {
                for (int j = 0; j < bwsRecipes.Count; j++)
                {
                    if (bwsRecipes[j].Matches(api.World, slots) && bwsRecipes[j].RequiredWorkstation == workstationname && bwsRecipes[j].ToolMode == curTMode)
                    {
                        recipe = bwsRecipes[j];
                        return true;
                    }
                }
            }
            else if (workstationtype == "complex")
            {
                for (int j = 0; j < cwsRecipes.Count; j++)
                {
                    string processmodifier = beworkstation.ProcessModifierSlot.Itemstack.Collectible.FirstCodePart() + "-" + beworkstation.ProcessModifierSlot.Itemstack.Collectible.FirstCodePart(1);
                    if (cwsRecipes[j].Matches(api.World, slots) && (cwsRecipes[j].RequiredWorkstation == workstationname && cwsRecipes[j].ToolMode == curTMode && cwsRecipes[j].ProcessModifier == processmodifier))
                    {
                        recipe = cwsRecipes[j];
                        return true;
                    }
                }
            }

            return false;
        }

        public bool processRecipe(CollectibleObject heldCollectible, ItemSlot activehotbarslot, IPlayer player, BlockPos pos, IDGBEWorkstation beworkstation, float secondsUsed)
        {
            string curTMode = heldCollectible.GetBehavior<BehaviorIDGTool>().GetToolModeName(player.InventoryManager.ActiveHotbarSlot.Itemstack);
            string workstationtype = beworkstation.Block.Attributes["workstationproperties"]["workstationtype"].ToString();
            
                if (recipe != null && recipe.ToolMode != curTMode)
                {
                    return false;
                }
                if (recipe == null)
                {
                    WorkstationRecipe retrRecipe;

                    GetMatchingRecipes(api.World, beworkstation.InputSlot, curTMode, beworkstation.Block.Attributes["inventoryclass"].ToString(), beworkstation.Block.Attributes["workstationproperties"]["workstationtype"].ToString(), out retrRecipe);
                    if (retrRecipe == null)
                    {
                        return false;
                    }

                    recipe = retrRecipe;
                }

                if (recipeValues == null)
                {
                    if (workstationtype == "basic")
                    {
                        recipeValues = new RecipeValues(beworkstation.InputSlot.Itemstack, recipe.IngredientMaterial, recipe.Output.ResolvedItemstack, recipe.ReturnStack.ResolvedItemstack, recipe.BaseToolDmg, null);
                    }
                    else if (workstationtype == "complex")
                    {
                        string processmodifier = beworkstation.ProcessModifierSlot.Itemstack.Collectible.FirstCodePart() + "-" + beworkstation.ProcessModifierSlot.Itemstack.Collectible.FirstCodePart(1);
                        recipeValues = new RecipeValues(beworkstation.InputSlot.Itemstack, recipe.IngredientMaterial, recipe.Output.ResolvedItemstack, recipe.ReturnStack.ResolvedItemstack, recipe.BaseToolDmg, processmodifier);
                    }
                }

                toolModeMod = heldCollectible.GetBehavior<BehaviorIDGTool>().GetToolModeMod(activehotbarslot.Itemstack);
            EntityPlayer entityPlayer = player.Entity;
            entityPlayer.StartAnimation(recipe.Animation);

            if (api.Side == EnumAppSide.Server)
                {
                //TODO Put Recipe And Resistance Getting To A Separate Method
                ItemStack InputStack = recipeValues.InputStack;

                    resistance = (InputStack.Block is Block ? InputStack.Block.Resistance
                    : InputStack.Item.Attributes["resistance"].AsFloat());

                    resistance *= InDappledGroves.baseWorkstationResistanceMult;


                lastSecondsUsed = secondsUsed-lastSecondsUsed < 0?0: lastSecondsUsed;
                    if (InputStack.Collectible is Block)
                    {
                        if ((secondsUsed - lastSecondsUsed) > 0.025)
                        {
                            curDmgFromMiningSpeed +=

                                 (heldCollectible.GetMiningSpeed(entityPlayer.ActiveHandItemSlot.Itemstack, entityPlayer.BlockSelection, InputStack.Block, player as IPlayer)
                                 * /*InDappledGroves.baseWorkstationMiningSpdMult*/ 0.1f) * (secondsUsed - lastSecondsUsed);
                        }
                    }
                    else
                    {
                        curDmgFromMiningSpeed += (heldCollectible.MiningSpeed[(EnumBlockMaterial)recipeValues.ingredientMaterial]) * (secondsUsed - lastSecondsUsed);
                    }




                    if (api.Side == EnumAppSide.Server)
                    {
                        this.recipeProgress = currentMiningDamage / resistance;
                        totalSecondsUsed += secondsUsed - lastSecondsUsed;
                        System.Diagnostics.Debug.WriteLine(totalSecondsUsed);
                        lastSecondsUsed = secondsUsed;
                        currentMiningDamage = totalSecondsUsed + (curDmgFromMiningSpeed * toolModeMod);
                        beworkstation.MarkDirty();
                        //};
                        if (/*api.Side == EnumAppSide.Server && */currentMiningDamage >= resistance)
                        {
                            if (beworkstation.workstationtype == "complex")
                            {
                                if (beworkstation.Block.Attributes["workstationproperties"]["damageprocessmodifier"].AsBool() == true)
                                {
                                    beworkstation.ProcessModifierSlot.Itemstack.Collectible.DamageItem(api.World, player.Entity, beworkstation.ProcessModifierSlot, 1);
                                }
                            }
                            heldCollectible.DamageItem(api.World, entityPlayer, entityPlayer.RightHandItemSlot, recipeValues.baseToolDamage);
                            CompleteRecipe(api, player);
                            beworkstation.MarkDirty();
                            clearRecipe();
                            return true;
                        }
                    }
                }
            
            return false;
        }

        public bool CompleteRecipe(ICoreAPI api, IPlayer byPlayer)
        {
            ItemStack returnStack = recipe.ReturnStack.ResolvedItemstack;
            if (returnStack.Collectible.FirstCodePart() == "air")
            {
                if (beworkstation.InputSlot.Empty) return false;
                beworkstation.InputSlot.Itemstack = null;
                byPlayer.Entity.StopAnimation(recipe.Animation);
                SpawnOutput(recipeValues.output, byPlayer.Entity,byPlayer.Entity.BlockSelection.Position);
                clearRecipe();
                return false; //If no stack is returned, clear stack
            }
            else
            {
                //TODO: Determine if check needed to prevent spawning of excess resources
                beworkstation.InputSlot.Itemstack = null;
                ReturnStackPut(returnStack.Clone(), beworkstation);
                byPlayer.Entity.StopAnimation(recipe.Animation);
                SpawnOutput(recipeValues.output, byPlayer.Entity, byPlayer.Entity.BlockSelection.Position);
                clearRecipe();
                return true; //If a stack is returned from the recipe, allow process to continue after resetting dmg accumulation
            }
        }

        public virtual void ReturnStackPut(ItemStack stack, IDGBEWorkstation workstation)
        {
            if (workstation.InputSlot.Empty)
            {
                workstation.InputSlot.Itemstack = stack;
            }
        }

        public void SpawnOutput(ItemStack output, EntityAgent byEntity, BlockPos pos)
        {
            clearRecipe();
            int j = output.StackSize;
            for (int i = j; i > 0; i--)
            {
                api.World.SpawnItemEntity(new ItemStack(output.Collectible, 1), pos.ToVec3d(), new Vec3d(0.05f, 0.1f, 0.05f));
                
            }
            
        }
    }

    public class RecipeValues
    {
        internal ItemStack InputStack;
        internal int ingredientMaterial;
        internal string processmodifier;
        internal ItemStack output;
        internal ItemStack returnStack;
        internal int baseToolDamage;

        public RecipeValues(ItemStack InputStack, int ingredientMaterial, ItemStack output, ItemStack returnStack, int baseToolDamage, string processmodifier = null)
        {
            this.InputStack = InputStack;
            this.ingredientMaterial = ingredientMaterial;
            this.output = output;
            this.processmodifier = processmodifier;
            this.returnStack = returnStack;
            this.baseToolDamage = baseToolDamage;
        }
    }
}
