using System;
using System.Collections.Generic;
using KSP.Localization;
using UnityEngine;

namespace Sandcastle.PrintShop
{
    /// <summary>
    /// Provides the shared nearby-vessel resource behavior used by deployed and EVA printers.
    /// Nearby vessels are searched from nearest to farthest and the printer's own vessel supplies
    /// any remainder that the remote vessels cannot provide.
    /// </summary>
    internal static class RemotePrinterResources
    {
        private const double ResourceTolerance = 1.0e-8;

        private sealed class NearbyVessel
        {
            internal Vessel vessel;
            internal double distance;
        }

        /// <summary>
        /// Gets the total amount and capacity available from nearby vessels and the printer vessel.
        /// </summary>
        internal static void GetResourceTotals(Part printerPart, int resourceID,
            float maxRemoteResourceRange, out double amount, out double maxAmount)
        {
            amount = 0.0;
            maxAmount = 0.0;
            if (printerPart == null)
                return;

            List<NearbyVessel> nearbyVessels = GetNearbyVessels(printerPart, maxRemoteResourceRange);
            for (int index = 0; index < nearbyVessels.Count; index++)
            {
                double remoteAmount;
                double remoteMaxAmount;
                nearbyVessels[index].vessel.GetConnectedResourceTotals(resourceID,
                    out remoteAmount, out remoteMaxAmount);
                amount += remoteAmount;
                maxAmount += remoteMaxAmount;
            }

            double localAmount;
            double localMaxAmount;
            printerPart.GetConnectedResourceTotals(resourceID, out localAmount, out localMaxAmount);
            amount += localAmount;
            maxAmount += localMaxAmount;
        }

        /// <summary>
        /// Requests a resource from nearby vessels first and then from the printer vessel.
        /// </summary>
        /// <returns>The amount of resource actually supplied.</returns>
        internal static double RequestResource(Part printerPart, int resourceID, double amount,
            ResourceFlowMode flowMode, float maxRemoteResourceRange)
        {
            if (printerPart == null || amount <= 0.0)
                return 0.0;

            // Vessel.RequestResource bypasses Part.RequestResource's stock Infinite Electricity
            // check, so preserve the stock cheat behavior before querying remote vessels.
            if (CheatOptions.InfiniteElectricity &&
                resourceID == PartResourceLibrary.ElectricityHashcode)
            {
                return amount;
            }

            double supplied = 0.0;
            double remaining = amount;
            List<NearbyVessel> nearbyVessels = GetNearbyVessels(printerPart, maxRemoteResourceRange);
            for (int index = 0; index < nearbyVessels.Count && remaining > ResourceTolerance; index++)
            {
                Vessel vessel = nearbyVessels[index].vessel;
                if (vessel.rootPart == null)
                    continue;

                double remoteAmount = vessel.RequestResource(vessel.rootPart, resourceID,
                    remaining, true);
                if (remoteAmount <= 0.0)
                    continue;

                supplied += remoteAmount;
                remaining = Math.Max(0.0, amount - supplied);
            }

            if (remaining <= ResourceTolerance)
                return supplied;

            double localAmount = flowMode == ResourceFlowMode.NULL
                ? printerPart.RequestResource(resourceID, remaining)
                : printerPart.RequestResource(resourceID, remaining, flowMode);
            if (localAmount > 0.0)
                supplied += localAmount;

            return supplied;
        }

        /// <summary>
        /// Consumes the resources required to operate a printer using nearby vessels and then the
        /// printer vessel. This mirrors ModuleResourceHandler's normal availability bookkeeping.
        /// </summary>
        internal static bool ConsumePrinterResources(Part printerPart,
            ModuleResourceHandler resourceHandler, float maxRemoteResourceRange, out string error)
        {
            error = string.Empty;
            if (printerPart == null || resourceHandler == null)
                return false;

            double fixedDeltaTime = TimeWarp.fixedDeltaTime;
            for (int index = 0; index < resourceHandler.inputResources.Count; index++)
            {
                ModuleResource inputResource = resourceHandler.inputResources[index];
                inputResource.currentRequest = inputResource.rate * fixedDeltaTime;

                if (CheatOptions.InfinitePropellant)
                {
                    inputResource.currentAmount = inputResource.currentRequest;
                    inputResource.available = true;
                    continue;
                }

                inputResource.currentAmount = RequestResource(printerPart, inputResource.id,
                    inputResource.currentRequest, inputResource.flowMode, maxRemoteResourceRange);
                inputResource.available = inputResource.currentAmount >=
                    inputResource.currentRequest * 0.1;
                if (inputResource.available)
                    continue;

                error = Localizer.Format("#autoLOC_6001043",
                    KSPUtil.PrintLocalizedModuleName(inputResource.name),
                    (inputResource.currentAmount / fixedDeltaTime).ToString("0.0"),
                    inputResource.rate.ToString("0.0"));
                return false;
            }

            if (resourceHandler.outputResources.Count > 0)
                resourceHandler.UpdateModuleResourceOutputs();

            return true;
        }

        private static List<NearbyVessel> GetNearbyVessels(Part printerPart,
            float maxRemoteResourceRange)
        {
            List<NearbyVessel> nearbyVessels = new List<NearbyVessel>();
            if (!HighLogic.LoadedSceneIsFlight || printerPart == null ||
                printerPart.vessel == null || maxRemoteResourceRange <= 0f ||
                FlightGlobals.VesselsLoaded == null)
            {
                return nearbyVessels;
            }

            for (int index = 0; index < FlightGlobals.VesselsLoaded.Count; index++)
            {
                Vessel vessel = FlightGlobals.VesselsLoaded[index];
                if (vessel == null || vessel == printerPart.vessel || !vessel.loaded ||
                    vessel.parts == null || vessel.parts.Count == 0)
                {
                    continue;
                }

                double distance = GetNearestPartDistance(printerPart, vessel);
                if (distance <= maxRemoteResourceRange)
                {
                    nearbyVessels.Add(new NearbyVessel
                    {
                        vessel = vessel,
                        distance = distance
                    });
                }
            }

            nearbyVessels.Sort(delegate(NearbyVessel first, NearbyVessel second)
            {
                int result = first.distance.CompareTo(second.distance);
                if (result != 0)
                    return result;
                return string.CompareOrdinal(first.vessel.id.ToString(), second.vessel.id.ToString());
            });
            return nearbyVessels;
        }

        private static double GetNearestPartDistance(Part printerPart, Vessel vessel)
        {
            double nearestDistance = double.MaxValue;
            Vector3 printerPosition = printerPart.transform.position;
            for (int index = 0; index < vessel.parts.Count; index++)
            {
                Part vesselPart = vessel.parts[index];
                if (vesselPart == null || vesselPart.transform == null)
                    continue;

                double distance = Vector3.Distance(printerPosition, vesselPart.transform.position);
                if (distance < nearestDistance)
                    nearestDistance = distance;
            }
            return nearestDistance;
        }
    }
}
