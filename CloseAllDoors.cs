using Oxide.Core;
using Oxide.Core.Plugins;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CloseAllDoors", "GigaBit", "1.1.7")]
    [Description("Closes all player-placed doors across the map.")]

    public class CloseAllDoors : RustPlugin
    {
        private string closeDoorsMessage = "Door Closing Initiated";

        protected override void LoadDefaultConfig()
        {
            Config["CloseDoorsMessage"] = closeDoorsMessage;
            SaveConfig();
        }

        private void OnServerInitialized()
        {
            closeDoorsMessage = Config["CloseDoorsMessage"].ToString();
            Puts("CloseAllDoors plugin initialized with message: " + closeDoorsMessage);
        }

        private void CloseAllPlayerDoors()
        {
            int doorCount = 0;
            Puts("Starting to close all player-placed doors...");

            // Use the entity list, filters only doors to make it less intense on server resources
            var doors = BaseNetworkable.serverEntities.OfType<Door>().Where(door => door.IsOpen());

            foreach (var door in doors)
            {
                // Close the door
                door.SetFlag(BaseEntity.Flags.Open, false);
                door.SendNetworkUpdateImmediate();
                doorCount++;
            }

            Puts($"All player-placed doors have been closed. Total doors closed: {doorCount}");
            SendChatMessageToAllPlayers(closeDoorsMessage);
        }

        private void SendChatMessageToAllPlayers(string message)
        {
            Puts("Sending chat message to all players: " + message);
            foreach (var player in BasePlayer.activePlayerList)
            {
                player.ChatMessage(message);
            }
        }

        [ConsoleCommand("closedoors")]
        private void CloseDoorsCommand(ConsoleSystem.Arg arg)
        {
            Puts("closedoors command invoked");

            if (arg.Connection != null) // Check if the command is sent from the console
            {
                Puts("This command can only be run from the server console.");
                return;
            }

            Puts("Executing closedoors command...");
            CloseAllPlayerDoors();
        }
    }
}
