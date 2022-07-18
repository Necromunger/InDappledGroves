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
using static InDappledGroves.Util.IDGRecipeNames.IDGRecipeLoader;

namespace InDappledGroves.Util
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
            private List<ChoppingRecipe> choppingRecipes = new List<ChoppingRecipe>();
            private List<SawingRecipe> sawingRecipes = new List<SawingRecipe>();
            private List<PlaningRecipe> planingRecipes = new List<PlaningRecipe>();

            public List<ChoppingRecipe> ChoppingRecipes
            {
                get
                {
                    return choppingRecipes;
                }
                set
                {
                    choppingRecipes = value;
                }
            }

            public List<SawingRecipe> SawingRecipes
            {
                get
                {
                    return sawingRecipes;
                }
                set
                {
                    sawingRecipes = value;
                }
            }

            public List<PlaningRecipe> PlaningRecipes
            {
                get
                {
                    return planingRecipes;
                }
                set
                {
                    planingRecipes = value;
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

            public override void StartServerSide(ICoreServerAPI api)
            {
                IDGRecipeRegistry.Create();
                this.api = api;
                api.Event.SaveGameLoaded += LoadIDGRecipes;
            }

            public override void Dispose()
            {
                base.Dispose();
                IDGRecipeRegistry.Dispose();
            }

            public void LoadIDGRecipes()
            {
                LoadChoppingRecipes();
                LoadSawingRecipes();
                LoadPlaningRecipes();
            }
            #region Chopping Recipes
            public void LoadChoppingRecipes()
            {
                Dictionary<AssetLocation, JToken> files = api.Assets.GetMany<JToken>(api.Server.Logger, "recipes/chopping");
                int recipeQuantity = 0;
                int ignored = 0;

                foreach (var val in files)
                {
                    if (val.Value is JObject)
                    {
                        ChoppingRecipe rec = val.Value.ToObject<ChoppingRecipe>();
                        if (!rec.Enabled) continue;

                        LoadChoppingRecipe(val.Key, rec, ref recipeQuantity, ref ignored);
                    }
                    if (val.Value is JArray)
                    {
                        foreach (var token in (val.Value as JArray))
                        {
                            ChoppingRecipe rec = token.ToObject<ChoppingRecipe>();
                            if (!rec.Enabled) continue;

                            LoadChoppingRecipe(val.Key, rec, ref recipeQuantity, ref ignored);
                        }
                    }
                }

                api.World.Logger.Event("{0} chopping recipes loaded", recipeQuantity);
                api.World.Logger.StoryEvent(Lang.Get("indappledgroves:The haft and the bit..."));
            }

            public void LoadChoppingRecipe(AssetLocation path, ChoppingRecipe recipe, ref int quantityRegistered, ref int quantityIgnored)
            {
                if (!recipe.Enabled) return;
                if (recipe.Name == null) recipe.Name = path;
                string className = "chopping recipe";


                Dictionary<string, string[]> nameToCodeMapping = recipe.GetNameToCodeMapping(api.World);

                if (nameToCodeMapping.Count > 0)
                {
                    List<ChoppingRecipe> subRecipes = new List<ChoppingRecipe>();

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
                            ChoppingRecipe rec;

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

                            rec.Output.FillPlaceHolder(val2.Key, variants[i % variants.Length]);
                        }

                        first = false;
                    }

                    if (subRecipes.Count == 0)
                    {
                        api.World.Logger.Warning("{1} file {0} make uses of wildcards, but no blocks or item matching those wildcards were found.", path, className);
                    }

                    foreach (ChoppingRecipe subRecipe in subRecipes)
                    {
                        if (!subRecipe.Resolve(api.World, className + " " + path))
                        {
                            quantityIgnored++;
                            continue;
                        }
                        IDGRecipeRegistry.Loaded.ChoppingRecipes.Add(subRecipe);
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

                    IDGRecipeRegistry.Loaded.ChoppingRecipes.Add(recipe);
                    quantityRegistered++;
                }
            }

            public class ChoppingIngredient : IByteSerializable
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
                        Inputs[i].Resolve(resolver, "Chopping Ingredient (FromBytes)");
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

                public ChoppingIngredient Clone()
                {
                    CraftingRecipeIngredient[] newings = new CraftingRecipeIngredient[Inputs.Length];

                    for (int i = 0; i < Inputs.Length; i++)
                    {
                        newings[i] = Inputs[i].Clone();
                    }

                    return new ChoppingIngredient()
                    {
                        Inputs = newings
                    };
                }
            }

            #endregion

            #region Sawing Recipes
            public void LoadSawingRecipes()
            {
                Dictionary<AssetLocation, JToken> files = api.Assets.GetMany<JToken>(api.Server.Logger, "recipes/sawing");
                int recipeQuantity = 0;
                int ignored = 0;

                foreach (var val in files)
                {
                    if (val.Value is JObject)
                    {
                        SawingRecipe rec = val.Value.ToObject<SawingRecipe>();
                        if (!rec.Enabled) continue;

                        LoadSawingRecipe(val.Key, rec, ref recipeQuantity, ref ignored);
                    }
                    if (val.Value is JArray)
                    {
                        foreach (var token in (val.Value as JArray))
                        {
                            SawingRecipe rec = token.ToObject<SawingRecipe>();
                            if (!rec.Enabled) continue;

                            LoadSawingRecipe(val.Key, rec, ref recipeQuantity, ref ignored);
                        }
                    }
                }

                api.World.Logger.Event("{0} sawing recipes loaded", recipeQuantity);
                api.World.Logger.StoryEvent(Lang.Get("indappledgroves:The dust and the dog..."));
            }

            public void LoadSawingRecipe(AssetLocation path, SawingRecipe recipe, ref int quantityRegistered, ref int quantityIgnored)
            {
                if (!recipe.Enabled) return;
                if (recipe.Name == null) recipe.Name = path;
                string className = "sawing recipe";

                Dictionary<string, string[]> nameToCodeMapping = recipe.GetNameToCodeMapping(api.World);

                if (nameToCodeMapping.Count > 0)
                {
                    List<SawingRecipe> subRecipes = new List<SawingRecipe>();

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
                            SawingRecipe rec;

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

                            rec.Output.FillPlaceHolder(val2.Key, variants[i % variants.Length]);
                        }

                        first = false;
                    }

                    if (subRecipes.Count == 0)
                    {
                        api.World.Logger.Warning("{1} file {0} make uses of wildcards, but no blocks or item matching those wildcards were found.", path, className);
                    }

                    foreach (SawingRecipe subRecipe in subRecipes)
                    {
                        if (!subRecipe.Resolve(api.World, className + " " + path))
                        {
                            quantityIgnored++;
                            continue;
                        }
                        IDGRecipeRegistry.Loaded.SawingRecipes.Add(subRecipe);
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

                    IDGRecipeRegistry.Loaded.SawingRecipes.Add(recipe);
                    quantityRegistered++;
                }
            }
            public class SawingIngredient : IByteSerializable
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
                        Inputs[i].Resolve(resolver, "Chopping Ingredient (FromBytes)");
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

                public SawingIngredient Clone()
                {
                    CraftingRecipeIngredient[] newings = new CraftingRecipeIngredient[Inputs.Length];

                    for (int i = 0; i < Inputs.Length; i++)
                    {
                        newings[i] = Inputs[i].Clone();
                    }

                    return new SawingIngredient()
                    {
                        Inputs = newings
                    };
                }
            }
            #endregion

            #region Planing Recipes
            public void LoadPlaningRecipes()
            {
                Dictionary<AssetLocation, JToken> files = api.Assets.GetMany<JToken>(api.Server.Logger, "recipes/planing");
                int recipeQuantity = 0;
                int ignored = 0;

                foreach (var val in files)
                {
                    if (val.Value is JObject)
                    {
                        PlaningRecipe rec = val.Value.ToObject<PlaningRecipe>();
                        if (!rec.Enabled) continue;

                        LoadPlaningRecipe(val.Key, rec, ref recipeQuantity, ref ignored);
                    }
                    if (val.Value is JArray)
                    {
                        foreach (var token in (val.Value as JArray))
                        {
                            PlaningRecipe rec = token.ToObject<PlaningRecipe>();
                            if (!rec.Enabled) continue;

                            LoadPlaningRecipe(val.Key, rec, ref recipeQuantity, ref ignored);
                        }
                    }
                }

                api.World.Logger.Event("{0} Planing recipes loaded", recipeQuantity);
                api.World.Logger.StoryEvent(Lang.Get("indappledgroves:The sole and the blade..."));
            }

            public void LoadPlaningRecipe(AssetLocation path, PlaningRecipe recipe, ref int quantityRegistered, ref int quantityIgnored)
            {
                if (!recipe.Enabled) return;
                if (recipe.Name == null) recipe.Name = path;
                string className = "Planing recipe";

                Dictionary<string, string[]> nameToCodeMapping = recipe.GetNameToCodeMapping(api.World);

                if (nameToCodeMapping.Count > 0)
                {
                    List<PlaningRecipe> subRecipes = new List<PlaningRecipe>();

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
                            PlaningRecipe rec;

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

                            rec.Output.FillPlaceHolder(val2.Key, variants[i % variants.Length]);
                        }

                        first = false;
                    }

                    if (subRecipes.Count == 0)
                    {
                        api.World.Logger.Warning("{1} file {0} make uses of wildcards, but no blocks or item matching those wildcards were found.", path, className);
                    }

                    foreach (PlaningRecipe subRecipe in subRecipes)
                    {
                        if (!subRecipe.Resolve(api.World, className + " " + path))
                        {
                            quantityIgnored++;
                            continue;
                        }
                        IDGRecipeRegistry.Loaded.PlaningRecipes.Add(subRecipe);
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

                    IDGRecipeRegistry.Loaded.PlaningRecipes.Add(recipe);
                    quantityRegistered++;
                }
            }
            public class PlaningIngredient : IByteSerializable
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
                        Inputs[i].Resolve(resolver, "Chopping Ingredient (FromBytes)");
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

                public PlaningIngredient Clone()
                {
                    CraftingRecipeIngredient[] newings = new CraftingRecipeIngredient[Inputs.Length];

                    for (int i = 0; i < Inputs.Length; i++)
                    {
                        newings[i] = Inputs[i].Clone();
                    }

                    return new PlaningIngredient()
                    {
                        Inputs = newings
                    };
                }
            }
            #endregion
        }

        public class ChoppingRecipe : IByteSerializable
        {
            public string Code = "choppingRecipe";


            public AssetLocation Name { get; set; }
            public bool Enabled { get; set; } = true;
            public bool RequiresStation { get; set; } = false;

            public ChoppingIngredient[] Ingredients;

            public JsonItemStack Output;

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


                return ok;
            }

            public void ToBytes(BinaryWriter writer)
            {
                writer.Write(Code);
                writer.Write(RequiresStation);
                writer.Write(Ingredients.Length);
                for (int i = 0; i < Ingredients.Length; i++)
                {
                    Ingredients[i].ToBytes(writer);
                }

                Output.ToBytes(writer);
            }

            public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
            {
                Code = reader.ReadString();
                RequiresStation = reader.ReadBoolean();
                Ingredients = new ChoppingIngredient[reader.ReadInt32()];

                for (int i = 0; i < Ingredients.Length; i++)
                {
                    Ingredients[i] = new ChoppingIngredient();
                    Ingredients[i].FromBytes(reader, resolver);
                    Ingredients[i].Resolve(resolver, "Chopping Recipe (FromBytes)");
                }

                Output = new JsonItemStack();
                Output.FromBytes(reader, resolver.ClassRegistry);
                Output.Resolve(resolver, "Chopping Recipe (FromBytes)");
            }

            public ChoppingRecipe Clone()
            {
                ChoppingIngredient[] ingredients = new ChoppingIngredient[Ingredients.Length];
                for (int i = 0; i < Ingredients.Length; i++)
                {
                    ingredients[i] = Ingredients[i].Clone();
                }

                return new ChoppingRecipe()
                {
                    RequiresStation = RequiresStation,
                    Output = Output.Clone(),
                    Code = Code,
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

        public class SawingRecipe : IByteSerializable
        {
            public string Code = "sawingRecipe";
            public AssetLocation Name { get; set; }
            public bool Enabled { get; set; } = true;
            public bool RequiresStation;

            public SawingIngredient[] Ingredients;

            public JsonItemStack Output;

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


                return ok;
            }

            public void ToBytes(BinaryWriter writer)
            {
                writer.Write(Code);
                writer.Write(RequiresStation);
                writer.Write(Ingredients.Length);
                for (int i = 0; i < Ingredients.Length; i++)
                {
                    Ingredients[i].ToBytes(writer);
                }

                Output.ToBytes(writer);
            }

            public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
            {
                Code = reader.ReadString();
                RequiresStation = reader.ReadBoolean();
                Ingredients = new SawingIngredient[reader.ReadInt32()];

                for (int i = 0; i < Ingredients.Length; i++)
                {
                    Ingredients[i] = new SawingIngredient();
                    Ingredients[i].FromBytes(reader, resolver);
                    Ingredients[i].Resolve(resolver, "Sawing Recipe (FromBytes)");
                }

                Output = new JsonItemStack();
                Output.FromBytes(reader, resolver.ClassRegistry);
                Output.Resolve(resolver, "Sawing Recipe (FromBytes)");
            }

            public SawingRecipe Clone()
            {
                SawingIngredient[] ingredients = new SawingIngredient[Ingredients.Length];
                for (int i = 0; i < Ingredients.Length; i++)
                {
                    ingredients[i] = Ingredients[i].Clone();
                }

                return new SawingRecipe()
                {
                    Output = Output.Clone(),
                    Code = Code,
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

        public class PlaningRecipe : IByteSerializable
        {
            public string Code = "planingRecipe";
            public AssetLocation Name { get; set; }
            public bool Enabled { get; set; } = true;

            public bool RequiresStation;

            public PlaningIngredient[] Ingredients;

            public JsonItemStack Output;

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


                return ok;
            }

            public void ToBytes(BinaryWriter writer)
            {
                writer.Write(Code);
                writer.Write(RequiresStation);
                writer.Write(Ingredients.Length);
                for (int i = 0; i < Ingredients.Length; i++)
                {
                    Ingredients[i].ToBytes(writer);
                }

                Output.ToBytes(writer);
            }

            public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
            {
                Code = reader.ReadString();
                RequiresStation = reader.ReadBoolean();
                Ingredients = new PlaningIngredient[reader.ReadInt32()];

                for (int i = 0; i < Ingredients.Length; i++)
                {
                    Ingredients[i] = new PlaningIngredient();
                    Ingredients[i].FromBytes(reader, resolver);
                    Ingredients[i].Resolve(resolver, "Planing Recipe (FromBytes)");
                }

                Output = new JsonItemStack();
                Output.FromBytes(reader, resolver.ClassRegistry);
                Output.Resolve(resolver, "Planing Recipe (FromBytes)");
            }

            public PlaningRecipe Clone()
            {
                PlaningIngredient[] ingredients = new PlaningIngredient[Ingredients.Length];
                for (int i = 0; i < Ingredients.Length; i++)
                {
                    ingredients[i] = Ingredients[i].Clone();
                }

                return new PlaningRecipe()
                {
                    Output = Output.Clone(),
                    Code = Code,
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
    }
}