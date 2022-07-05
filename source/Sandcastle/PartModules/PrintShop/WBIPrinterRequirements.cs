using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandcastle.Inventory;
using UnityEngine;
using KSP.Localization;
using WildBlueCore;

namespace Sandcastle.PrintShop
{
    /// <summary>
    /// Describes the 3D Printer requirements for the part. This is a stub part module; the real functionality is over in PrinterInfoHelper.
    /// We have to do this because GetInfo is called during game start, we rely on PartLoader to get information about other parts that are needed to 3D print this part,
    /// and not all of the parts will be loaded when GetInfo is called.
    /// </summary>
    [KSPModule("#LOC_SANDCASTLE_printRequirementsTitle")]
    public class WBIPrinterRequirements: BasePartModule
    {
        [KSPField]
        public string requirementsInfo = "Placeholder. Filled out via PrinterInfoHelper.";

        public override string GetInfo()
        {
            return requirementsInfo;
        }
    }
}
