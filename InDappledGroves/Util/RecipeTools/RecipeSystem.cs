using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;
using Vintagestory.GameContent;
using static InDappledGroves.Util.RecipeTools.IDGRecipeNames.IDGRecipeLoader;
using System.Security.Cryptography.X509Certificates;
using InDappledGroves.BlockEntities;

namespace InDappledGroves.Util.RecipeTools
{
    public class IDGRecipeNames : ICookingRecipeNamingHelper
    {
        public string GetNameForIngredients(IWorldAccessor worldForResolve, string recipeCode, ItemStack[] stacks)
        {
            return "Cheat Code";
        }

        public class IDGRecipeRegistry
        {
            private static IDGRecipeRegistry loaded;
            private List<BasicWorkstationRecipe> workstationrecipes = new List<BasicWorkstationRecipe>();
            private List<GroundRecipe> groundRecipes = new List<GroundRecipe>();
            private List<ComplexWorkstationRecipe> logSplitterRecipes = new List<ComplexWorkstationRecipe>();


            public List<BasicWorkstationRecipe> BasicWorkstationRecipes
            {
                get
                {
                    return workstationrecipes;
                }
                set
                {
                    workstationrecipes = value;
                }
            }

            public List<ComplexWorkstationRecipe> ComplexWorkstationRecipes
            {
                get
                {
                    return logSplitterRecipes;
                }
                set
                {
                    logSplitterRecipes = value;
                }
            }

            public List<GroundRecipe> GroundRecipes
            {
                get
                {
                    return groundRecipes;
                }
                set
                {
                    groundRecipes = value;
                }
            }

            public static IDGRecipeRegistry Create()
            {
                if (loaded == null)
                {
                    loaded = new IDGRecipeRegistry();
                }
                return Loaded;
            }

            public static IDGRecipeRegistry Loaded
            {
                get
                {
                    if (loaded == null)
                    {
                        loaded = new IDGRecipeRegistry();
                    }
                    return loaded;
                }
            }

            public static void Dispose()
            {
                if (loaded == null) return;
                loaded = null;
            }
        }

        public class IDGRecipeLoader : RecipeLoader
        {
            public ICoreServerAPI api;

            public override double ExecuteOrder()
            {
                return 100;
            }

            public override void AssetsFinalize(ICoreAPI capi)
            {
                IDGRecipeRegistry.Create();
                LoadIDGRecipes();
                base.AssetsFinalize(api);
            }

            public override void AssetsLoaded(ICoreAPI api)
            {
                //override to prevent double loading
                if (!(api is ICoreServerAPI sapi)) return;
                this.api = sapi;
            }

            public override void Dispose()
            {
                base.Dispose();
                IDGRecipeRegistry.Dispose();
            }

            public void LoadIDGRecipes()
            {
                api.World.Logger.StoryEvent(Lang.Get("indappledgroves:The Tyee and the bullcook..."));
                LoadGroundRecipes();
                LoadWorkStationRecipes("choppingblock");
                LoadWorkStationRecipes("sawbuck");
                LoadComplexWorkstationRecipes("logsplitter");
            }

            #region WorkStation Base Recipes
            public void LoadWorkStationRecipes(string recipedirectory)
            {
                Dictionary<AssetLocation, JToken> files = api.Assets.GetMany<JToken>(api.Server.Logger, "recipes/" + recipedirectory);
                int recipeQuantity = 0;
                int ignored = 0;
                int orphaned = 0;
                int wrongWorkstation = 0;
                foreach (var val in files)
                {
                    if (val.Value is JObject)
                    {
                        BasicWorkstationRecipe rec = val.Value.ToObject<BasicWorkstationRecipe>();
                        if (!rec.Enabled) continue;
                        if (rec.RequiredWorkstation == null) continue;
                        LoadWorkStationRecipe(val.Key, rec, ref recipeQuantity, ref ignored, recipedirectory);
                    }
                    if (val.Value is JArray)
                    {
                        foreach (var token in val.Value as JArray)
                        {
                            BasicWorkstationRecipe rec = token.ToObject<BasicWorkstationRecipe>();
                            if (!rec.Enabled) continue;
                            if (rec.RequiredWorkstation == "none") { orphaned++; continue; };
                            if (rec.RequiredWorkstation != recipedirectory) { wrongWorkstation++;};
                            LoadWorkStationRecipe(val.Key, rec, ref recipeQuantity, ref ignored, recipedirectory);
                        }
                    }
                }

                api.World.Logger.Event("{0} " + recipedirectory + " recipes loaded", recipeQuantity);
                api.World.Logger.Event("{0} " + recipedirectory + " recipes orphaned", orphaned);
                api.World.Logger.Event("{0} " + recipedirectory + " have workstation that does not match directory.", wrongWorkstation);
                //api.World.Logger.StoryEvent(Lang.Get("indappledgroves:...with sturdy haft and bit"));
            }

            public void LoadWorkStationRecipe(AssetLocation path, BasicWorkstationRecipe recipe, ref int quantityRegistered, ref int quantityIgnored, String classname)
            {
                if (!recipe.Enabled) return;
                if (recipe.Name == null) recipe.Name = path;
                string className = classname + " recipe";

                Dictionary<string, string[]> nameToCodeMapping = recipe.GetNameToCodeMapping(api.World);

                if (nameToCodeMapping.Count > 0)
                {
                    List<BasicWorkstationRecipe> subRecipes = new List<BasicWorkstationRecipe>();

                    int qCombs = 0;
                    bool first = true;
                    foreach (var val2 in nameToCodeMapping)
                    {
                        if (first) qCombs = val2.Value.Length;
                        else qCombs *= val2.Value.Length;
                        first = false;
                    }

                    first = true;
                    foreach (var val2 in nameToCodeMapping)
                    {
                        string variantCode = val2.Key;
                        string[] variants = val2.Value;

                        for (int i = 0; i < qCombs; i++)
                        {
                            BasicWorkstationRecipe rec;

                            if (first) subRecipes.Add(rec = recipe.Clone());
                            else rec = subRecipes[i];

                            if (rec.Ingredients != null)
                            {
                                foreach (var ingreds in rec.Ingredients)
                                {
                                    if (ingreds.Inputs.Length <= 0) continue;
                                    CraftingRecipeIngredient ingred = ingreds.Inputs[0];

                                    if (ingred.Name == variantCode)
                                    {
                                        ingred.Code = ingred.Code.CopyWithPath(ingred.Code.Path.Replace("*", variants[i % variants.Length]));
                                    }
                                }
                            }

                            rec.ReturnStack.FillPlaceHolder(val2.Key, variants[i % variants.Length]);
                            rec.Output.FillPlaceHolder(val2.Key, variants[i % variants.Length]);
                        }

                        first = false;
                    }

                    if (subRecipes.Count == 0)
                    {
                        api.World.Logger.Warning("{1} file {0} make uses of wildcards, but no blocks or item matching those wildcards were found.", path, className);
                    }

                    foreach (BasicWorkstationRecipe subRecipe in subRecipes)
                    {
                        if (!subRecipe.Resolve(api.World, className + " " + path))
                        {
                            quantityIgnored++;
                            continue;
                        }
                        IDGRecipeRegistry.Loaded.BasicWorkstationRecipes.Add(subRecipe);
                        quantityRegistered++;
                    }

                }
                else
                {
                    if (!recipe.Resolve(api.World, className + " " + path))
                    {
                        quantityIgnored++;
                        return;
                    }

                    IDGRecipeRegistry.Loaded.BasicWorkstationRecipes.Add(recipe);
                    quantityRegistered++;
                }
            }

            public class WorkStationIngredient : IByteSerializable
            {
                public CraftingRecipeIngredient[] Inputs;

                public CraftingRecipeIngredient GetMatch(ItemStack stack)
                {
                    if (stack == null) return null;

                    for (int i = 0; i < Inputs.Length; i++)
                    {
                        if (Inputs[i].SatisfiesAsIngredient(stack)) return Inputs[i];
                    }

                    return null;
                }

                public bool Resolve(IWorldAccessor world, string debug)
                {
                    bool ok = true;

                    for (int i = 0; i < Inputs.Length; i++)
                    {
                        ok &= Inputs[i].Resolve(world, debug);
                    }

                    return ok;
                }

                public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
                {
                    Inputs = new CraftingRecipeIngredient[reader.ReadInt32()];

                    for (int i = 0; i < Inputs.Length; i++)
                    {
                        Inputs[i] = new CraftingRecipeIngredient();
                        Inputs[i].FromBytes(reader, resolver);
                        Inputs[i].Resolve(resolver, "Workstation Ingredient (FromBytes)");
                    }
                }

                public void ToBytes(BinaryWriter writer)
                {
                    writer.Write(Inputs.Length);
                    for (int i = 0; i < Inputs.Length; i++)
                    {
                        Inputs[i].ToBytes(writer);
                    }
                }

                public WorkStationIngredient Clone()
                {
                    CraftingRecipeIngredient[] newings = new CraftingRecipeIngredient[Inputs.Length];

                    for (int i = 0; i < Inputs.Length; i++)
                    {
                        newings[i] = Inputs[i].Clone();
                    }

                    return new WorkStationIngredient()
                    {
                        Inputs = newings
                    };
                }
            }
            #endregion



            #region Splitter Recipes
            public void LoadComplexWorkstationRecipes(string classname)
            {
                Dictionary<AssetLocation, JToken> files = api.Assets.GetMany<JToken>(api.Server.Logger, "recipes/" + classname);
                int recipeQuantity = 0;
                int ignored = 0;

                foreach (var val in files)
                {
                    if (val.Value is JObject)
                    {
                        ComplexWorkstationRecipe rec = val.Value.ToObject<ComplexWorkstationRecipe>();
                        if (!rec.Enabled) continue;

                        LoadComplexWorkstationRecipe(val.Key, rec, ref recipeQuantity, ref ignored, classname);
                    }
                    if (val.Value is JArray)
                    {
                        foreach (var token in val.Value as JArray)
                        {
                            ComplexWorkstationRecipe rec = token.ToObject<ComplexWorkstationRecipe>();
                            if (!rec.Enabled) continue;

                            LoadComplexWorkstationRecipe(val.Key, rec, ref recipeQuantity, ref ignored, classname);
                        }
                    }
                }

                api.World.Logger.Event("{0} " + classname + "splitter recipes loaded", recipeQuantity);
                //api.World.Logger.StoryEvent(Lang.Get("indappledgroves:working sole and blade..."));
            }

            public void LoadComplexWorkstationRecipe(AssetLocation path, ComplexWorkstationRecipe recipe, ref int quantityRegistered, ref int quantityIgnored, string classname)
            {
                if (!recipe.Enabled) return;
                if (recipe.Name == null) recipe.Name = path;

                Dictionary<string, string[]> nameToCodeMapping = recipe.GetNameToCodeMapping(api.World);

                if (nameToCodeMapping.Count > 0)
                {
                    List<ComplexWorkstationRecipe> subRecipes = new List<ComplexWorkstationRecipe>();

                    int qCombs = 0;
                    bool first = true;
                    foreach (var val2 in nameToCodeMapping)
                    {
                        if (first) qCombs = val2.Value.Length;
                        else qCombs *= val2.Value.Length;
                        first = false;
                    }

                    first = true;
                    foreach (var val2 in nameToCodeMapping)
                    {
                        string variantCode = val2.Key;
                        string[] variants = val2.Value;

                        for (int i = 0; i < qCombs; i++)
                        {
                            ComplexWorkstationRecipe rec;

                            if (first) subRecipes.Add(rec = recipe.Clone());
                            else rec = subRecipes[i];

                            if (rec.Ingredients != null)
                            {
                                foreach (var ingreds in rec.Ingredients)
                                {
                                    if (ingreds.Inputs.Length <= 0) continue;
                                    CraftingRecipeIngredient ingred = ingreds.Inputs[0];

                                    if (ingred.Name == variantCode)
                                    {
                                        ingred.Code = ingred.Code.CopyWithPath(ingred.Code.Path.Replace("*", variants[i % variants.Length]));
                                    }
                                }
                            }

                            rec.ReturnStack.FillPlaceHolder(val2.Key, variants[i % variants.Length]);
                            rec.Output.FillPlaceHolder(val2.Key, variants[i % variants.Length]);
                        }

                        first = false;
                    }

                    if (subRecipes.Count == 0)
                    {
                        api.World.Logger.Warning("{1} file {0} make uses of wildcards, but no blocks or item matching those wildcards were found.", path, classname);
                    }

                    foreach (ComplexWorkstationRecipe subRecipe in subRecipes)
                    {
                        if (!subRecipe.Resolve(api.World, classname + " " + path))
                        {
                            quantityIgnored++;
                            continue;
                        }
                        IDGRecipeRegistry.Loaded.ComplexWorkstationRecipes.Add(subRecipe);
                        quantityRegistered++;
                    }

                }
                else
                {
                    if (!recipe.Resolve(api.World, classname + " " + path))
                    {
                        quantityIgnored++;
                        return;
                    }

                    IDGRecipeRegistry.Loaded.ComplexWorkstationRecipes.Add(recipe);
                    quantityRegistered++;
                }
            }
            #endregion

            #region Ground Recipes
            public void LoadGroundRecipes()
            {
                Dictionary<AssetLocation, JToken> files = api.Assets.GetMany<JToken>(api.Server.Logger, "recipes/ground");
                int recipeQuantity = 0;
                int ignored = 0;

                foreach (var val in files)
                {
                    if (val.Value is JObject)
                    {
                        GroundRecipe rec = val.Value.ToObject<GroundRecipe>();
                        if (!rec.Enabled) continue;

                        LoadGroundRecipe(val.Key, rec, ref recipeQuantity, ref ignored);
                    }
                    if (val.Value is JArray)
                    {
                        foreach (var token in val.Value as JArray)
                        {
                            GroundRecipe rec = token.ToObject<GroundRecipe>();
                            if (!rec.Enabled) continue;

                            LoadGroundRecipe(val.Key, rec, ref recipeQuantity, ref ignored);
                        }
                    }
                }

                api.World.Logger.Event("{0} ground recipes loaded", recipeQuantity);
                //api.World.Logger.StoryEvent(Lang.Get("indappledgroves:working sole and blade..."));
            }

            public void LoadGroundRecipe(AssetLocation path, GroundRecipe recipe, ref int quantityRegistered, ref int quantityIgnored)
            {
                if (!recipe.Enabled) return;
                if (recipe.Name == null) recipe.Name = path;
                string className = "ground recipe";

                Dictionary<string, string[]> nameToCodeMapping = recipe.GetNameToCodeMapping(api.World);

                if (nameToCodeMapping.Count > 0)
                {
                    List<GroundRecipe> subRecipes = new List<GroundRecipe>();

                    int qCombs = 0;
                    bool first = true;
                    foreach (var val2 in nameToCodeMapping)
                    {
                        if (first) qCombs = val2.Value.Length;
                        else qCombs *= val2.Value.Length;
                        first = false;
                    }

                    first = true;
                    foreach (var val2 in nameToCodeMapping)
                    {
                        string variantCode = val2.Key;
                        string[] variants = val2.Value;

                        for (int i = 0; i < qCombs; i++)
                        {
                            GroundRecipe rec;

                            if (first) subRecipes.Add(rec = recipe.Clone());
                            else rec = subRecipes[i];

                            if (rec.Ingredients != null)
                            {
                                foreach (var ingreds in rec.Ingredients)
                                {
                                    if (ingreds.Inputs.Length <= 0) continue;
                                    CraftingRecipeIngredient ingred = ingreds.Inputs[0];

                                    if (ingred.Name == variantCode)
                                    {
                                        ingred.Code = ingred.Code.CopyWithPath(ingred.Code.Path.Replace("*", variants[i % variants.Length]));
                                    }
                                }
                            }

                            rec.ReturnStack.FillPlaceHolder(val2.Key, variants[i % variants.Length]);
                            rec.Output.FillPlaceHolder(val2.Key, variants[i % variants.Length]);
                        }

                        first = false;
                    }

                    if (subRecipes.Count == 0)
                    {
                        api.World.Logger.Warning("{1} file {0} make uses of wildcards, but no blocks or item matching those wildcards were found.", path, className);
                    }

                    foreach (GroundRecipe subRecipe in subRecipes)
                    {
                        if (!subRecipe.Resolve(api.World, className + " " + path))
                        {
                            quantityIgnored++;
                            continue;
                        }
                        IDGRecipeRegistry.Loaded.GroundRecipes.Add(subRecipe);
                        quantityRegistered++;
                    }

                }
                else
                {
                    if (!recipe.Resolve(api.World, className + " " + path))
                    {
                        quantityIgnored++;
                        return;
                    }

                    IDGRecipeRegistry.Loaded.GroundRecipes.Add(recipe);
                    quantityRegistered++;
                }
            }
            public class GroundIngredient : IByteSerializable
            {
                public CraftingRecipeIngredient[] Inputs;

                public CraftingRecipeIngredient GetMatch(ItemStack stack)
                {
                    if (stack == null) return null;

                    for (int i = 0; i < Inputs.Length; i++)
                    {
                        if (Inputs[i].SatisfiesAsIngredient(stack)) return Inputs[i];
                    }

                    return null;
                }

                public bool Resolve(IWorldAccessor world, string debug)
                {
                    bool ok = true;

                    for (int i = 0; i < Inputs.Length; i++)
                    {
                        ok &= Inputs[i].Resolve(world, debug);
                    }

                    return ok;
                }

                public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
                {
                    Inputs = new CraftingRecipeIngredient[reader.ReadInt32()];

                    for (int i = 0; i < Inputs.Length; i++)
                    {
                        Inputs[i] = new CraftingRecipeIngredient();
                        Inputs[i].FromBytes(reader, resolver);
                        Inputs[i].Resolve(resolver, "Ground Ingredient (FromBytes)");
                    }
                }

                /// <summary>Converts to bytes.</summary>
                /// <param name="writer">The writer.</param>
                public void ToBytes(BinaryWriter writer)
                {
                    writer.Write(Inputs.Length);
                    for (int i = 0; i < Inputs.Length; i++)
                    {
                        Inputs[i].ToBytes(writer);
                    }
                }

                public GroundIngredient Clone()
                {
                    CraftingRecipeIngredient[] newings = new CraftingRecipeIngredient[Inputs.Length];

                    for (int i = 0; i < Inputs.Length; i++)
                    {
                        newings[i] = Inputs[i].Clone();
                    }

                    return new GroundIngredient()
                    {
                        Inputs = newings
                    };
                }
            }
            #endregion           

        }

        public class WorkstationRecipe : IByteSerializable
        {
            public string Code = "Work Station Recipe";

            public virtual AssetLocation Name { get; set; }
            public virtual bool Enabled { get; set; } = true;

            public virtual int BaseToolDmg { get; set; } = 1;
            public virtual string ToolMode { get; set; } = "none";

            public virtual string Animation { get; set; } = "axesplit-fp";

            public virtual string RequiredWorkstation { get; set; } = "none";

            public virtual int IngredientMaterial { get; set; } = 4;
            public  virtual double IngredientResistance { get; set; } = 4.0;

            


            public WorkStationIngredient[] Ingredients;

            public JsonItemStack Output;

            public JsonItemStack ReturnStack = new JsonItemStack() { Code = new AssetLocation("air"), Type = EnumItemClass.Block };

            public ItemStack TryCraftNow(ICoreAPI api, ItemSlot inputslots)
            {

                var matched = pairInput(inputslots);

                ItemStack mixedStack = Output.ResolvedItemstack.Clone();
                mixedStack.StackSize = getOutputSize(matched);

                if (mixedStack.StackSize <= 0) return null;


                foreach (var val in matched)
                {
                    val.Key.TakeOut(val.Value.Quantity * (mixedStack.StackSize / Output.StackSize));
                    val.Key.MarkDirty();
                }

                return mixedStack;
            }

            public bool Matches(IWorldAccessor worldForResolve, ItemSlot inputSlots)
            {
                int outputStackSize = 0;

                List<KeyValuePair<ItemSlot, CraftingRecipeIngredient>> matched = pairInput(inputSlots);
                if (matched == null) return false;

                outputStackSize = getOutputSize(matched);

                return outputStackSize >= 0;
            }

            protected List<KeyValuePair<ItemSlot, CraftingRecipeIngredient>> pairInput(ItemSlot inputStacks)
            {
                List<int> alreadyFound = new List<int>();

                Queue<ItemSlot> inputSlotsList = new Queue<ItemSlot>();
                if (!inputStacks.Empty) inputSlotsList.Enqueue(inputStacks);

                if (inputSlotsList.Count != Ingredients.Length) return null;

                List<KeyValuePair<ItemSlot, CraftingRecipeIngredient>> matched = new List<KeyValuePair<ItemSlot, CraftingRecipeIngredient>>();

                while (inputSlotsList.Count > 0)
                {
                    ItemSlot inputSlot = inputSlotsList.Dequeue();
                    bool found = false;

                    for (int i = 0; i < Ingredients.Length; i++)
                    {
                        CraftingRecipeIngredient ingred = Ingredients[i].GetMatch(inputSlot.Itemstack);

                        if (ingred != null && !alreadyFound.Contains(i))
                        {
                            matched.Add(new KeyValuePair<ItemSlot, CraftingRecipeIngredient>(inputSlot, ingred));
                            alreadyFound.Add(i);
                            found = true;
                            break;
                        }
                    }

                    if (!found) return null;
                }

                // We're missing ingredients
                if (matched.Count != Ingredients.Length)
                {
                    return null;
                }

                return matched;
            }


            internal int getOutputSize(List<KeyValuePair<ItemSlot, CraftingRecipeIngredient>> matched)
            {
                int outQuantityMul = -1;

                foreach (var val in matched)
                {
                    ItemSlot inputSlot = val.Key;
                    CraftingRecipeIngredient ingred = val.Value;
                    int posChange = inputSlot.StackSize / ingred.Quantity;

                    if (posChange < outQuantityMul || outQuantityMul == -1) outQuantityMul = posChange;
                }

                if (outQuantityMul == -1)
                {
                    return -1;
                }


                foreach (var val in matched)
                {
                    ItemSlot inputSlot = val.Key;
                    CraftingRecipeIngredient ingred = val.Value;


                    // Must have same or more than the total crafted amount
                    if (inputSlot.StackSize < ingred.Quantity * outQuantityMul) return -1;

                }

                outQuantityMul = 1;
                return Output.StackSize * outQuantityMul;
            }

            public string GetOutputName()
            {
                return Lang.Get("indappledgroves:Will make {0}", Output.ResolvedItemstack.GetName());
            }

            public bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
            {
                bool ok = true;

                for (int i = 0; i < Ingredients.Length; i++)
                {
                    ok &= Ingredients[i].Resolve(world, sourceForErrorLogging);
                }

                ok &= Output.Resolve(world, sourceForErrorLogging);

                ok &= ReturnStack.Resolve(world, sourceForErrorLogging);


                return ok;
            }

            public void ToBytes(BinaryWriter writer)
            {
                writer.Write(Code);
                writer.Write(BaseToolDmg);
                writer.Write(ToolMode);
                writer.Write(RequiredWorkstation);
                writer.Write(Animation);
                writer.Write(IngredientMaterial);
                writer.Write(IngredientResistance);
                writer.Write(Ingredients.Length);
                for (int i = 0; i < Ingredients.Length; i++)
                {
                    Ingredients[i].ToBytes(writer);
                }

                Output.ToBytes(writer);
                ReturnStack.ToBytes(writer);
            }

            public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
            {
                Code = reader.ReadString();
                BaseToolDmg = reader.ReadInt32();
                ToolMode = reader.ReadString();
                RequiredWorkstation = reader.ReadString();
                Animation = reader.ReadString();
                IngredientMaterial = reader.ReadInt32();
                IngredientResistance = reader.ReadDouble();
                Ingredients = new WorkStationIngredient[reader.ReadInt32()];

                for (int i = 0; i < Ingredients.Length; i++)
                {
                    Ingredients[i] = new WorkStationIngredient();
                    Ingredients[i].FromBytes(reader, resolver);
                    Ingredients[i].Resolve(resolver, Code.ToString() + " (FromBytes)");
                }

                Output = new JsonItemStack();
                Output.FromBytes(reader, resolver.ClassRegistry);
                Output.Resolve(resolver, Code.ToString() + " (FromBytes)");
                ReturnStack = new JsonItemStack();
                ReturnStack.FromBytes(reader, resolver.ClassRegistry);
                ReturnStack.Resolve(resolver, Code.ToString() + " (FromBytes)");
            }

            public BasicWorkstationRecipe Clone()
            {
                WorkStationIngredient[] ingredients = new WorkStationIngredient[Ingredients.Length];
                for (int i = 0; i < Ingredients.Length; i++)
                {
                    ingredients[i] = Ingredients[i].Clone();
                }

                return new BasicWorkstationRecipe()
                {

                    Output = Output.Clone(),
                    ReturnStack = ReturnStack.Clone(),
                    Code = Code,
                    IngredientMaterial = IngredientMaterial,
                    IngredientResistance = IngredientResistance,
                    BaseToolDmg = BaseToolDmg,
                    ToolMode = ToolMode,
                    RequiredWorkstation = RequiredWorkstation,
                    Animation = Animation,
                    Enabled = Enabled,
                    Name = Name,
                    Ingredients = ingredients
                };
            }

            public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
            {
                Dictionary<string, string[]> mappings = new Dictionary<string, string[]>();

                if (Ingredients == null || Ingredients.Length == 0) return mappings;

                foreach (var ingreds in Ingredients)
                {
                    if (ingreds.Inputs.Length <= 0) continue;
                    CraftingRecipeIngredient ingred = ingreds.Inputs[0];
                    if (ingred == null || !ingred.Code.Path.Contains("*") || ingred.Name == null) continue;

                    int wildcardStartLen = ingred.Code.Path.IndexOf("*");
                    int wildcardEndLen = ingred.Code.Path.Length - wildcardStartLen - 1;

                    List<string> codes = new List<string>();

                    if (ingred.Type == EnumItemClass.Block)
                    {
                        for (int i = 0; i < world.Blocks.Count; i++)
                        {
                            if (world.Blocks[i].Code == null || world.Blocks[i].IsMissing) continue;

                            if (WildcardUtil.Match(ingred.Code, world.Blocks[i].Code))
                            {
                                string code = world.Blocks[i].Code.Path.Substring(wildcardStartLen);
                                string codepart = code.Substring(0, code.Length - wildcardEndLen);
                                if (ingred.AllowedVariants != null && !ingred.AllowedVariants.Contains(codepart)) continue;

                                codes.Add(codepart);

                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < world.Items.Count; i++)
                        {
                            if (world.Items[i].Code == null || world.Items[i].IsMissing) continue;

                            if (WildcardUtil.Match(ingred.Code, world.Items[i].Code))
                            {
                                string code = world.Items[i].Code.Path.Substring(wildcardStartLen);
                                string codepart = code.Substring(0, code.Length - wildcardEndLen);
                                if (ingred.AllowedVariants != null && !ingred.AllowedVariants.Contains(codepart)) continue;

                                codes.Add(codepart);
                            }
                        }
                    }

                    mappings[ingred.Name] = codes.ToArray();
                }

                return mappings;
            }
        }

        public class BasicWorkstationRecipe : WorkstationRecipe
        {

            public void ToBytes(BinaryWriter writer)
            {
                writer.Write(Code);
                writer.Write(BaseToolDmg);
                writer.Write(ToolMode);
                writer.Write(RequiredWorkstation);
                writer.Write(Animation);
                writer.Write(IngredientMaterial);
                writer.Write(IngredientResistance);
                writer.Write(Ingredients.Length);
                for (int i = 0; i < Ingredients.Length; i++)
                {
                    Ingredients[i].ToBytes(writer);
                }

                Output.ToBytes(writer);
                ReturnStack.ToBytes(writer);
            }

            public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
            {
                Code = reader.ReadString();
                BaseToolDmg = reader.ReadInt32();
                ToolMode = reader.ReadString();
                RequiredWorkstation = reader.ReadString();
                Animation = reader.ReadString();
                IngredientMaterial = reader.ReadInt32();
                IngredientResistance = reader.ReadDouble();
                Ingredients = new WorkStationIngredient[reader.ReadInt32()];

                for (int i = 0; i < Ingredients.Length; i++)
                {
                    Ingredients[i] = new WorkStationIngredient();
                    Ingredients[i].FromBytes(reader, resolver);
                    Ingredients[i].Resolve(resolver, Code.ToString() + " (FromBytes)");
                }

                Output = new JsonItemStack();
                Output.FromBytes(reader, resolver.ClassRegistry);
                Output.Resolve(resolver, Code.ToString() + " (FromBytes)");
                ReturnStack = new JsonItemStack();
                ReturnStack.FromBytes(reader, resolver.ClassRegistry);
                ReturnStack.Resolve(resolver, Code.ToString() + " (FromBytes)");
            }
            public BasicWorkstationRecipe Clone()
            { 
                WorkStationIngredient[] ingredients = new WorkStationIngredient[Ingredients.Length];
                for (int i = 0; i < Ingredients.Length; i++)
                {
                    ingredients[i] = Ingredients[i].Clone();
                }

                return new BasicWorkstationRecipe()
                {

                    Output = Output.Clone(),
                    ReturnStack = ReturnStack.Clone(),
                    Code = Code,
                    IngredientMaterial = IngredientMaterial,
                    IngredientResistance = IngredientResistance,
                    BaseToolDmg = BaseToolDmg,
                    ToolMode = ToolMode,
                    RequiredWorkstation = RequiredWorkstation,
                    Animation = Animation,
                    Enabled = Enabled,
                    Name = Name,
                    Ingredients = ingredients
                };
            }

            public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
            {
                Dictionary<string, string[]> mappings = new Dictionary<string, string[]>();

                if (Ingredients == null || Ingredients.Length == 0) return mappings;

                foreach (var ingreds in Ingredients)
                {
                    if (ingreds.Inputs.Length <= 0) continue;
                    CraftingRecipeIngredient ingred = ingreds.Inputs[0];
                    if (ingred == null || !ingred.Code.Path.Contains("*") || ingred.Name == null) continue;

                    int wildcardStartLen = ingred.Code.Path.IndexOf("*");
                    int wildcardEndLen = ingred.Code.Path.Length - wildcardStartLen - 1;

                    List<string> codes = new List<string>();

                    if (ingred.Type == EnumItemClass.Block)
                    {
                        for (int i = 0; i < world.Blocks.Count; i++)
                        {
                            if (world.Blocks[i].Code == null || world.Blocks[i].IsMissing) continue;

                            if (WildcardUtil.Match(ingred.Code, world.Blocks[i].Code))
                            {
                                string code = world.Blocks[i].Code.Path.Substring(wildcardStartLen);
                                string codepart = code.Substring(0, code.Length - wildcardEndLen);
                                if (ingred.AllowedVariants != null && !ingred.AllowedVariants.Contains(codepart)) continue;

                                codes.Add(codepart);

                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < world.Items.Count; i++)
                        {
                            if (world.Items[i].Code == null || world.Items[i].IsMissing) continue;

                            if (WildcardUtil.Match(ingred.Code, world.Items[i].Code))
                            {
                                string code = world.Items[i].Code.Path.Substring(wildcardStartLen);
                                string codepart = code.Substring(0, code.Length - wildcardEndLen);
                                if (ingred.AllowedVariants != null && !ingred.AllowedVariants.Contains(codepart)) continue;

                                codes.Add(codepart);
                            }
                        }
                    }

                    mappings[ingred.Name] = codes.ToArray();
                }

                return mappings;
            }
        }

        public class ComplexWorkstationRecipe : WorkstationRecipe
        {
            public new string Code = "SplitterRecipe";

            public override string ToolMode { get; set; } = "pounding";

            public override string RequiredWorkstation { get; set; } = "logsplitter";

            public string Animation = "axesplit-fp";

            public string ProcessModifier { get; set; } = "splitterblade-single"; //Can be single, cross, or any bladetype introduced later.

            public void ToBytes(BinaryWriter writer)
            {
                writer.Write(Code);
                writer.Write(BaseToolDmg);
                writer.Write(ToolMode);
                writer.Write(RequiredWorkstation);
                writer.Write(Animation);
                writer.Write(ProcessModifier);
                writer.Write(IngredientMaterial);
                writer.Write(IngredientResistance);
                writer.Write(Ingredients.Length);
                for (int i = 0; i < Ingredients.Length; i++)
                {
                    Ingredients[i].ToBytes(writer);
                }
                Output.ToBytes(writer);
                ReturnStack.ToBytes(writer);
            }

            public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
            {
                Code = reader.ReadString();
                BaseToolDmg = reader.ReadInt32();
                ToolMode = reader.ReadString();
                RequiredWorkstation = reader.ReadString();
                Animation = reader.ReadString();
                ProcessModifier = reader.ReadString();
                IngredientMaterial = reader.ReadInt32();
                IngredientResistance = reader.ReadDouble();
                Ingredients = new WorkStationIngredient[reader.ReadInt32()];

                for (int i = 0; i < Ingredients.Length; i++)
                {
                    Ingredients[i] = new WorkStationIngredient();
                    Ingredients[i].FromBytes(reader, resolver);
                    Ingredients[i].Resolve(resolver, RequiredWorkstation + " Recipe (FromBytes)");
                }

                Output = new JsonItemStack();
                Output.FromBytes(reader, resolver.ClassRegistry);
                Output.Resolve(resolver, RequiredWorkstation + " Recipe (FromBytes)");
                ReturnStack = new JsonItemStack();
                ReturnStack.FromBytes(reader, resolver.ClassRegistry);
                ReturnStack.Resolve(resolver, RequiredWorkstation + " Recipe (FromBytes)");
            }

            public bool Matches(IWorldAccessor worldForResolve, ItemSlot inputSlots)
            {
                int outputStackSize = 0;
                List<KeyValuePair<ItemSlot, CraftingRecipeIngredient>> matched = pairInput(inputSlots);
                if (matched == null) return false;
                outputStackSize = getOutputSize(matched);

                return outputStackSize >= 0;
            }

            public ComplexWorkstationRecipe Clone()
            {
                WorkStationIngredient[] ingredients = new WorkStationIngredient[Ingredients.Length];
                for (int i = 0; i < Ingredients.Length; i++)
                {
                    ingredients[i] = Ingredients[i].Clone();
                }

                return new ComplexWorkstationRecipe()
                {

                    Output = Output.Clone(),
                    ReturnStack = ReturnStack.Clone(),
                    Code = Code,
                    IngredientMaterial = IngredientMaterial,
                    IngredientResistance = IngredientResistance,
                    BaseToolDmg = BaseToolDmg,
                    ToolMode = ToolMode,
                    RequiredWorkstation = RequiredWorkstation,
                    Animation = Animation,
                    ProcessModifier = ProcessModifier,
                    Enabled = Enabled,
                    Name = Name,
                    Ingredients = ingredients
                };
            }
        }

        #region Very Complex Workstation Workspace
        //public class ProcessModifier : IByteSerializable
        //{
        //    public CraftingRecipeIngredient[] Inputs;

        //    public CraftingRecipeIngredient GetMatch(ItemStack stack)
        //    {
        //        if (stack == null) return null;

        //        for (int i = 0; i < Inputs.Length; i++)
        //        {
        //            if (Inputs[i].SatisfiesAsIngredient(stack)) return Inputs[i];
        //        }

        //        return null;
        //    }

        //    public bool Resolve(IWorldAccessor world, string debug)
        //    {
        //        bool ok = true;

        //        for (int i = 0; i < Inputs.Length; i++)
        //        {
        //            ok &= Inputs[i].Resolve(world, debug);
        //        }

        //        return ok;
        //    }

        //    public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        //    {
        //        Inputs = new CraftingRecipeIngredient[reader.ReadInt32()];

        //        for (int i = 0; i < Inputs.Length; i++)
        //        {
        //            Inputs[i] = new CraftingRecipeIngredient();
        //            Inputs[i].FromBytes(reader, resolver);
        //            Inputs[i].Resolve(resolver, "Workstation Ingredient (FromBytes)");
        //        }
        //    }

        //    public void ToBytes(BinaryWriter writer)
        //    {
        //        writer.Write(Inputs.Length);
        //        for (int i = 0; i < Inputs.Length; i++)
        //        {
        //            Inputs[i].ToBytes(writer);
        //        }
        //    }

        //    public WorkStationIngredient Clone()
        //    {
        //        CraftingRecipeIngredient[] newings = new CraftingRecipeIngredient[Inputs.Length];

        //        for (int i = 0; i < Inputs.Length; i++)
        //        {
        //            newings[i] = Inputs[i].Clone();
        //        }

        //        return new WorkStationIngredient()
        //        {
        //            Inputs = newings
        //        };
        //    }
        //}


        //public class VeryComplexWorkstationRecipe : WorkstationRecipe
        //{
        //    //A Place To Work Through Implementing An Array of Process Modifers
        //    public string Code = "SplitterRecipe";

        //    public string ToolMode { get; set; } = "pounding";

        //    public string RequiredWorkstation = "logsplitter";

        //    public string Animation = "axesplit-fp";

        //    //public ProcessModifier[] ProcessModifiers;

        //    public string BladeType { get; set; } = "single"; //Can be single, cross, or any bladetype introduced later.

        //    public void ToBytes(BinaryWriter writer)
        //    {
        //        writer.Write(Code);
        //        writer.Write(BaseToolDmg);
        //        writer.Write(ToolMode);
        //        writer.Write(RequiredWorkstation);
        //        writer.Write(Animation);
        //        writer.Write(BladeType);
        //        writer.Write(IngredientMaterial);
        //        writer.Write(IngredientResistance);
        //        writer.Write(Ingredients.Length);
        //        for (int i = 0; i < Ingredients.Length; i++)
        //        {
        //            Ingredients[i].ToBytes(writer);
        //        }
        //        //for (int i = 0; i < ProcessModifiers.Length; i++)
        //        //{
        //        //    ProcessModifiers[i].ToBytes(writer);
        //        //}

        //        Output.ToBytes(writer);
        //        ReturnStack.ToBytes(writer);
        //    }

        //    public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        //    {
        //        Code = reader.ReadString();
        //        BaseToolDmg = reader.ReadInt32();
        //        ToolMode = reader.ReadString();
        //        RequiredWorkstation = reader.ReadString();
        //        Animation = reader.ReadString();
        //        BladeType = reader.ReadString();
        //        IngredientMaterial = reader.ReadInt32();
        //        IngredientResistance = reader.ReadDouble();
        //        Ingredients = new WorkStationIngredient[reader.ReadInt32()];

        //        for (int i = 0; i < Ingredients.Length; i++)
        //        {
        //            Ingredients[i] = new WorkStationIngredient();
        //            Ingredients[i].FromBytes(reader, resolver);
        //            Ingredients[i].Resolve(resolver, RequiredWorkstation + " Recipe (FromBytes)");
        //        }

        //        for (int i = 0; i < ProcessModifiers.Length; i++)
        //        {
        //            ProcessModifiers[i] = new ProcessModifier();
        //            ProcessModifiers[i].FromBytes(reader, resolver);
        //            ProcessModifiers[i].Resolve(resolver, "RequiredWorkstation" + " ProcessModifier (FromBytes)");
        //        }

        //        Output = new JsonItemStack();
        //        Output.FromBytes(reader, resolver.ClassRegistry);
        //        Output.Resolve(resolver, "Log Splitter Recipe (FromBytes)");
        //        ReturnStack = new JsonItemStack();
        //        ReturnStack.FromBytes(reader, resolver.ClassRegistry);
        //        ReturnStack.Resolve(resolver, "Log Splitter Recipe (FromBytes)");
        //    }

        //    public VeryComplexWorkstationRecipe Clone()
        //    {
        //        WorkStationIngredient[] ingredients = new WorkStationIngredient[Ingredients.Length];
        //        for (int i = 0; i < Ingredients.Length; i++)
        //        {
        //            ingredients[i] = Ingredients[i].Clone();
        //        }

        //        return new VeryComplexWorkstationRecipe()
        //        {

        //            Output = Output.Clone(),
        //            ReturnStack = ReturnStack.Clone(),
        //            Code = Code,
        //            IngredientMaterial = IngredientMaterial,
        //            IngredientResistance = IngredientResistance,
        //            BaseToolDmg = BaseToolDmg,
        //            ToolMode = ToolMode,
        //            RequiredWorkstation = RequiredWorkstation,
        //            Animation = Animation,
        //            BladeType = BladeType,
        //            Enabled = Enabled,
        //            Name = Name,
        //            Ingredients = ingredients
        //        };
        //    }
        //}
        #endregion
        public class GroundRecipe : IByteSerializable
        {
            public string Code = "groundRecipe";
            public AssetLocation Name { get; set; }
            public bool Enabled { get; set; } = true;

            public string ToolMode = "chopping";
            public int BaseToolDmg { get; set; } = 1;

            public GroundIngredient[] Ingredients;

            public JsonItemStack Output;

            public JsonItemStack ReturnStack = new JsonItemStack() { Code = new AssetLocation("air"), Type = EnumItemClass.Block, Quantity = 1 };

            public ItemStack TryCraftNow(ICoreAPI api, ItemSlot inputslots)
            {

                var matched = pairInput(inputslots);

                ItemStack mixedStack = Output.ResolvedItemstack.Clone();
                mixedStack.StackSize = getOutputSize(matched);

                if (mixedStack.StackSize <= 0) return null;


                foreach (var val in matched)
                {
                    val.Key.TakeOut(val.Value.Quantity * (mixedStack.StackSize / Output.StackSize));
                    val.Key.MarkDirty();
                }

                return mixedStack;
            }

            public bool Matches(IWorldAccessor worldForResolve, ItemSlot inputSlots)
            {
                int outputStackSize = 0;

                List<KeyValuePair<ItemSlot, CraftingRecipeIngredient>> matched = pairInput(inputSlots);
                if (matched == null) return false;

                outputStackSize = getOutputSize(matched);

                return outputStackSize >= 0;
            }

            List<KeyValuePair<ItemSlot, CraftingRecipeIngredient>> pairInput(ItemSlot inputStacks)
            {
                List<int> alreadyFound = new List<int>();

                Queue<ItemSlot> inputSlotsList = new Queue<ItemSlot>();
                if (!inputStacks.Empty) inputSlotsList.Enqueue(inputStacks);

                if (inputSlotsList.Count != Ingredients.Length) return null;

                List<KeyValuePair<ItemSlot, CraftingRecipeIngredient>> matched = new List<KeyValuePair<ItemSlot, CraftingRecipeIngredient>>();

                while (inputSlotsList.Count > 0)
                {
                    ItemSlot inputSlot = inputSlotsList.Dequeue();
                    bool found = false;

                    for (int i = 0; i < Ingredients.Length; i++)
                    {
                        CraftingRecipeIngredient ingred = Ingredients[i].GetMatch(inputSlot.Itemstack);

                        if (ingred != null && !alreadyFound.Contains(i))
                        {
                            matched.Add(new KeyValuePair<ItemSlot, CraftingRecipeIngredient>(inputSlot, ingred));
                            alreadyFound.Add(i);
                            found = true;
                            break;
                        }
                    }

                    if (!found) return null;
                }

                // We're missing ingredients
                if (matched.Count != Ingredients.Length)
                {
                    return null;
                }

                return matched;
            }


            int getOutputSize(List<KeyValuePair<ItemSlot, CraftingRecipeIngredient>> matched)
            {
                int outQuantityMul = -1;

                foreach (var val in matched)
                {
                    ItemSlot inputSlot = val.Key;
                    CraftingRecipeIngredient ingred = val.Value;
                    int posChange = inputSlot.StackSize / ingred.Quantity;

                    if (posChange < outQuantityMul || outQuantityMul == -1) outQuantityMul = posChange;
                }

                if (outQuantityMul == -1)
                {
                    return -1;
                }


                foreach (var val in matched)
                {
                    ItemSlot inputSlot = val.Key;
                    CraftingRecipeIngredient ingred = val.Value;


                    // Must have same or more than the total crafted amount
                    if (inputSlot.StackSize < ingred.Quantity * outQuantityMul) return -1;

                }

                outQuantityMul = 1;
                return Output.StackSize * outQuantityMul;
            }

            public string GetOutputName()
            {
                return Lang.Get("indappledgroves:Will make {0}", Output.ResolvedItemstack.GetName());
            }

            public bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
            {
                bool ok = true;

                for (int i = 0; i < Ingredients.Length; i++)
                {
                    ok &= Ingredients[i].Resolve(world, sourceForErrorLogging);
                }

                ok &= Output.Resolve(world, sourceForErrorLogging);

                ok &= ReturnStack.Resolve(world, sourceForErrorLogging);


                return ok;
            }

            public void ToBytes(BinaryWriter writer)
            {
                writer.Write(Code);
                writer.Write(ToolMode);
                writer.Write(BaseToolDmg);
                writer.Write(Ingredients.Length);
                for (int i = 0; i < Ingredients.Length; i++)
                {
                    Ingredients[i].ToBytes(writer);
                }

                Output.ToBytes(writer);
                ReturnStack.ToBytes(writer);
            }

            public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
            {
                Code = reader.ReadString();
                ToolMode = reader.ReadString();
                BaseToolDmg = reader.ReadInt32();
                Ingredients = new GroundIngredient[reader.ReadInt32()];

                for (int i = 0; i < Ingredients.Length; i++)
                {
                    Ingredients[i] = new GroundIngredient();
                    Ingredients[i].FromBytes(reader, resolver);
                    Ingredients[i].Resolve(resolver, "Ground Recipe (FromBytes)");
                }

                Output = new JsonItemStack();
                Output.FromBytes(reader, resolver.ClassRegistry);
                Output.Resolve(resolver, "Ground Recipe (FromBytes)");
                ReturnStack = new JsonItemStack();
                ReturnStack.FromBytes(reader, resolver.ClassRegistry);
                ReturnStack.Resolve(resolver, "Ground Recipe Return Stack Not Resolved", true);
            }

            public GroundRecipe Clone()
            {
                GroundIngredient[] ingredients = new GroundIngredient[Ingredients.Length];
                for (int i = 0; i < Ingredients.Length; i++)
                {
                    ingredients[i] = Ingredients[i].Clone();
                }

                return new GroundRecipe()
                {
                    Enabled = Enabled,
                    Name = Name,
                    Code = Code,
                    ToolMode = ToolMode,
                    BaseToolDmg = BaseToolDmg,
                    Ingredients = ingredients,
                    Output = Output.Clone(),
                    ReturnStack = ReturnStack.Clone(),

                };
            }

            public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
            {
                Dictionary<string, string[]> mappings = new Dictionary<string, string[]>();

                if (Ingredients == null || Ingredients.Length == 0) return mappings;

                foreach (var ingreds in Ingredients)
                {
                    if (ingreds.Inputs.Length <= 0) continue;
                    CraftingRecipeIngredient ingred = ingreds.Inputs[0];
                    if (ingred == null || !ingred.Code.Path.Contains("*") || ingred.Name == null) continue;

                    int wildcardStartLen = ingred.Code.Path.IndexOf("*");
                    int wildcardEndLen = ingred.Code.Path.Length - wildcardStartLen - 1;

                    List<string> codes = new List<string>();

                    if (ingred.Type == EnumItemClass.Block)
                    {
                        for (int i = 0; i < world.Blocks.Count; i++)
                        {
                            if (world.Blocks[i].Code == null || world.Blocks[i].IsMissing) continue;

                            if (WildcardUtil.Match(ingred.Code, world.Blocks[i].Code))
                            {
                                string code = world.Blocks[i].Code.Path.Substring(wildcardStartLen);
                                string codepart = code.Substring(0, code.Length - wildcardEndLen);
                                if (ingred.AllowedVariants != null && !ingred.AllowedVariants.Contains(codepart)) continue;

                                codes.Add(codepart);

                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < world.Items.Count; i++)
                        {
                            if (world.Items[i].Code == null || world.Items[i].IsMissing) continue;

                            if (WildcardUtil.Match(ingred.Code, world.Items[i].Code))
                            {
                                string code = world.Items[i].Code.Path.Substring(wildcardStartLen);
                                string codepart = code.Substring(0, code.Length - wildcardEndLen);
                                if (ingred.AllowedVariants != null && !ingred.AllowedVariants.Contains(codepart)) continue;

                                codes.Add(codepart);
                            }
                        }
                    }

                    mappings[ingred.Name] = codes.ToArray();
                }

                return mappings;
            }
        }

    }
}