using System.Collections.Generic;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using InDappledGroves.Blocks;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using InDappledGroves.Util.Config;
using System.Linq;
using InDappledGroves.BlockEntities;
using static OpenTK.Graphics.OpenGL.GL;

namespace InDappledGroves.Util.WorldGen
{

    public class TreeHollows : ModSystem
    {
        private ICoreServerAPI sapi; //The main interface we will use for interacting with Vintage Story
        private ICoreClientAPI capi;
        private int chunkSize; //Size of chunks. Chunks are cubes so this is the size of the cube.
        private ISet<string> hollowTypes; //Stores tree types that will be used for detecting trees for placing our tree hollows
        private ISet<string> stumpTypes; //Stores tree types that will be used for detecting trees for placing our tree stumps
        private TreeLootObject[] treelootbase;
        private string[] dirs = { "north", "south", "east", "west" };
        private List<string> woods = new();
        private List<string> stumps = new();
        public static TreeGenComplete TreeDone = new();

        public override void Start(ICoreAPI api)
        {
            sapi = api as ICoreServerAPI;
            capi = api as ICoreClientAPI;
            base.Start(api);
        }

        public override double ExecuteOrder()
        {
            return 0.65;
        }

        //Our mod only needs to be loaded by the server
        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            //this.sapi = api;
            //chunkSize = worldBlockAccessor.ChunkSize;
            woods.AddRange(IDGTreeConfig.Current.woodTypes);
            stumps.AddRange(IDGTreeConfig.Current.stumpTypes);
            hollowTypes = new HashSet<string>();
            stumpTypes = new HashSet<string>();
            LoadTreeTypes(hollowTypes);
            LoadStumpTypes(stumpTypes);
            sapi.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, ClearTreeGen);
            TreeDone.OnTreeGenCompleteEvent += NewChunkStumpAndHollowGen;
        }

        private void ClearTreeGen()
        {
            TreeDone.OnTreeGenCompleteEvent -= NewChunkStumpAndHollowGen;
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            base.AssetsFinalize(api);
            treelootbase = CreateTreeLootList(IDGHollowLootConfig.Current.treehollowjson.ToArray());
        }

        private void LoadTreeTypes(ISet<string> treeTypes)
        {
            foreach (var variant in woods)
            {
                treeTypes.Add($"log-grown-" + variant + "-ud");
            }
        }

        private void LoadStumpTypes(ISet<string> stumpTypes)
        {
            foreach (var variant in stumps)
            {
                stumpTypes.Add($"log-grown-" + variant + "-ud");
            }
        }

        private void NewChunkStumpAndHollowGen(Dictionary<BlockPos, Block> treeBaseDict, IBlockAccessor ba, bool isWideTrunk)
        {
            if (treeBaseDict.Count != 0)
            {
                    foreach (KeyValuePair<BlockPos, Block> entry in treeBaseDict)
                    {

                        if (IsStumpLog(entry.Value))
                        {

                            PlaceTreeStump(ba, entry.Key);
                        }
                    }
            }
                float randNumb = (float)sapi.World.Rand.NextDouble();
                if (!sapi.ModLoader.IsModSystemEnabled("primitivesurvival") && randNumb <= IDGTreeConfig.Current.TreeHollowsSpawnProbability) PlaceTreeHollow(ba, treeBaseDict.Last().Key);
        }


        private bool IsStumpLog(Block block)
        {
            return stumpTypes.Contains(block.Code.Path);
        }

        // Delegate for /hollow command. Places a treehollow 2 blocks in front of the player
        private void PlaceTreeHollowInFrontOfPlayer(IServerPlayer player, int groupId, CmdArgs args)
        {
            PlaceTreeHollow(sapi.World.BlockAccessor, player.Entity.Pos.HorizontalAheadCopy(2).AsBlockPos);
        }

        // Places a tree stump at the given world coordinates using the given IBlockAccessor
        private bool PlaceTreeStump(IBlockAccessor blockAccessor, BlockPos pos)
        {
            
            var treeBlock = blockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            var stumpType = "pine";
            sapi.Logger.Debug(treeBlock.FirstCodePart());
            if (treeBlock.FirstCodePart() == "treestump" || blockAccessor.GetBlock(pos.DownCopy(), 0).FirstCodePart() == "treestump" || blockAccessor.GetBlock(pos.DownCopy(), 0).FirstCodePart() == "log") return false;

            //Abortive attempt at fixing the Kapok Issue
            //if (treeBlock.FirstCodePart() == "log" && treeBlock.FirstCodePart(2) == "kapok")
            //{
            //    return (OldKapokStumps(blockAccessor, pos, sapi.World));
            //}

            if (treeBlock.FirstCodePart() == "log" && blockAccessor.GetBlock(pos.DownCopy(), 0).FirstCodePart() != "treestump")
            {   
                stumpType = treeBlock.FirstCodePart(2);
            }

            var withPath = (treeBlock.Code.Domain == "game" ? "indappledgroves" : treeBlock.Code.Domain) + ":treestump-grown-" + stumpType + "-" + dirs[sapi.World.Rand.Next(4)];
            var withBlockID = sapi.WorldManager.GetBlockId(new AssetLocation(withPath));
            var withBlock = blockAccessor.GetBlock(withBlockID);
            blockAccessor.SetBlock(0, pos);
            if (withBlock.TryPlaceBlockForWorldGen(blockAccessor, pos, BlockFacing.UP, null))
            {
                return true;
            }
            return false;
        }

        //Abortive attempt at fixing the Kapok issue.  Does nothing different than the original code that only placed two out of four stumps on a large Kapok.
        //private bool OldKapokStumps(IBlockAccessor blockAccessor, BlockPos pos, IWorldAccessor world)
        //{
        //    BlockPos secondPos = null;
        //    sapi.World.BlockAccessor.WalkBlocks(pos.AddCopy(1, 0, 1), pos.AddCopy(-1, 0, -1), (block, x, y, z) =>
        //    {
        //    var treeBlock = blockAccessor.GetBlock(pos, BlockLayersAccess.Default);
        //    var stumpType = "kapok";
        //    if (treeBlock.FirstCodePart() == "log" && blockAccessor.GetBlock(pos.DownCopy(), 0).FirstCodePart() != "treestump" && blockAccessor.GetBlock(pos.DownCopy(), 0).FirstCodePart() != "log")
        //    {
        //        stumpType = treeBlock.FirstCodePart(2);
        //    }
        //    var withPath = (treeBlock.Code.Domain == "game" ? "indappledgroves" : treeBlock.Code.Domain) + ":treestump-grown-" + stumpType + "-" + dirs[world.Rand.Next(4)];
        //    var withBlockID = sapi.WorldManager.GetBlockId(new AssetLocation(withPath));
        //    var withBlock = blockAccessor.GetBlock(withBlockID);
        //    blockAccessor.SetBlock(0, pos);
        //    withBlock.TryPlaceBlockForWorldGen(blockAccessor, pos, BlockFacing.UP, null);
        //    }, true);
        //    return false;
        //}

        // Places a tree hollow filled with random items at the given world coordinates using the given IBlockAccessor
        private BlockPos PlaceTreeHollow(IBlockAccessor blockAccessor, BlockPos pos)
        {

            //consider moving it upwards
            var upCount = sapi.World.Rand.Next(2, 8);
            var upCandidateBlock = blockAccessor.GetBlock(pos.UpCopy(upCount), BlockLayersAccess.Default);

            if (upCandidateBlock.FirstCodePart() == "log")
            { pos = pos.UpCopy(upCount); }

            var treeBlock = blockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            var woodType = "pine";

            if (treeBlock.FirstCodePart() == "log")
            {
                woodType = treeBlock.FirstCodePart(2);
            }

            var hollowType = "up";
            if (sapi.World.Rand.Next(2) == 1)
            { hollowType = "up2"; }
            var belowBlock = blockAccessor.GetBlock(pos.DownCopy(), BlockLayersAccess.Default);
            if (belowBlock.Fertility > 0) //fertile ground below?
            {
                if (sapi.World.Rand.Next(2) == 1)
                { hollowType = "base"; }
                else
                { hollowType = "base2"; }
            }

            
            var withPath = (treeBlock.Code.Domain == "game" ? "indappledgroves" : treeBlock.Code.Domain) + ":treehollowgrown-" + hollowType + "-" + woodType + "-" + dirs[sapi.World.Rand.Next(4)];
            var withBlockID = sapi.WorldManager.GetBlockId(new AssetLocation(withPath));
            var withBlock = blockAccessor.GetBlock(withBlockID);
            if (withBlock.TryPlaceBlockForWorldGen(blockAccessor, pos, BlockFacing.UP, null))
            {
                var block = blockAccessor.GetBlock(pos, BlockLayersAccess.Default) as BlockTreeHollowGrown;
                if (block?.EntityClass != null)
                {
                    if (block.EntityClass == withBlock.EntityClass)
                    {
                        var be = blockAccessor.GetBlockEntity(pos) as BETreeHollowGrown;
                        if (be is BETreeHollowGrown)
                        {
                            ItemStack[] lootStacks = GetTreeLoot(treelootbase, pos);
                            if (lootStacks != null) AddItemStacks(be, lootStacks);
                            be.MarkDirty(true);
                        }
                    }
                }
                return pos;
            }
            return null;
        }

        //Adds the given list of ItemStacks to the first slots in the given hollow.
        public void AddItemStacks(IBlockEntityContainer hollow, ItemStack[] itemStacks)
        {
            var slotNumber = 0;
            var lootNumber = sapi.World.Rand.Next(1, hollow.Inventory.Count-1);
            if (itemStacks != null)
            {
                while (slotNumber < hollow.Inventory.Count-1 && slotNumber < lootNumber)
                {
                    var slot = hollow.Inventory[slotNumber];
                    slot.Itemstack = itemStacks[slotNumber];
                    hollow.Inventory.MarkSlotDirty(slotNumber);
                    slotNumber++;
                }
            }
        }

        private ItemStack[] GetTreeLoot(TreeLootObject[] treeLoot, BlockPos pos)
        {
            List<ItemStack> lootList = null;
            if (sapi != null)
            {
                ClimateCondition climate = sapi.World.BlockAccessor.GetClimateAt(pos);

                foreach (TreeLootObject lootItem in treeLoot)
                {
                    if (lootList == null && ClimateLootFilter(lootItem, pos))
                    {
                        lootItem.bstack.Resolve(sapi.World, "treedrop: ", lootItem.bstack.Code);
                        lootList = new();
                        lootList.Add(lootItem.bstack.GetNextItemStack());
                        continue;
                    }
                    if (lootList != null && ClimateLootFilter(lootItem, pos) && lootList.Count >= 0 && lootList.Count <= 8)
                    {
                        lootItem.bstack.Resolve(sapi.World, "treedrop: ", lootItem.bstack.Code);

                        lootList.Add(lootItem.bstack.GetNextItemStack());
                    }
                }
            }

            return lootList == null ? null : lootList.ToArray();
        }

        private TreeLootObject[] CreateTreeLootList(JsonObject[] treeLoot)
        {
            List<TreeLootObject> treelootlist = new();
            foreach (JsonObject lootStack in treeLoot)
            {
                TreeLootObject obj = new TreeLootObject(lootStack);
                if (obj.bstack.Resolve(sapi.World, "treedrop: ", obj.bstack.Code))
                {
                    treelootlist.Add(obj);
                }
            }
            return treelootlist.Count > 0 ? treelootlist.ToArray() : null;
        }

        private bool ClimateLootFilter(TreeLootObject obj, BlockPos pos)
        {
            ClimateCondition local = sapi.World.BlockAccessor.GetClimateAt(pos);

            return local.ForestDensity >= obj.cReqs.minForest
            && local.ForestDensity <= obj.cReqs.maxForest
            && local.ShrubDensity >= obj.cReqs.minShrub
            && local.ShrubDensity <= obj.cReqs.maxShrub
            && local.Rainfall >= obj.cReqs.minRain
            && local.Rainfall <= obj.cReqs.maxRain
            && local.Temperature >= obj.cReqs.minTemperature
            && local.Temperature <= obj.cReqs.maxTemperature
            && ((int)sapi.World.Calendar.GetSeason(pos) == obj.cReqs.season ||
                obj.cReqs.season == 4);
        }

        public static List<string> treehollowloot { get; set; } = new()
        {
            @"{""dropStack"": {""type"":""block"", ""code"": ""game:mushroom-fieldmushroom-normal"", ""quantity"": { ""avg"": 0.5, ""var"": 2, ""dist"": ""strongerinvexp""} }, ""dropReqs"": {}}",
            @"{""dropStack"": {""type"":""item"", ""code"": ""game:fruit-yellowapple"", ""quantity"": {""avg"": 0.5, ""var"": 2, ""dist"": ""strongerinvexp""}}, ""dropReqs"": {}}",
            @"{""dropStack"": {""type"":""item"", ""code"": ""game:fruit-redapple"", ""quantity"": {""avg"": 0.5, ""var"": 2, ""dist"": ""strongerinvexp""}}, ""dropReqs"": {}}",
            @"{""dropStack"": {""type"":""item"", ""code"": ""game:drygrass"", ""quantity"": {""avg"": 0.5, ""var"": 2, ""dist"": ""strongerinvexp""}}, ""dropReqs"": {}}",
            @"{""dropStack"": {""type"":""item"", ""code"": ""game:fruit-cherry"", ""quantity"": {""avg"": 0.5, ""var"": 2, ""dist"": ""strongerinvexp""}}, ""dropReqs"": {}}",
            @"{""dropStack"": {""type"":""item"", ""code"": ""game:insect-grub"", ""quantity"": {""avg"": 0.5, ""var"": 2, ""dist"": ""strongerinvexp""}}, ""dropReqs"": {}}",
            @"{""dropStack"": {""type"":""item"", ""code"": ""game:insect-termite"", ""quantity"": {""avg"": 0.5, ""var"": 2, ""dist"": ""strongerinvexp""}}, ""dropReqs"": {}}",
            @"{""dropStack"": {""type"":""item"", ""code"": ""game:cattailroot"", ""quantity"": {""avg"": 0.5, ""var"": 2, ""dist"": ""strongerinvexp""}}, ""dropReqs"": {}}",
            @"{""dropStack"": {""type"":""item"", ""code"": ""game:cattailtops"", ""quantity"": {""avg"": 0.5, ""var"": 2, ""dist"": ""strongerinvexp""}}, ""dropReqs"": {}}",
            @"{""dropStack"": {""type"":""item"", ""code"": ""game:honeycomb"", ""quantity"": {""avg"": 0.5, ""var"": 2, ""dist"": ""strongerinvexp""}}, ""dropReqs"": {}}",
            @"{""dropStack"": {""type"":""item"", ""code"": ""game:rot"", ""quantity"": {""avg"": 0.5, ""var"": 2, ""dist"": ""strongerinvexp""}}, ""dropReqs"": {}}",
            @"{""dropStack"": {""type"":""item"", ""code"": ""game:stick"", ""quantity"": {""avg"": 0.5, ""var"": 2, ""dist"": ""strongerinvexp""}}, ""dropReqs"": {}}",
            @"{""dropStack"": {""type"":""item"", ""code"": ""game:stone-limestone"", ""quantity"": {""avg"": 0.5, ""var"": 2, ""dist"": ""strongerinvexp""}}, ""dropReqs"": {}}",
            @"{""dropStack"": {""type"":""item"", ""code"": ""game:arrow-flint"", ""quantity"": {""avg"": 0.5, ""var"": 2, ""dist"": ""strongerinvexp""}}, ""dropReqs"": {}}",
            @"{""dropStack"": {""type"":""item"", ""code"": ""game:gear-rusty"", ""quantity"": {""avg"": 0.5, ""var"": 1, ""dist"": ""strongerinvexp""}}, ""dropReqs"": {}}",
            @"{""dropStack"": {""type"":""block"", ""code"": ""game:mushroom-fieldmushroom-normal"", ""quantity"": {""avg"": 0.5, ""var"": 2, ""dist"": ""strongerinvexp""}}, ""dropReqs"": {}}",
            @"{""dropStack"": {""type"":""block"", ""code"": ""game:mushroom-commonmorel-normal"", ""quantity"": {""avg"": 0.5, ""var"": 2, ""dist"": ""strongerinvexp""}}, ""dropReqs"": {}}",
            @"{""dropStack"": {""type"":""block"", ""code"": ""game:mushroom-almondmushroom-normal"", ""quantity"": {""avg"": 0.5, ""var"": 2, ""dist"": ""strongerinvexp""}}, ""dropReqs"": {}}",
            @"{""dropStack"": {""type"":""block"", ""code"": ""game:mushroom-orangeoakbolete-normal"", ""quantity"": {""avg"": 0.5, ""var"": 2, ""dist"": ""strongerinvexp""}}, ""dropReqs"": {}}",
            @"{""dropStack"": {""type"":""block"", ""code"": ""game:mushroom-flyagaric-harvested"", ""quantity"": {""avg"": 0.5, ""var"": 2, ""dist"": ""strongerinvexp""}}, ""dropReqs"": {}}"
        };

    }
    internal class TreeLootObject
    {
        public BlockDropItemStack bstack { get; }
        public ClimateRequirements cReqs { get; }

        public TreeLootObject() { }
        public TreeLootObject(JsonObject treeLoot)
        {
            bstack = treeLoot["dropStack"].AsObject<BlockDropItemStack>();
            cReqs = treeLoot["dropReqs"].AsObject<ClimateRequirements>();
        }
    }

    internal class ClimateRequirements
    {
        public float minForest { get; set; } = 0.0f;
        public float maxForest { get; set; } = 1f;
        public float minRain { get; set; } = 0.0f;

        public float maxRain { get; set; } = 1f;
        public float minShrub { get; set; } = 0.0f;
        public float maxShrub { get; set; } = 1f;
        public float minTemperature { get; set; } = -100.0f;
        public float maxTemperature { get; set; } = 200f;
        public int season { get; set; } = 4;
        public ClimateRequirements() { }

    }

    public delegate void TreeGenCompleteDelegate(Dictionary<BlockPos, Block> treeBaseDict, IWorldGenBlockAccessor ba, bool isWideTrunk);
    //subscriber class

    public class TreeGenComplete
    {

        public event TreeGenCompleteDelegate OnTreeGenCompleteEvent;

        public void OnTreeGenComplete(Dictionary<BlockPos, Block> treeBaseDict, IWorldGenBlockAccessor ba, bool isWideTrunk)
        {
            OnTreeGenCompleteEvent?.Invoke(treeBaseDict, ba, isWideTrunk);
        }

    }
}