namespace Sandcastle.PrintShop
{
    /// <summary>
    /// A print shop that can draw printing and operating resources from nearby loaded vessels.
    /// Remote vessels are used first; resources on the printer vessel provide the fallback.
    /// </summary>
    [KSPModule("#LOC_SANDCASTLE_printShopTitle")]
    public class WBIDeployedPrintShop : WBIPrintShop
    {
        /// <summary>
        /// Maximum distance in meters between the printer and a part on a supplying vessel.
        /// Set to zero or a negative value to disable remote resource access.
        /// </summary>
        [KSPField]
        public float maxRemoteResourceRange = 20f;

        /// <summary>
        /// Creates the print-shop UI and identifies its material resources as remotely supplied.
        /// </summary>
        public override void OnAwake()
        {
            base.OnAwake();
            shopUI.resourcesAreRemote = true;
        }

        protected override bool consumePrinterResources()
        {
            string error;
            if (RemotePrinterResources.ConsumePrinterResources(part, resHandler,
                maxRemoteResourceRange, out error))
            {
                return true;
            }

            lastUpdateTime = Planetarium.GetUniversalTime();
            updateUIStatus(error);
            if (debugMode)
            {
                UnityEngine.Debug.Log("[Sandcastle] - Cannot print, out of resources to run printer");
                UnityEngine.Debug.Log("[Sandcastle] - Reported error: " + error);
            }
            return false;
        }

        protected override void getMaterialResourceTotals(int resourceID, out double amount,
            out double maxAmount)
        {
            RemotePrinterResources.GetResourceTotals(part, resourceID,
                maxRemoteResourceRange, out amount, out maxAmount);
        }

        protected override void requestMaterialResource(int resourceID, double amount,
            ResourceFlowMode flowMode)
        {
            RemotePrinterResources.RequestResource(part, resourceID, amount, flowMode,
                maxRemoteResourceRange);
        }
    }
}
