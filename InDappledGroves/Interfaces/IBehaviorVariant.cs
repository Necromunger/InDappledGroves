using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;

namespace InDappledGroves.CollectibleBehaviors
{
    interface IBehaviorVariant
    {
        public SkillItem[] GetSkillItems();
    }
}
