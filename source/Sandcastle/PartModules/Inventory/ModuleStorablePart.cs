using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
using Upgradeables;
using KSP.UI.Screens;
using KSP.Localization;
using System.IO;

namespace Sandcastle.Inventory
{
    public class ModuleStorablePart: ModuleCargoPart
    {
        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();

            info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_storablePartDescription"));
            info.AppendLine(" ");
            info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_storablePartDryMass", new string[] { string.Format("{0:n3}", part.mass) }));
            info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_storablePartPackedVolume", new string[] { string.Format("{0:n1}", packedVolume) }));
            if (stackableQuantity > 1)
                info.AppendLine(Localizer.Format("#LOC_SANDCASTLE_storablePartStackingCapacity", stackableQuantity.ToString()));

            return info.ToString();
        }
        // Can we auto-calculate the volume? See https://docs.unity3d.com/ScriptReference/Mesh-bounds.html
    }
}
