using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace TreeReplacer
{
    public class TreeReplacer : ModSystem
    {
        ICoreServerAPI sapi;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.sapi = api;

            api.Logger.Event("Tree Replacer - Starting on server");

            api.Event.ChunkColumnLoaded += Event_ChunkColumnLoaded;
            api.Event.BreakBlock += Event_BreakBlock;
        }

        private void Event_BreakBlock(IServerPlayer byPlayer, BlockSelection blockSel, ref float dropQuantityMultiplier, ref EnumHandling handling)
        {
            byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, $"You are at {byPlayer.Entity.Pos.AsBlockPos.X},{byPlayer.Entity.Pos.AsBlockPos.Y},{byPlayer.Entity.Pos.AsBlockPos.Z}", EnumChatType.Notification);
        }

        private void Event_ChunkColumnLoaded(Vintagestory.API.MathTools.Vec2i chunkCoord, IWorldChunk[] chunks)
        {
            int chY = -1;
            foreach (var chunk in chunks)
            {
                chY++;
                if (chunk.Empty) continue;

                IMapChunk mc = sapi.World.BlockAccessor.GetMapChunk(chunkCoord);
                if (mc == null) continue;   //this chunk isn't actually loaded, no need to examine it.

                sapi.Logger.VerboseDebug($"Event_ChunkColumnLoaded - ({chunkCoord.X},{chY},{chunkCoord.Y}) - {(chunk.Empty?"Empty chunk!":"")}");

                int chunkSize = sapi.WorldManager.ChunkSize;
                int idGold = sapi.World.GetBlock(new AssetLocation("game:metalblock-gold")).BlockId;

                //now check every block on the surface of that chunk for a tree
                for (int x = 0; x < chunkSize; x++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        
                        int y = mc.WorldGenTerrainHeightMap[z*chunkSize + x];
                        int offsetY = 0;
                        
                        while((y % chunkSize) + offsetY < chunkSize)
                        {

                            int blockId = chunk.UnpackAndReadBlock(MapUtil.Index3d(x, (y+offsetY) % chunkSize, z, chunkSize, chunkSize), BlockLayersAccess.FluidOrSolid);

                            if(blockId == 0)
                            {
                                break;
                            }
                            
                            Block block = sapi.World.Blocks[blockId];

                            if (block.BlockMaterial == EnumBlockMaterial.Wood)
                            {
                                sapi.World.BlockAccessor.SetBlock(idGold, new BlockPos(chunkSize * chunkCoord.X + x, (y+offsetY), chunkSize * chunkCoord.Y + z));
                                sapi.Logger.VerboseDebug($"Replaced a block at ({chunkSize * chunkCoord.X + x},{y+offsetY},{chunkSize * chunkCoord.Y + z})!");
                                
                                break;
                            }

                            offsetY++;
                        }
                    }
                }
            }
        }
    }
}
