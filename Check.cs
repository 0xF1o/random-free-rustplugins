using Newtonsoft.Json;
using System;
using UnityEngine;

/*
 * Changelog:
 *
 * Version 1.0.7:
 * - Added Use Permission.
 */

namespace Oxide.Plugins
{
    [Info("Check", "Wrecks", "1.0.7")]
    [Description("Check Item Properties.")]
    public class Check : RustPlugin
    {
        #region Variables

        private const string UsePermission = "Check.use";

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(UsePermission, this);
        }

        #endregion

        #region Config

        private Configuration _config;

        public class Configuration
        {
            [JsonProperty("Note SkinID")] public ulong SkinId;

            public static Configuration DefaultConfig()
            {
                return new Configuration {
                    SkinId = 3341490454
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) LoadDefaultConfig();
                SaveConfig();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                PrintWarning("Creating new configuration file.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => _config = Configuration.DefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(_config);

        #endregion

        #region Command

        [ChatCommand("check")]
        private void CheckCommand(BasePlayer player)
        {
            if (player.IsAdmin || permission.UserHasPermission(player.UserIDString, UsePermission))
            {
                var item = player.GetActiveItem();
                if (item != null)
                {
                    CheckItem(player, item);
                }
                else
                {
                    CheckRaycast(player);
                }
            }
        }

        #endregion

        #region ItemCheck

        private void CheckItem(BasePlayer player, Item item)
        {
            var itemName = string.IsNullOrEmpty(item.name) ? item.info.displayName.english : item.name;
            var itemSkinId = item.skin;
            var itemShortname = item.info?.shortname ?? "Invalid";
            var itemId = item.info?.itemid;
            var itemtext = item.text ?? "No Text";
            var note = ItemManager.CreateByName("note");
            if (note == null) return;
            note.skin = _config.SkinId;
            note.name = "Properties for " + itemName;
            note.text = $"Item Name: {itemName}\nSkinID: {itemSkinId}\nItemID: {itemId}\nShortname: {itemShortname}\nText: {itemtext}";
            player.GiveItem(note);
            LogItem(itemName, itemSkinId, itemId, itemShortname, itemtext);
        }

        #endregion

        #region PrefabCheck

        private void CheckRaycast(BasePlayer player)
        {
            if (Physics.Raycast(player.eyes.HeadRay(), out var hit, 5f))
            {
                var hitEntity = hit.GetEntity();
                if (hitEntity != null)
                {
                    var prefabId = hitEntity.prefabID;
                    var netid = hitEntity.net.ID;
                    var prefabShortname = hitEntity.ShortPrefabName;
                    var prefabPath = hitEntity.PrefabName;
                    var layerMask = LayerMask.LayerToName(hitEntity.gameObject.layer);
                    var note = ItemManager.CreateByName("note");
                    if (note == null) return;
                    note.skin = _config.SkinId;
                    note.name = "Properties for " + prefabShortname;
                    note.text = $"Prefab ID: {prefabId}\nNetID: {netid}\nPrefab Shortname: {prefabShortname}\nPrefab Path: {prefabPath}\nLayerMask: {layerMask}";
                    player.GiveItem(note);
                    LogPrefab(prefabId, netid, prefabShortname, prefabPath, layerMask);
                }
                else
                {
                    player.ChatMessage("Entity is Invalid.");
                }
            }
            else
            {
                player.ChatMessage("Entity not found.");
            }
        }

        #endregion

        #region Loggers

        private void LogItem(string itemName, ulong itemSkinId, int? itemId, string itemShortname, string itemtext)
        {
            Puts($"\n<color=green>Item Properties for {itemName}</color>\n" + $"<color=yellow>Item Name:</color> {itemName}\n" + $"<color=yellow>SkinID:</color> {itemSkinId}\n" + $"<color=yellow>ItemID:</color> {itemId}\n" + $"<color=yellow>Shortname:</color> {itemShortname}\n" + $"<color=yellow>Text:</color> {itemtext}");
        }

        private void LogPrefab(uint prefabId, NetworkableId netid, string prefabShortname, string prefabPath, string layerMask)
        {
            Puts($"\n<color=green>Prefab Properties</color>\n" + $"<color=yellow>Prefab ID:</color> {prefabId}\n" + $"<color=yellow>NetID:</color> {netid}\n" + $"<color=yellow>Prefab Shortname:</color> {prefabShortname}\n" + $"<color=yellow>Prefab Path:</color> {prefabPath}\n" + $"<color=yellow>LayerMask:</color> {layerMask}");
        }

        #endregion
    }
}