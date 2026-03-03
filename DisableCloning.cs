using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("DisableCloning", "0xCC", "0.1.0")]
    [Description("Correctly prevents cloning")]
    class DisableCloning : RustPlugin
    {
        object CanTakeCutting(BasePlayer player, GrowableEntity entity)
        {
            if (player == null || entity == null) return null;

            SendReply(player, $"Trying to clone prefabID={entity.prefabID} skinID={entity.skinID} ShortPrefabName={entity.ShortPrefabName}");

            if (entity.ShortPrefabName.Contains("hemp"))
            {
                SendReply(player, "Cloning this is disabled!");
                return false; 
            }

            return null;
        }
    }
}