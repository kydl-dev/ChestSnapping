using KydlCore.Common;
using KydlCore.Core;

namespace ChestSnapping
{
    /// <summary>Applies the saved per-prefab point adjustments. Gate: EnableAll and [Offsets] Enable.</summary>
    internal sealed class AdjustmentsFeature : IFeature
    {
        internal const string FeatureId = "adjustments";

        /// <summary>True between Start and Stop.</summary>
        internal static bool Running { get; private set; }

        public string Id { get { return FeatureId; } }

        public bool IsEnabled() { return ModSettings.OffsetsOn; }

        public bool Start()
        {
            Running = true;
            ModLog.Verbose("AdjustmentsFeature: started");
            return Running;
        }

        public bool Stop()
        {
            Running = false;
            ModLog.Verbose("AdjustmentsFeature: stopped");
            return !Running;
        }
    }
}
