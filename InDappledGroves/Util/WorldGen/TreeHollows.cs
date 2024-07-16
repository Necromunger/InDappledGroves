using System.Collections.Generic;
using VSJsonObject = Vintagestory.API.Datastructures.JsonObject;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using InDappledGroves.Blocks;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using InDappledGroves.Util.Config;
using System.Linq;
using InDappledGroves.BlockEntities;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using SystemJsonObject = System.Text.Json.Nodes.JsonObject;

namespace InDappledGroves.Util.WorldGen
{
    public class TreeHollows : ModSystem
    {
        private ICoreServerAPI sapi; //The main interface we will use for interacting with Vintage Story
        private ICoreClientAPI capi;
        private int chunkSize; //Size of chunks. Chunks are cubes so this is the size of the cube.
        private ISet<string> hollowTypes; //Stores tree types that will be used for detecting trees for placing our tree hollows
        private ISet<string> stumpTypes; //Stores tree types that will be used for detecting trees for placing our tree stumps
        public TreeLootObject[] treelootbase;
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
            TreeDone.OnTreeGenCompleteEvent += NewChunkStumpAndHollowGen;
        }

        private void ClearTreeGen()
        {
            TreeDone.OnTreeGenCompleteEvent -= NewChunkStumpAndHollowGen;
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            base.AssetsFinalize(api);
            api.Logger.Debug("AssetFinalize Tree Hollows has run");
            treelootbase = CreateTreeLootList(IDGHollowLootConfig.Current.treehollowjson.ToArray());
        }

        private void LoadTreeTypes(ISet<string> treeTypes)
        {
            foreach (var variant in woods)
            {
                treeTypes.Add($"log-grown-" + variant + "-ud");
                treeTypes.Add($"logsection-grown-" + variant + "-ne-ud");
                treeTypes.Add($"logsection-grown-" + variant + "-nw-ud");
                treeTypes.Add($"logsection-grown-" + variant + "-sw-ud");
                treeTypes.Add($"logsection-grown-" + variant + "-se-ud");
            }
        }

        private void LoadStumpTypes(ISet<string> stumpTypes)
        {
            foreach (var variant in stumps)
            {
                stumpTypes.Add($"log-grown-" + variant + "-ud");
                stumpTypes.Add($"logsection-grown-" + variant + "-ne-ud");
                stumpTypes.Add($"logsection-grown-" + variant + "-nw-ud");
                stumpTypes.Add($"logsection-grown-" + variant + "-sw-ud");
                stumpTypes.Add($"logsection-grown-" + variant + "-se-ud");
            }
        }

        private void NewChunkStumpAndHollowGen(Dictionary<BlockPos, Block> treeBaseDict, IBlockAccessor ba, bool isWideTrunk)
        {
            if (treeBaseDict.Count != 0)
            {
                int hollowcount = 0;
                int burlcount = 0;
                foreach (KeyValuePair<BlockPos, Block> entry in treeBaseDict)
                {

                    if (IsStumpLog(entry.Value))
                    {
                        PlaceTreeStump(ba, entry);

                        if (hollowcount == 0)
                        {
                            float randNumb = (float)sapi.World.Rand.NextDouble();
                            bool flag = sapi.ModLoader.IsModEnabled("primitivesurvival");
                            if (!sapi.ModLoader.IsModEnabled("primitivesurvival") && IDGTreeConfig.Current.DisableIDGHollowsWithPrimitiveSurvivalInstalled){
                                if (randNumb <= IDGTreeConfig.Current.TreeHollowsSpawnProbability)
                                    PlaceTreeHollow(ba, entry.Key);
                                hollowcount++;
                            }
                            if (capi != null) { capi.SendChatMessage(entry.Key.ToString()); };
                        }
                        if (burlcount == 0)
                        {
                            float randNumb = (float)sapi.World.Rand.NextDouble();
                            if(randNumb <= 0.02)
                            {
                            PlaceTreeBurl(ba, entry.Key);
                            burlcount++;
                            }
                        }
                    }
                }
            }
        }

        private bool IsStumpLog(Block block)
        {
            return stumpTypes.Contains(block.Code.Path);
        }

        private void PlaceTreeBurl(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var treeBlock = blockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            var upCount = sapi.World.Rand.Next(2, 6);
            var upCandidateBlock = blockAccessor.GetBlock(pos.UpCopy(upCount), BlockLayersAccess.Default);
            string firstCodePartUpCandidate = upCandidateBlock.FirstCodePart();
            string treeBlockFirstCodePart = blockAccessor.GetBlock(pos, BlockLayersAccess.Default).FirstCodePart();

            if (firstCodePartUpCandidate == "log" || firstCodePartUpCandidate == "logsection")
            {
                pos = pos.UpCopy(upCount);
            }

            var woodType = "pine";
            List<string> directions = new();
            if (treeBlockFirstCodePart == "log" || treeBlockFirstCodePart == "treestump")
            {
                woodType = treeBlock.FirstCodePart(2);
                directions.Add(dirs[sapi.World.Rand.Next(4)]);
            }
            else if (treeBlockFirstCodePart == "logsection" || treeBlockFirstCodePart == "treestumpsection")
            {
                woodType = treeBlock.FirstCodePart(2);
                switch (treeBlock.FirstCodePart(3))
                {
                    case "nw":
                        directions.Add("south");
                        directions.Add("east");
                        break;
                    case "ne":
                        directions.Add("south");
                        directions.Add("west");
                        break;
                    case "sw":
                        directions.Add("north");
                        directions.Add("east");
                        break;
                    case "se":
                        directions.Add("north");
                        directions.Add("west");
                        break;
                }

            } else if (firstCodePartUpCandidate == "treehollow")
            {
                return;
            }
            var burltype = sapi.World.Rand.NextDouble()<=0.7?"burl"+sapi.World.Rand.Next(1,4): "fatburl" + sapi.World.Rand.Next(1, 2);

            var withPath = (treeBlock.Code.Domain == "game" ? "indappledgroves" : treeBlock.Code.Domain) + ":idgburl-" + woodType + "-" + "grown" + "-" + burltype + "-" + directions[sapi.World.Rand.Next(directions.Count)].ToString();
            var withBlockID = sapi.WorldManager.GetBlockId(new AssetLocation(withPath));
            var withBlock = blockAccessor.GetBlock(withBlockID);
            BlockPos thispos = pos;
            switch (withBlock.Variant["side"])
            {
                case "north": thispos = pos.SouthCopy(); break;
                case "south": thispos = pos.NorthCopy(); break;
                case "east": thispos = pos.WestCopy(); break;
                case "west": thispos = pos.EastCopy(); break;
                default: return;
            }
            withBlock.TryPlaceBlockForWorldGen(blockAccessor, thispos, BlockFacing.UP, null);
            
        }

        // Places a tree stump at the given world coordinates using the given IBlockAccessor
        private bool PlaceTreeStump(IBlockAccessor blockAccessor, KeyValuePair<BlockPos, Block> posPair)
        {

            var treeBlock = posPair.Value;
            BlockPos pos = posPair.Key;

            if (treeBlock.FirstCodePart() == "treestump"
                || treeBlock.FirstCodePart() == "treestumpsection"
                || blockAccessor.GetBlock(pos.DownCopy(), 0).FirstCodePart() == "treestump"
                || blockAccessor.GetBlock(pos.DownCopy(), 0).FirstCodePart() == "log"
                || blockAccessor.GetBlock(pos.DownCopy(), 0).FirstCodePart() == "logsection"
                || blockAccessor.GetBlock(pos.DownCopy(), 0).FirstCodePart() == "treestumpsection") return false;

            string stumpType = treeBlock.FirstCodePart(2);

            string withPath;
            if (treeBlock.FirstCodePart() == "logsection")
            {
                withPath = (treeBlock.Code.Domain == "game" ? "indappledgroves" : treeBlock.Code.Domain) + ":treestumpsection-grown-" + stumpType + "-" + treeBlock.Variant["segment"];
            }
            else
            {
                withPath = (treeBlock.Code.Domain == "game" ? "indappledgroves" : treeBlock.Code.Domain) + ":treestump-grown-" + stumpType + "-" + dirs[sapi.World.Rand.Next(4)];
            }
            var withBlockID = sapi.WorldManager.GetBlockId(new AssetLocation(withPath));
            var withBlock = blockAccessor.GetBlock(withBlockID);

            if (blockAccessor.GetBlock(pos.DownCopy()).BlockId == 0)
            {
                //TODO: Quality Test This
                return (withBlock.TryPlaceBlockForWorldGen(blockAccessor, pos.DownCopy(), BlockFacing.UP, null));

            }
            blockAccessor.SetBlock(0, pos);
            if (withBlock.TryPlaceBlockForWorldGen(blockAccessor, pos, BlockFacing.UP, null))
            {
                return true;
            }
            return false;
        }

      

        // Delegate for /hollow command. Places a treehollow 2 blocks in front of the player
        private void PlaceTreeHollowInFrontOfPlayer(IServerPlayer player, int groupId, CmdArgs args)
        {
            PlaceTreeHollow(sapi.World.BlockAccessor, player.Entity.Pos.HorizontalAheadCopy(2).AsBlockPos);
        }

        // Places a tree hollow filled with random items at the given world coordinates using the given IBlockAccessor
        private BlockPos PlaceTreeHollow(IBlockAccessor blockAccessor, BlockPos pos)
        {
            if(treelootbase == null)
            {
                treelootbase = CreateTreeLootList(IDGHollowLootConfig.Current.treehollowjson.ToArray());
            }
            //consider moving it upwards
            var treeBlock = blockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            var upCount = sapi.World.Rand.Next(3, 8);
            var upCandidateBlock = blockAccessor.GetBlock(pos.UpCopy(upCount), BlockLayersAccess.Default);
            string firstCodePartUpCandidate = upCandidateBlock.FirstCodePart();
            string treeBlockFistCodePart = blockAccessor.GetBlock(pos, BlockLayersAccess.Default).FirstCodePart();

            if (firstCodePartUpCandidate == "log" || firstCodePartUpCandidate == "logsection")
            {
                pos = pos.UpCopy(upCount);
            }
            //if(treeBlockFistCodePart == "air") return null;

            var woodType = "pine";
            List<string> directions = new();
            if (treeBlockFistCodePart == "log" || treeBlockFistCodePart == "treestump")
            {
                woodType = treeBlock.FirstCodePart(2);
                directions.Add(dirs[sapi.World.Rand.Next(4)]);
            }
            else if (treeBlockFistCodePart == "logsection" || treeBlockFistCodePart == "treestumpsection")
            {
                woodType = treeBlock.FirstCodePart(2);
                switch (treeBlock.FirstCodePart(3))
                {
                    case "nw":
                        directions.Add("south");
                        directions.Add("east");
                        break;
                    case "ne":
                        directions.Add("south");
                        directions.Add("west");
                        break;
                    case "sw":
                        directions.Add("north");
                        directions.Add("east");
                        break;
                    case "se":
                        directions.Add("north");
                        directions.Add("west");
                        break;
                }

            }

            var hollowType = "up";
            if (sapi.World.Rand.Next(2) == 1)
            { hollowType = "up2"; }
            var belowBlock = blockAccessor.GetBlock(pos.DownCopy(), BlockLayersAccess.Default);
            if (belowBlock.Fertility > 0) //fertile ground below?
            {
                return null; 
            }


            var withPath = (treeBlock.Code.Domain == "game" ? "indappledgroves" : treeBlock.Code.Domain) + ":treehollowgrown-" + hollowType + "-" + woodType + "-" + directions[sapi.World.Rand.Next(directions.Count)].ToString();
            var withBlockID = sapi.WorldManager.GetBlockId(new AssetLocation(withPath));
            var withBlock = blockAccessor.GetBlock(withBlockID);
            blockAccessor.SetBlock(0, pos);
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
                            ItemStack[] lootStacks = GetTreeLoot(woodType, treelootbase, pos);
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
            var lootNumber = sapi.World.Rand.Next(1, hollow.Inventory.Count - 1);
            if (itemStacks != null)
            {
                while (slotNumber < hollow.Inventory.Count - 1 && slotNumber < lootNumber)
                {
                    var slot = hollow.Inventory[slotNumber];
                    slot.Itemstack = itemStacks[slotNumber];
                    hollow.Inventory.MarkSlotDirty(slotNumber);
                    slotNumber++;
                }
            }
        }

        private ItemStack[] GetTreeLoot(string woodType, TreeLootObject[] treeLoot, BlockPos pos)
        {
            List<ItemStack> lootList = null;
            if (sapi != null)
            {
                ClimateCondition climate = sapi.World.BlockAccessor.GetClimateAt(pos);

                foreach (TreeLootObject lootItem in treeLoot)
                {
                    if (lootList == null && ClimateLootFilter(woodType, lootItem, pos))
                    {
                        lootItem.bstack.Resolve(sapi.World, "treedrop: ", lootItem.bstack.Code);
                        lootList = new();
                        lootList.Add(lootItem.bstack.GetNextItemStack());
                        continue;
                    }
                    if (lootList != null && ClimateLootFilter(woodType, lootItem, pos) && lootList.Count >= 0 && lootList.Count <= 8)
                    {
                        lootItem.bstack.Resolve(sapi.World, "treedrop: ", lootItem.bstack.Code);

                        lootList.Add(lootItem.bstack.GetNextItemStack());
                    }
                }
            }

            return lootList == null ? null : lootList.ToArray();
        }

        private TreeLootObject[] CreateTreeLootList(JToken[] treeLoot)
        {

            List<TreeLootObject> treelootlist = new();
            foreach (JToken lootStack in treeLoot)
            {
           
                TreeLootObject obj = new TreeLootObject(VSJsonObject.FromJson(lootStack.ToString()));
                obj.bstack = VSJsonObject.FromJson(lootStack["Token"]["dropStack"].ToString()).AsObject<BlockDropItemStack>();
                obj.cReqs = VSJsonObject.FromJson(lootStack["Token"]["dropReqs"].ToString()).AsObject<ClimateRequirements>();
                if (obj.bstack.Resolve(sapi.World, "treedrop: ", obj.bstack.Code))
                {
                    treelootlist.Add(obj);
                }
            }
            return treelootlist.Count > 0 ? treelootlist.ToArray() : null;
        }

        private bool ClimateLootFilter(string woodType, TreeLootObject obj, BlockPos pos)
        {
            ClimateCondition local = sapi.World.BlockAccessor.GetClimateAt(pos);
            bool meetsReqs = local.ForestDensity >= obj.cReqs.minForest
            && local.ForestDensity <= obj.cReqs.maxForest
            && local.ShrubDensity >= obj.cReqs.minShrub
            && local.ShrubDensity <= obj.cReqs.maxShrub
            && local.Rainfall >= obj.cReqs.minRain
            && local.Rainfall <= obj.cReqs.maxRain
            && local.Temperature >= obj.cReqs.minTemperature
            && local.Temperature <= obj.cReqs.maxTemperature
            && (obj.cReqs.season.Contains(((int)sapi.World.Calendar.GetSeason(pos))) || obj.cReqs.season[0] == 4)
            && (obj.cReqs.treeTypes[0] == "all" || obj.cReqs.treeTypes.Contains<string>(woodType));
            return meetsReqs;
        }

        public static List<string> treehollowloot { get; set; } = new()
        {
            @"{""dropStack"": {""type"":""block"", ""code"": ""game:mushroom-fieldmushroom-normal"", ""quantity"": { ""avg"": 0.5, ""var"": 2, ""dist"": ""strongerinvexp""} }, ""dropReqs"": {""season"": [4]}, ""treeTypes"": [""birch""]}",
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
    public class TreeLootObject
    {
        public BlockDropItemStack bstack { get; set; }
        public ClimateRequirements cReqs { get; set;  }

        public TreeLootObject() { }
        public TreeLootObject(VSJsonObject treeLoot)
        {
            bstack = treeLoot["dropStack"].AsObject<BlockDropItemStack>();
            cReqs = treeLoot["dropReqs"].AsObject<ClimateRequirements>();
        }
    }

    public class ClimateRequirements
    {
        public float minForest { get; set; } = 0.0f;
        public float maxForest { get; set; } = 1f;
        public float minRain { get; set; } = 0.0f;

        public float maxRain { get; set; } = 1f;
        public float minShrub { get; set; } = 0.0f;
        public float maxShrub { get; set; } = 1f;
        public float minTemperature { get; set; } = -100.0f;
        public float maxTemperature { get; set; } = 200f;
        public int[] season { get; set; } = { 4 };

        public string[] treeTypes { get; set; } = { "all" };

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