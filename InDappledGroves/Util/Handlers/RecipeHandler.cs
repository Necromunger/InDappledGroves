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
    internal class RecipeHandler
    {
        public RecipeValues recipeValues;
        public float currentMiningDamage { get; set; }

        public float recipeProgress { get; set; }

        public float playNextSound { get; set; }
        public float resistance { get; set; }
        public float lastSecondsUsed { get; set; }
        public float curDmgFromMiningSpeed { get; set; }
        
        public float toolModeMod { get; set; }

        public ICoreAPI api;

        public string curTMode { get; set; }

        public RecipeHandler(ICoreAPI Api) {
            this.api = Api;
        }

        public void clearRecipe(bool clearCurrentMiningDamage = true) 
        {
            recipeValues = null;
            playNextSound = 0.7f;
            resistance = 0;
            lastSecondsUsed = 0;
            curDmgFromMiningSpeed = 0;
            toolModeMod = 0;
            if (clearCurrentMiningDamage)
            {
                recipeProgress = 0;
                clearcurrentMiningDamage();
            }
        }
        public void clearcurrentMiningDamage()
        {
            currentMiningDamage = 0;
        }


        public bool processRecipe(CollectibleObject heldCollectible, string curTMode, ItemSlot slot, EntityPlayer player, BlockPos pos, float secondsUsed)
        {
                toolModeMod = heldCollectible.GetBehavior<BehaviorIDGTool>().GetToolModeMod(slot.Itemstack);

                //TODO Put Recipe And Resistance Getting To A Separate Method
                ItemStack InputStack = recipeValues.InputStack;

                resistance = (InputStack.Block is Block ? InputStack.Block.Resistance
                : InputStack.Item.Attributes["resistance"].AsFloat());

                resistance *= InDappledGroves.baseWorkstationResistanceMult;

                player.StartAnimation("axesplit-fp");

                if (InputStack.Collectible is Block)
                {
                    if ((secondsUsed - lastSecondsUsed) > 0.025)
                    {
                        curDmgFromMiningSpeed +=

                             (heldCollectible.GetMiningSpeed(player.ActiveHandItemSlot.Itemstack,        player.BlockSelection, InputStack.Block, player as IPlayer)
                             * /*InDappledGroves.baseWorkstationMiningSpdMult*/ 0.1f) * (secondsUsed - lastSecondsUsed);
                    }
                }
                else
                {
                    curDmgFromMiningSpeed += (heldCollectible.MiningSpeed[(EnumBlockMaterial)recipeValues.ingredientMaterial]) * (secondsUsed - lastSecondsUsed);
                }
                lastSecondsUsed = secondsUsed;
            currentMiningDamage =
                    secondsUsed + (curDmgFromMiningSpeed * toolModeMod);


            recipeProgress = currentMiningDamage / resistance;
                if (api.Side == EnumAppSide.Server && currentMiningDamage >= resistance)
                {
                    SpawnOutput(recipeValues.output, player, player.BlockSelection.Position);
                    heldCollectible.DamageItem(api.World, player, player.RightHandItemSlot, recipeValues.baseToolDamage);
                    return true;
                }
            
            return false;
        }

        public bool UpdateInventory(ICoreAPI api, IPlayer byPlayer, ItemStack returnStack, IDGBEWorkstation workstation)
        {

            if (returnStack.Collectible.FirstCodePart() == "air")
            {
                if (workstation.InputSlot.Empty) return false;
                workstation.InputSlot.Itemstack = null;

                clearRecipe();
                return false; //If no stack is returned, clear stack
            }
            else
            {
                //TODO: Determine if check needed to prevent spawning of excess resources
                workstation.InputSlot.Itemstack = null;
                ReturnStackPut(returnStack.Clone(), workstation);
                clearRecipe(); //Reset recipe handler to begin next stage.
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
            int j = output.StackSize;
            for (int i = j; i > 0; i--)
            {
                api.World.SpawnItemEntity(new ItemStack(output.Collectible, 1), pos.ToVec3d(), new Vec3d(0.05f, 0.1f, 0.05f));
            }
            
        }
    }

    internal class RecipeValues
    {
        internal ItemStack InputStack;
        internal int ingredientMaterial;
        internal ItemStack output;
        internal ItemStack returnStack;
        internal int baseToolDamage;

        public RecipeValues(ItemStack InputStack, int ingredientMaterial, ItemStack output, ItemStack returnStack, int baseToolDamage)
        {
            this.InputStack = InputStack;
            this.ingredientMaterial = ingredientMaterial;
            this.output = output;
            this.returnStack = returnStack;
            this.baseToolDamage = baseToolDamage;
        }
    }
}
