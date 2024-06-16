using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;

namespace InDappledGroves.Interfaces
{
    interface IIDGTool 
    {
        public string GetToolModeName(ItemStack stack);

        public float GetToolModeMod(ItemStack stack);
        
    }
}
