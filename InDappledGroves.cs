using InDappledGroves.Items.Tools;
using System;
using Vintagestory.API.Common;

namespace InDappledGroves
{
    public class InDappledGroves : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("itemidgaxe", typeof(ItemIDGAxe));
            api.RegisterItemClass("itemidgsaw", typeof(ItemIDGSaw));
        }
    }
}
