using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core;
using Oxide.Core.Configuration;
using System.Collections.Generic;
using System.Linq;
using System;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("AdminTeamManager", "HardRock", "1.0.10")]
    [Description("Allows admins to manage players and teams via commands.")]
    public class AdminTeamManager : CovalencePlugin
    {
        [PluginReference] private Plugin AutomaticAuthorization, HeliSignals, BradleyDrops;

        private const string permissionUse = "adminteammanager.use";
        private int maxTeamSize = 8;
        private StoredData storedData;
        private DynamicConfigFile dataFile;

        #region Hooks and Initialization

        private void Init()
        {
            permission.RegisterPermission(permissionUse, this);
            dataFile = Interface.Oxide.DataFileSystem.GetFile("AdminTeamManager_Data");
            LoadData();
            LoadConfig();
        }

        protected override void LoadDefaultConfig()
        {
            Config["MaxTeamSize"] = maxTeamSize;
            SaveConfig();
        }

        private void LoadConfig()
        {
            maxTeamSize = Convert.ToInt32(Config["MaxTeamSize"] ?? maxTeamSize);
        }

        private void OnServerInitialized()
        {
            ScanAllPlayers();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            UpdatePlayerData(player);
            CheckAndSaveTeam(player);
        }


        #endregion

        #region Data Management

        private class TeamData
        {
            public string LeaderName { get; set; }
            public ulong LeaderID { get; set; }
            public List<PlayerData> Members { get; set; } = new List<PlayerData>();
        }

        private class PlayerData
        {
            public string Name { get; set; }
            public ulong ID { get; set; }
        }

        private class StoredData
        {
            public Dictionary<string, TeamData> Teams = new Dictionary<string, TeamData>();
            public Dictionary<string, ulong> PlayerLookup = new Dictionary<string, ulong>();
        }

        private void LoadData()
        {
            try
            {
                storedData = dataFile.Exists() ? dataFile.ReadObject<StoredData>() : new StoredData();
            }
            catch
            {
                storedData = new StoredData();
            }
        }

        private void SaveData()
        {
            dataFile.WriteObject(storedData);
        }

        private void UpdatePlayerData(BasePlayer player)
        {
            if (!storedData.PlayerLookup.ContainsKey(player.displayName))
            {
                storedData.PlayerLookup[player.displayName] = player.userID;
                SaveData();
            }
        }


        private void OnNewSave(string filename)
        {
            ClearDataOnWipe();
        }

        private void ClearDataOnWipe()
        {
            storedData = new StoredData();
            SaveData();
            Puts("AdminTeamManager_Data file has been cleared due to a new wipe.");
        }

        private void ScanAllPlayers()
        {
            int addedCount = 0;
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (!storedData.PlayerLookup.ContainsKey(player.displayName))
                {
                    storedData.PlayerLookup[player.displayName] = player.userID;
                    addedCount++;
                }

                RelationshipManager.PlayerTeam team = RelationshipManager.ServerInstance?.FindTeam(player.currentTeam);
                if (team != null && !storedData.Teams.ContainsKey(team.teamID.ToString()))
                {
                    storedData.Teams[team.teamID.ToString()] = new TeamData
                    {
                        LeaderName = player.displayName,
                        LeaderID = player.userID,
                        Members = new List<PlayerData> { new PlayerData { Name = player.displayName, ID = player.userID } }
                    };
                }
            }
            foreach (var sleeper in BasePlayer.sleepingPlayerList)
            {
                if (!storedData.PlayerLookup.ContainsKey(sleeper.displayName))
                {
                    storedData.PlayerLookup[sleeper.displayName] = sleeper.userID;
                    addedCount++;
                }
            }
            SaveData();
            Puts($"Scanned all players and sleepers. Added {addedCount} new entries to the data file.");
        }

        private ulong? FindPlayerByName(string name)
        {
            var matches = storedData.PlayerLookup
                .Where(kvp => kvp.Key.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            if (matches.Count == 0)
            {
                return null;
            }
            if (matches.Count > 1)
            {
                PrintWarning($"<Multiple players found matching '{name}': {string.Join(", ", matches.Select(m => m.Key))}");
                return null;
            }
            return matches.First().Value;
        }

        #endregion

        #region Utility Functions

        private bool HasPermissionAndValidArgs(IPlayer player, string[] args, int requiredArgs, string usageMessage)
        {
            if (!player.HasPermission(permissionUse))
            {
                player.Reply("<color=yellow>[INFO]:</color>  You do not have permission to use this command.");
                return false;
            }
            if (args.Length != requiredArgs)
            {
                player.Reply(usageMessage);
                return false;
            }
            return true;
        }

        private BasePlayer GetPlayerByNameOrID(string input)
        {
            ulong? playerID = null;

            if (ulong.TryParse(input, out ulong id))
            {
                playerID = id;
            }
            else
            {
                playerID = FindPlayerByName(input);
            }

            return playerID != null ? BasePlayer.FindAwakeOrSleeping(playerID.ToString()) : null;
        }

        private void CheckAndSaveTeam(BasePlayer player)
        {
            RelationshipManager.PlayerTeam team = RelationshipManager.ServerInstance?.FindPlayersTeam(player.userID);

            if (team == null || storedData.Teams.ContainsKey(team.teamID.ToString()))
                return; 

            BasePlayer leader = BasePlayer.FindByID(team.teamLeader);
            if (leader == null) return;

            storedData.Teams[team.teamID.ToString()] = new TeamData
            {
                LeaderName = leader.displayName,
                LeaderID = leader.userID,
                Members = team.members.Select(memberID =>
                {
                    BasePlayer teamMember = BasePlayer.FindByID(memberID);
                    return new PlayerData
                    {
                        Name = teamMember != null ? teamMember.displayName : "Unknown",
                        ID = memberID
                    };
                }).ToList()
            };

            SaveData();
            Puts($" Team created: {leader.displayName} (ID: {team.teamID}) with {team.members.Count} members.");
        }

        private void OnTeamCreated(BasePlayer player, RelationshipManager.PlayerTeam team)
        {
            if (player == null || team == null)
                return;

            string teamID = team.teamID.ToString();

            if (storedData.Teams.ContainsKey(teamID))
                return; // Already saved somehow

            storedData.Teams[teamID] = new TeamData
            {
                LeaderName = player.displayName,
                LeaderID = player.userID,
                Members = new List<PlayerData>
        {
            new PlayerData
            {
                Name = player.displayName,
                ID = player.userID
            }
        }
            };

            SaveData();
            Puts($"[AdminTeamManager] New team created by {player.displayName} ({player.userID}), Team ID {teamID} added to data file.");
        }

        private void OnTeamUpdated(ulong teamID, ulong _)
        {
            if (!storedData.Teams.TryGetValue(teamID.ToString(), out var teamData))
                return;

            var serverTeam = RelationshipManager.ServerInstance?.FindTeam(teamID);
            if (serverTeam == null)
                return;

            var serverMembers = serverTeam.members;

            // Find members that exist in storedData but not in server
            var removedMembers = teamData.Members
                .Where(m => !serverMembers.Contains(m.ID))
                .ToList();

            foreach (var removed in removedMembers)
            {
                teamData.Members.RemoveAll(m => m.ID == removed.ID);
                Puts($"[AdminTeamManager] Removed {removed.Name} ({removed.ID}) from Team {teamID} (left the team).");
            }

            // Find members that exist in server but not in storedData
            foreach (var serverMemberID in serverMembers)
            {
                if (!teamData.Members.Any(m => m.ID == serverMemberID))
                {
                    BasePlayer player = BasePlayer.FindByID(serverMemberID) ?? BasePlayer.FindSleeping(serverMemberID.ToString());
                    string name = player != null ? player.displayName : "Unknown";

                    teamData.Members.Add(new PlayerData
                    {
                        Name = name,
                        ID = serverMemberID
                    });
                    Puts($"[AdminTeamManager] Added {name} ({serverMemberID}) to Team {teamID} (joined the team).");
                }
            }

            // If no members left, delete team
            if (teamData.Members.Count == 0)
            {
                storedData.Teams.Remove(teamID.ToString());
                Puts($"[AdminTeamManager] Team {teamID} deleted (no members left).");
            }

            SaveData();
        }

        private object OnTeamLeave(RelationshipManager.PlayerTeam team, BasePlayer player)
        {
            if (team == null || player == null)
                return null;

            string teamID = team.teamID.ToString();

            if (!storedData.Teams.TryGetValue(teamID, out var teamData))
                return null;

            // Remove the player from storedData
            if (teamData.Members.RemoveAll(m => m.ID == player.userID) > 0)
            {
                Puts($"[AdminTeamManager] {player.displayName} ({player.userID}) left or was kicked from Team {teamID}, removed from data file.");
                SaveData();
            }

            // Delete the team if no members left
            if (teamData.Members.Count == 0)
            {
                storedData.Teams.Remove(teamID);
                Puts($"[AdminTeamManager] Team {teamID} deleted (no members left).");
                SaveData();
            }

            return null;
        }


        // Called when a team is fully disbanded
        private void OnTeamDisbanded(ulong teamID)
        {
            if (storedData.Teams.Remove(teamID.ToString()))
            {
                SaveData();
                Puts($"[AdminTeamManager] Team {teamID} has been disbanded and removed.");
            }
        }

        private void AuthorizePlayerOnTeamCodeLocks(BasePlayer player, RelationshipManager.PlayerTeam team)
        {
            foreach (var memberID in team.members)
            {
                BasePlayer member = BasePlayer.FindByID(memberID);
                if (member == null) continue;

                List<CodeLock> locks = new List<CodeLock>();

                List<BaseEntity> entities = BaseNetworkable.serverEntities
                    .OfType<BaseEntity>()
                    .Where(e => e.OwnerID == member.userID)
                    .ToList();

                foreach (var entity in entities)
                {
                    var codeLock = entity.GetSlot(BaseEntity.Slot.Lock) as CodeLock;
                    if (codeLock != null)
                    {
                        if (!locks.Contains(codeLock))
                        {
                            locks.Add(codeLock);
                        }
                    }
                }

                foreach (var codeLock in locks)
                {
                    if (!codeLock.whitelistPlayers.Contains(player.userID))
                    {
                        codeLock.whitelistPlayers.Add(player.userID);
                        codeLock.SendNetworkUpdate();
                    }
                }
            }
        }


        #endregion

        #region Commands

        [Command("newteam")]
        private void CreateTeamCommand(IPlayer admin, string command, string[] args)
        {
            if (!HasPermissionAndValidArgs(admin, args, 1, "<color=yellow>[INFO]:</color> Usage: /newteam <leaderNameOrID>"))
                return;

            BasePlayer leader = GetPlayerByNameOrID(args[0]);
            if (leader == null || leader.currentTeam != 0)
            {
                admin.Reply("<color=yellow>[INFO]:</color> Invalid leader or already in a team.");
                return;
            }

            var newTeam = RelationshipManager.ServerInstance?.CreateTeam();
            if (newTeam == null) return;

            newTeam.AddPlayer(leader);
            newTeam.SetTeamLeader(leader.userID);

            storedData.Teams[newTeam.teamID.ToString()] = new TeamData
            {
                LeaderName = leader.displayName,
                LeaderID = leader.userID,
                Members = new List<PlayerData> { new PlayerData { Name = leader.displayName, ID = leader.userID } }
            };
            SaveData();

            admin.Reply($"New team created with {leader.displayName} ({leader.userID}) as leader.");
        }

        [Command("addteam")]
        private void AddTeamCommand(IPlayer admin, string command, string[] args)
        {
            if (!HasPermissionAndValidArgs(admin, args, 2, "Usage: /addteam <teamMemberNameOrID> <playerToAddNameOrID>"))
                return;

            BasePlayer teamMember = GetPlayerByNameOrID(args[0]);
            ulong teamMemberID = 0;

            if (teamMember != null)
            {
                teamMemberID = teamMember.userID;
            }
            else
            {
                ulong? foundID = FindPlayerByName(args[0]);
                if (foundID.HasValue)
                {
                    teamMemberID = foundID.Value;
                }
            }

            if (teamMemberID == 0)
            {
                admin.Reply("The referenced team member was not found.");
                return;
            }

            RelationshipManager.PlayerTeam team = RelationshipManager.ServerInstance?.FindPlayersTeam(teamMemberID);

            if (team == null)
            {
                admin.Reply($"Player with ID {teamMemberID} is not in a team.");
                return;
            }

            BasePlayer playerToAdd = GetPlayerByNameOrID(args[1]);

            if (playerToAdd == null)
            {
                admin.Reply("The player to add was not found or is offline.");
                return;
            }

            if (playerToAdd.currentTeam != 0)
            {
                admin.Reply($"{playerToAdd.displayName} is already in another team. Remove them first.");
                return;
            }

            if (team.members.Contains(playerToAdd.userID))
            {
                admin.Reply($"{playerToAdd.displayName} is already in the team.");
                return;
            }

            if (team.members.Count >= maxTeamSize)
            {
                admin.Reply("The team is full.");
                return;
            }

            team.AddPlayer(playerToAdd);
            playerToAdd.currentTeam = team.teamID;
            AuthorizePlayerOnTeamCodeLocks(playerToAdd, team);

            // Update plugin's storedData based on real in-game team info
            if (RelationshipManager.ServerInstance.FindTeam(team.teamID) is RelationshipManager.PlayerTeam updatedTeam)
            {
                var leader = BasePlayer.FindByID(updatedTeam.teamLeader) ?? BasePlayer.FindSleeping(updatedTeam.teamLeader.ToString());
                if (leader != null)
                {
                    storedData.Teams[updatedTeam.teamID.ToString()] = new TeamData
                    {
                        LeaderName = leader.displayName,
                        LeaderID = leader.userID,
                        Members = updatedTeam.members.Select(memberID =>
                        {
                            var member = BasePlayer.FindByID(memberID) ?? BasePlayer.FindSleeping(memberID.ToString());
                            return new PlayerData
                            {
                                Name = member != null ? member.displayName : "Unknown",
                                ID = memberID
                            };
                        }).ToList()
                    };
                    SaveData();
                }
            }

            foreach (ulong memberID in team.members)
            {
                BasePlayer member = BasePlayer.FindByID(memberID) ?? BasePlayer.FindSleeping(memberID.ToString());
                if (member != null && member.IsConnected)
                {
                    member.UpdateTeam(team.teamID);
                }
            }

            AutomaticAuthorization?.Call("UpdateAuthList", teamMemberID, -1);
            AutomaticAuthorization?.Call("UpdateAuthList", playerToAdd.userID, -1);
            HeliSignals?.Call("UpdateHeliTeam", teamMemberID, playerToAdd.userID);
            BradleyDrops?.Call("UpdateBradleyTeam", teamMemberID, playerToAdd.userID);

            admin.Reply($"{playerToAdd.displayName} has been added to the team.");
        }

        [Command("removeplayer")]
        private void RemoveTeamPlayerCommand(IPlayer admin, string command, string[] args)
        {
            if (!HasPermissionAndValidArgs(admin, args, 2, "<color=yellow>[INFO]:</color> Usage: /removeplayer <teamLeaderNameOrID> <playerNameOrID>"))
                return;

            BasePlayer leader = GetPlayerByNameOrID(args[0]);
            BasePlayer playerToRemove = GetPlayerByNameOrID(args[1]);
            if (leader == null || playerToRemove == null) return;

            RelationshipManager.PlayerTeam team = RelationshipManager.ServerInstance?.FindTeam(leader.currentTeam);
            if (team == null || !team.members.Contains(playerToRemove.userID)) return;

            team.RemovePlayer(playerToRemove.userID);
            storedData.Teams[team.teamID.ToString()].Members.RemoveAll(member => member.ID == playerToRemove.userID);
            SaveData();

            admin.Reply($"{playerToRemove.displayName} ({playerToRemove.userID}) removed from {leader.displayName}'s team.");
        }

        [Command("changeleader")]
        private void ChangeLeaderCommand(IPlayer admin, string command, string[] args)
        {
            if (!HasPermissionAndValidArgs(admin, args, 2, "<color=yellow>[INFO]:</color> Usage: /changeleader <currentLeaderNameOrID> <newLeaderNameOrID>"))
                return;

            BasePlayer currentLeader = GetPlayerByNameOrID(args[0]);
            BasePlayer newLeader = GetPlayerByNameOrID(args[1]);
            if (currentLeader == null || newLeader == null) return;

            RelationshipManager.PlayerTeam team = RelationshipManager.ServerInstance?.FindTeam(currentLeader.currentTeam);
            if (team == null || !team.members.Contains(newLeader.userID)) return;

            team.SetTeamLeader(newLeader.userID);
            storedData.Teams[team.teamID.ToString()].LeaderName = newLeader.displayName;
            SaveData();

            admin.Reply($"<color=yellow>[INFO]:</color> Leadership transferred from {currentLeader.displayName} to {newLeader.displayName}.");
        }

        [Command("teaminfo")]
        private void TeamInfoCommand(IPlayer admin, string command, string[] args)
        {
            if (!HasPermissionAndValidArgs(admin, args, 1, "<color=yellow>[INFO]:</color> Usage: /teaminfo <leaderNameOrID>"))
                return;

            BasePlayer leader = GetPlayerByNameOrID(args[0]);
            if (leader == null) return;

            RelationshipManager.PlayerTeam team = RelationshipManager.ServerInstance?.FindTeam(leader.currentTeam);
            if (team == null || !storedData.Teams.ContainsKey(team.teamID.ToString()))
            {
                admin.Reply("Team not found.");
                return;
            }

            var teamData = storedData.Teams[team.teamID.ToString()];
            string message = $"Team Leader: {teamData.LeaderName} ({teamData.LeaderID})\nMembers:";
            foreach (var member in teamData.Members)
            {
                message += $"\n- {member.Name} ({member.ID})";
            }

            admin.Reply(message);
        }

        [Command("cleardata")]
        private void ClearDataCommand(IPlayer admin, string command, string[] args)
        {
            if (!admin.HasPermission(permissionUse))
            {
                admin.Reply("<color=yellow>[INFO]:</color> You do not have permission to use this command.");
                return;
            }

            storedData = new StoredData();
            SaveData();

            admin.Reply("AdminTeamManager data file has been cleared.");
            Puts("AdminTeamManager data file has been cleared by an admin.");
        }

        [Command("scanplayers")]
        private void ScanPlayersCommand(IPlayer admin, string command, string[] args)
        {
            if (!admin.HasPermission(permissionUse))
            {
                admin.Reply("<color=yellow>[INFO]:</color> You do not have permission to use this command.");
                return;
            }

            ScanAllPlayers();
            admin.Reply("Scanned all active players and sleepers. Data file has been updated with players and their teams.");
        }

        [Command("scanteams")]
        private void ScanTeamsCommand(IPlayer admin, string command, string[] args)
        {
            if (!admin.HasPermission(permissionUse))
            {
                admin.Reply("<color=yellow>[INFO]:</color> You do not have permission to use this command.");
                return;
            }

            int teamsAdded = 0;

            foreach (var teamEntry in RelationshipManager.ServerInstance.teams)
            {
                var team = teamEntry.Value;
                if (team == null || storedData.Teams.ContainsKey(team.teamID.ToString()))
                    continue;

                BasePlayer leader = BasePlayer.FindByID(team.teamLeader) ?? BasePlayer.FindSleeping(team.teamLeader.ToString());
                if (leader == null)
                    continue;

                storedData.Teams[team.teamID.ToString()] = new TeamData
                {
                    LeaderName = leader.displayName,
                    LeaderID = leader.userID,
                    Members = team.members.Select(memberID =>
                    {
                        BasePlayer member = BasePlayer.FindByID(memberID) ?? BasePlayer.FindSleeping(memberID.ToString());
                        return new PlayerData
                        {
                            Name = member != null ? member.displayName : "Unknown",
                            ID = memberID
                        };
                    }).ToList()
                };

                teamsAdded++;
            }

            SaveData();
            admin.Reply($"<color=yellow>[INFO]:</color> Scanned and added {teamsAdded} existing teams into the data file.");
            Puts($"[AdminTeamManager] {teamsAdded} teams added via /scanteams.");
        }

        #endregion
    }
}
