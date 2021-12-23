using System;
using Vintagestory.API.Common;
using VinterTweaks.Items.Tools;

namespace InDappledGroves
{
    public class InDappledGroves : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("itemvtaxe", typeof(ItemIDGAxe));
            api.RegisterItemClass("itemvtsaw", typeof(ItemIDGSaw));
        }
    }
}
