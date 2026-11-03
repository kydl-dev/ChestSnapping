using Kydl.Common;
using Kydl.Core.Features;

namespace ChestSnapping
{
    /// <summary>Snap points on chests. Gate: EnableAll and [Prefabs] Enable. The pieces are updated by Plugin.Update after a switch changes.</summary>
    internal sealed class SnappingFeature : IFeature
    {
        internal const string FeatureId = "snapping";

        /// <summary>True between Start and Stop.</summary>
        internal static bool Running { get; private set; }

        public string Id { get { return FeatureId; } }

        public bool IsEnabled() { return ModSettings.PrefabsOn; }

        public bool Start()
        {
            Running = true;
            ModLog.Verbose("SnappingFeature: started");
            return Running;
        }

        public bool Stop()
        {
            Running = false;
            ModLog.Verbose("SnappingFeature: stopped");
            return !Running;
        }
    }
}
