using System;
using System.Collections.Generic;
using System.Text;

namespace InDappledGroves.Blocks.BlockUtils
{
    class SawBuckProperties
    {
		public SawbuckTypeProperties this[string type]
		{
			get
			{
				SawbuckTypeProperties props;
				if (!this.Properties.TryGetValue(type, out props))
				{
					return this.Properties["*"];
				}
				return props;
			}
		}

		// Token: 0x040004D1 RID: 1233
		public Dictionary<string, SawbuckTypeProperties> Properties;

		// Token: 0x040004D2 RID: 1234
		public string[] Types;

		// Token: 0x040004D4 RID: 1236
		public string DefaultType = "wood-aged";

		// Token: 0x040004D5 RID: 1237
		public string VariantByGroup;

		// Token: 0x040004D6 RID: 1238
		public string VariantByGroupInventory;

		// Token: 0x040004D7 RID: 1239
		public string InventoryClassName = "crate";
	}
}
