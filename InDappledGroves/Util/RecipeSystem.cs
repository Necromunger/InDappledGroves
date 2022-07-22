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
            private List<ChoppingBlockRecipe> choppingBlockRecipes = new List<ChoppingBlockRecipe>();
            private List<SawbuckRecipe> sawbuckRecipes = new List<SawbuckRecipe>();
            private List<SawHorseRecipe> sawhorseRecipes = new List<SawHorseRecipe>();
            private List<GroundRecipe> groundRecipes = new List<GroundRecipe>();

            public List<ChoppingBlockRecipe> ChoppingBlockRecipes
            {
                get
                {
                    return choppingBlockRecipes;
                }
                set
                {
                    choppingBlockRecipes = value;
                }
            }

            public List<SawbuckRecipe> SawbuckRecipes
            {
                get
                {
                    return sawbuckRecipes;
                }
                set
                {
                    sawbuckRecipes = value;
                }
            }

            public List<SawHorseRecipe> SawHorseRecipes
            {
                get
                {
                    return sawhorseRecipes;
                }
                set
                {
                    sawhorseRecipes = value;
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
                api.World.Logger.StoryEvent(Lang.Get("indappledgroves:The Tyee and the bullcook..."));
                LoadChoppingBlockRecipes();
                LoadSawbuckRecipes();
                LoadSawHorseRecipes();
                LoadGroundRecipes();
            }
            #region Chopping Recipes
            public void LoadChoppingBlockRecipes()
            {
                Dictionary<AssetLocation, JToken> files = api.Assets.GetMany<JToken>(api.Server.Logger, "recipes/choppingblock");
                int recipeQuantity = 0;
                int ignored = 0;

                foreach (var val in files)
                {
                    if (val.Value is JObject)
                    {
                        ChoppingBlockRecipe rec = val.Value.ToObject<ChoppingBlockRecipe>();
                        if (!rec.Enabled) continue;

                        LoadChoppingBlockRecipe(val.Key, rec, ref recipeQuantity, ref ignored);
                    }
                    if (val.Value is JArray)
                    {
                        foreach (var token in (val.Value as JArray))
                        {
                            ChoppingBlockRecipe rec = token.ToObject<ChoppingBlockRecipe>();
                            if (!rec.Enabled) continue;

                            LoadChoppingBlockRecipe(val.Key, rec, ref recipeQuantity, ref ignored);
                        }
                    }
                }

                api.World.Logger.Event("{0} chopping block recipes loaded", recipeQuantity);
                api.World.Logger.StoryEvent(Lang.Get("indappledgroves:...with sturdy haft and bit"));
            }

            public void LoadChoppingBlockRecipe(AssetLocation path, ChoppingBlockRecipe recipe, ref int quantityRegistered, ref int quantityIgnored)
            {
                if (!recipe.Enabled) return;
                if (recipe.Name == null) recipe.Name = path;
                string className = "chopping block recipe";


                Dictionary<string, string[]> nameToCodeMapping = recipe.GetNameToCodeMapping(api.World);

                if (nameToCodeMapping.Count > 0)
                {
                    List<ChoppingBlockRecipe> subRecipes = new List<ChoppingBlockRecipe>();

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
                            ChoppingBlockRecipe rec;

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

                    foreach (ChoppingBlockRecipe subRecipe in subRecipes)
                    {
                        if (!subRecipe.Resolve(api.World, className + " " + path))
                        {
                            quantityIgnored++;
                            continue;
                        }
                        IDGRecipeRegistry.Loaded.ChoppingBlockRecipes.Add(subRecipe);
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

                    IDGRecipeRegistry.Loaded.ChoppingBlockRecipes.Add(recipe);
                    quantityRegistered++;
                }
            }

            public class ChoppingBlockIngredient : IByteSerializable
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

                public void ToBytes(BinaryWriter writer)
                {
                    writer.Write(Inputs.Length);
                    for (int i = 0; i < Inputs.Length; i++)
                    {
                        Inputs[i].ToBytes(writer);
                    }
                }

                public ChoppingBlockIngredient Clone()
                {
                    CraftingRecipeIngredient[] newings = new CraftingRecipeIngredient[Inputs.Length];

                    for (int i = 0; i < Inputs.Length; i++)
                    {
                        newings[i] = Inputs[i].Clone();
                    }

                    return new ChoppingBlockIngredient()
                    {
                        Inputs = newings
                    };
                }
            }
            #endregion

            #region Sawing Recipes
            public void LoadSawbuckRecipes()
            {
                Dictionary<AssetLocation, JToken> files = api.Assets.GetMany<JToken>(api.Server.Logger, "recipes/sawbuck");
                int recipeQuantity = 0;
                int ignored = 0;

                foreach (var val in files)
                {
                    if (val.Value is JObject)
                    {
                        SawbuckRecipe rec = val.Value.ToObject<SawbuckRecipe>();
                        if (!rec.Enabled) continue;

                        LoadSawbuckRecipe(val.Key, rec, ref recipeQuantity, ref ignored);
                    }
                    if (val.Value is JArray)
                    {
                        foreach (var token in (val.Value as JArray))
                        {
                            SawbuckRecipe rec = token.ToObject<SawbuckRecipe>();
                            if (!rec.Enabled) continue;

                            LoadSawbuckRecipe(val.Key, rec, ref recipeQuantity, ref ignored);
                        }
                    }
                }

                api.World.Logger.Event("{0} sawbuck recipes loaded", recipeQuantity);
                api.World.Logger.StoryEvent(Lang.Get("indappledgroves:...by dust and the dog"));
            }

            public void LoadSawbuckRecipe(AssetLocation path, SawbuckRecipe recipe, ref int quantityRegistered, ref int quantityIgnored)
            {
                if (!recipe.Enabled) return;
                if (recipe.Name == null) recipe.Name = path;
                string className = "sawbuck recipe";

                Dictionary<string, string[]> nameToCodeMapping = recipe.GetNameToCodeMapping(api.World);

                if (nameToCodeMapping.Count > 0)
                {
                    List<SawbuckRecipe> subRecipes = new List<SawbuckRecipe>();

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
                            SawbuckRecipe rec;

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

                    foreach (SawbuckRecipe subRecipe in subRecipes)
                    {
                        if (!subRecipe.Resolve(api.World, className + " " + path))
                        {
                            quantityIgnored++;
                            continue;
                        }
                        IDGRecipeRegistry.Loaded.SawbuckRecipes.Add(subRecipe);
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

                    IDGRecipeRegistry.Loaded.SawbuckRecipes.Add(recipe);
                    quantityRegistered++;
                }
            }
            public class SawbuckIngredient : IByteSerializable
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

                public SawbuckIngredient Clone()
                {
                    CraftingRecipeIngredient[] newings = new CraftingRecipeIngredient[Inputs.Length];

                    for (int i = 0; i < Inputs.Length; i++)
                    {
                        newings[i] = Inputs[i].Clone();
                    }

                    return new SawbuckIngredient()
                    {
                        Inputs = newings
                    };
                }
            }
            #endregion

            #region Sawhorse Recipes
            public void LoadSawHorseRecipes()
            {
                Dictionary<AssetLocation, JToken> files = api.Assets.GetMany<JToken>(api.Server.Logger, "recipes/sawhorse");
                int recipeQuantity = 0;
                int ignored = 0;

                foreach (var val in files)
                {
                    if (val.Value is JObject)
                    {
                        SawHorseRecipe rec = val.Value.ToObject<SawHorseRecipe>();
                        if (!rec.Enabled) continue;

                        LoadSawHorseRecipe(val.Key, rec, ref recipeQuantity, ref ignored);
                    }
                    if (val.Value is JArray)
                    {
                        foreach (var token in (val.Value as JArray))
                        {
                            SawHorseRecipe rec = token.ToObject<SawHorseRecipe>();
                            if (!rec.Enabled) continue;

                            LoadSawHorseRecipe(val.Key, rec, ref recipeQuantity, ref ignored);
                        }
                    }
                }

                api.World.Logger.Event("{0} sawhorse recipes loaded", recipeQuantity);
                api.World.Logger.StoryEvent(Lang.Get("indappledgroves:...working sole and blade"));
            }

            public void LoadSawHorseRecipe(AssetLocation path, SawHorseRecipe recipe, ref int quantityRegistered, ref int quantityIgnored)
            {
                if (!recipe.Enabled) return;
                if (recipe.Name == null) recipe.Name = path;
                string className = "planing recipe";

                Dictionary<string, string[]> nameToCodeMapping = recipe.GetNameToCodeMapping(api.World);

                if (nameToCodeMapping.Count > 0)
                {
                    List<SawHorseRecipe> subRecipes = new List<SawHorseRecipe>();

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
                            SawHorseRecipe rec;

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

                    foreach (SawHorseRecipe subRecipe in subRecipes)
                    {
                        if (!subRecipe.Resolve(api.World, className + " " + path))
                        {
                            quantityIgnored++;
                            continue;
                        }
                        IDGRecipeRegistry.Loaded.SawHorseRecipes.Add(subRecipe);
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

                    IDGRecipeRegistry.Loaded.SawHorseRecipes.Add(recipe);
                    quantityRegistered++;
                }
            }
            public class SawHorseIngredient : IByteSerializable
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

                public SawHorseIngredient Clone()
                {
                    CraftingRecipeIngredient[] newings = new CraftingRecipeIngredient[Inputs.Length];

                    for (int i = 0; i < Inputs.Length; i++)
                    {
                        newings[i] = Inputs[i].Clone();
                    }

                    return new SawHorseIngredient()
                    {
                        Inputs = newings
                    };
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
                        foreach (var token in (val.Value as JArray))
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

        public class ChoppingBlockRecipe : IByteSerializable
        {
            public string Code = "ChoppingBlockRecipe";


            public AssetLocation Name { get; set; }
            public bool Enabled { get; set; } = true;
            public bool RequiresStation { get; set; } = false;

            public int ToolTime = 4;
            
            public int ToolDamage = 4;

            public string ToolMode { get; set; } = "chopping";

            public ChoppingBlockIngredient[] Ingredients;

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
                writer.Write(ToolTime);
                writer.Write(ToolDamage);
                writer.Write(ToolMode);
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
                RequiresStation = reader.ReadBoolean();
                ToolTime = reader.ReadInt32();
                ToolDamage = reader.ReadInt32();
                ToolMode = reader.ReadString();
                Ingredients = new ChoppingBlockIngredient[reader.ReadInt32()];

                for (int i = 0; i < Ingredients.Length; i++)
                {
                    Ingredients[i] = new ChoppingBlockIngredient();
                    Ingredients[i].FromBytes(reader, resolver);
                    Ingredients[i].Resolve(resolver, "Chopping Recipe (FromBytes)");
                }

                Output = new JsonItemStack();
                Output.FromBytes(reader, resolver.ClassRegistry);
                Output.Resolve(resolver, "Chopping Recipe (FromBytes)");
                ReturnStack = new JsonItemStack();
                ReturnStack.FromBytes(reader, resolver.ClassRegistry);
                ReturnStack.Resolve(resolver, "Chopping Recipe (FromBytes)");
            }

            public ChoppingBlockRecipe Clone()
            {
                ChoppingBlockIngredient[] ingredients = new ChoppingBlockIngredient[Ingredients.Length];
                for (int i = 0; i < Ingredients.Length; i++)
                {
                    ingredients[i] = Ingredients[i].Clone();
                }

                return new ChoppingBlockRecipe()
                {

                    Output = Output.Clone(),
                    ReturnStack = ReturnStack.Clone(),
                    Code = Code,
                    ToolTime = ToolTime,
                    ToolDamage = ToolDamage,
                    ToolMode = ToolMode,
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

        public class SawbuckRecipe : IByteSerializable
        {
            public string Code = "sawingRecipe";
            public AssetLocation Name { get; set; }
            public bool Enabled { get; set; } = true;
            public string ToolMode = "sawing";
            public int ToolTime = 4;
            public int ToolDamage = 4;
            public SawbuckIngredient[] Ingredients;

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
                writer.Write(ToolMode);
                writer.Write(ToolTime);
                writer.Write(ToolDamage);
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
                ToolTime = reader.ReadInt32();
                ToolDamage = reader.ReadInt32();
                Ingredients = new SawbuckIngredient[reader.ReadInt32()];

                for (int i = 0; i < Ingredients.Length; i++)
                {
                    Ingredients[i] = new SawbuckIngredient();
                    Ingredients[i].FromBytes(reader, resolver);
                    Ingredients[i].Resolve(resolver, "Sawing Recipe (FromBytes)");
                }

                Output = new JsonItemStack();
                Output.FromBytes(reader, resolver.ClassRegistry);
                Output.Resolve(resolver, "Sawing Recipe (FromBytes)");
                ReturnStack = new JsonItemStack();
                ReturnStack.FromBytes(reader, resolver.ClassRegistry);
                ReturnStack.Resolve(resolver, "Sawing Recipe (FromBytes)");
            }

            public SawbuckRecipe Clone()
            {
                SawbuckIngredient[] ingredients = new SawbuckIngredient[Ingredients.Length];
                for (int i = 0; i < Ingredients.Length; i++)
                {
                    ingredients[i] = Ingredients[i].Clone();
                }

                return new SawbuckRecipe()
                {
                    Output = Output.Clone(),
                    ReturnStack = ReturnStack.Clone(),
                    ToolMode = ToolMode,
                    ToolTime = ToolTime,
                    ToolDamage = ToolDamage,
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

        public class SawHorseRecipe : IByteSerializable
        {
            public string Code = "sawhorserecipe";
            public AssetLocation Name { get; set; }
            public bool Enabled { get; set; } = true;

            public string ToolMode = "planing";

            public int ToolTime = 4;

            public int ToolDamage = 4;

            public SawHorseIngredient[] Ingredients;

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
                writer.Write(ToolMode);
                writer.Write(ToolTime);
                writer.Write(ToolDamage);
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
                ToolTime = reader.ReadInt32();
                ToolDamage = reader.ReadInt32();
                Ingredients = new SawHorseIngredient[reader.ReadInt32()];

                for (int i = 0; i < Ingredients.Length; i++)
                {
                    Ingredients[i] = new SawHorseIngredient();
                    Ingredients[i].FromBytes(reader, resolver);
                    Ingredients[i].Resolve(resolver, "Planing Recipe (FromBytes)");
                }

                Output = new JsonItemStack();
                Output.FromBytes(reader, resolver.ClassRegistry);
                Output.Resolve(resolver, "Sawhorse Recipe (FromBytes)");
                ReturnStack = new JsonItemStack();
                ReturnStack.FromBytes(reader, resolver.ClassRegistry);
                ReturnStack.Resolve(resolver, "Sawhorse Recipe (FromBytes)");
            }

            public SawHorseRecipe Clone()
            {
                SawHorseIngredient[] ingredients = new SawHorseIngredient[Ingredients.Length];
                for (int i = 0; i < Ingredients.Length; i++)
                {
                    ingredients[i] = Ingredients[i].Clone();
                }

                return new SawHorseRecipe()
                {
                    Output = Output.Clone(),
                    ReturnStack = ReturnStack.Clone(),
                    Code = Code,
                    Enabled = Enabled,
                    Name = Name,
                    ToolMode = ToolMode,
                    ToolTime = ToolTime,
                    ToolDamage = ToolDamage,
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

        public class GroundRecipe : IByteSerializable
        {
            public string Code = "groundRecipe";
            public AssetLocation Name { get; set; }
            public bool Enabled { get; set; } = true;

            public string ToolMode = "chopping";

            public int ToolTime = 4;

            public int ToolDamage = 4;

            public GroundIngredient[] Ingredients;

            public JsonItemStack Output;

            public JsonItemStack ReturnStack = new JsonItemStack() { Code = new AssetLocation("air"), Type = EnumItemClass.Block, Quantity = 1};

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
                writer.Write(ToolMode);
                writer.Write(ToolTime);
                writer.Write(ToolDamage);
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
                ToolTime = reader.ReadInt32();
                ToolDamage = reader.ReadInt32();
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
                    Output = Output.Clone(),
                    ReturnStack = ReturnStack.Clone(),
                    ToolMode = ToolMode,
                    ToolTime = ToolTime,
                    ToolDamage = ToolDamage,
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