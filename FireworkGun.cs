using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Firework Gun", "k1lly0u and Kusha", "10.10.24")]
    [Description("Shoot Firework")]
    class FireworkGun : RustPlugin
    {
        #region Fields
		private const ulong WEAPON_SKIN_ID = 2789113742;
        private const int WEAPON_ID = 1318558775;

        #endregion

        #region Oxide Hooks
		private List<string> fpreflist = new List<string>()
        {
            "assets/prefabs/deployable/fireworks/mortarchampagne.prefab",
            "assets/prefabs/deployable/fireworks/mortargreen.prefab",
            "assets/prefabs/deployable/fireworks/mortarblue.prefab",
            "assets/prefabs/deployable/fireworks/mortarviolet.prefab",
            "assets/prefabs/deployable/fireworks/mortarred.prefab",
        };

        System.Random _rng = new System.Random();
		

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player == null || input == null)
                return;

            if (input.IsDown(BUTTON.FIRE_PRIMARY))
		{
                Item activeItem = player.GetActiveItem();
                if (activeItem == null || activeItem.info.itemid != WEAPON_ID || activeItem.skin != WEAPON_SKIN_ID) return;
				var selfpref = fpreflist[_rng.Next(fpreflist.Count)];
                RepeatingFirework baseEntity = GameManager.server.CreateEntity(selfpref, player.eyes.position) as RepeatingFirework;

                baseEntity.enableSaving = false;
                baseEntity.transform.up = player.eyes.HeadForward();
                baseEntity.Spawn();

                baseEntity.ClientRPC(null, "RPCFire");

                baseEntity.Kill();
        }
		}
		
	
		        #endregion

        #region Commands
        [ChatCommand("firework")]
        private void cmdFirework(BasePlayer player, string command, string[] args)
        {
			if (!player.IsAdmin)
			{
				Puts("No permission to execute this command. You need auth level 2");
				return;
			}
            Item item = ItemManager.CreateByItemID(WEAPON_ID, 1, WEAPON_SKIN_ID);
            BaseProjectile baseProjectile = item.GetHeldEntity()?.GetComponent<BaseProjectile>();
            if (baseProjectile != null && baseProjectile.primaryMagazine.contents > 0)
                baseProjectile.primaryMagazine.contents = 0;

            player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
           
        }
        #endregion
    }
}
