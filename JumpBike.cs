/*▄▄▄    ███▄ ▄███▓  ▄████  ▄▄▄██▀▀▀▓█████▄▄▄█████▓
▓█████▄ ▓██▒▀█▀ ██▒ ██▒ ▀█▒   ▒██   ▓█   ▀▓  ██▒ ▓▒
▒██▒ ▄██▓██    ▓██░▒██░▄▄▄░   ░██   ▒███  ▒ ▓██░ ▒░
▒██░█▀  ▒██    ▒██ ░▓█  ██▓▓██▄██▓  ▒▓█  ▄░ ▓██▓ ░ 
░▓█  ▀█▓▒██▒   ░██▒░▒▓███▀▒ ▓███▒   ░▒████▒ ▒██▒ ░ 
░▒▓███▀▒░ ▒░   ░  ░ ░▒   ▒  ▒▓▒▒░   ░░ ▒░ ░ ▒ ░░   
▒░▒   ░ ░  ░      ░  ░   ░  ▒ ░▒░    ░ ░  ░   ░    
 ░    ░ ░      ░   ░ ░   ░  ░ ░ ░      ░    ░      
 ░             ░         ░  ░   ░      ░  ░*/
using HarmonyLib;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("JumpBike", "bmgjet", "1.0.4")]
    class JumpBike : RustPlugin
    {
        private static JumpBike plugin;
        private readonly string permUse = "JumpBike.Allow";
        private readonly string permUnlimitedFuel = "JumpBike.Fuel";
        private readonly string permMyBike = "JumpBike.MyBike";
        private readonly string permView = "JumpBike.View";
        private readonly string permSuper = "JumpBike.Super";
        private Dictionary<ulong, ModDefaults> jumpBikes = new Dictionary<ulong, ModDefaults>();
        private List<ulong> TPView = new List<ulong>();

        public class ModDefaults
        {
            public object idleFuelPerSec;
            public object maxFuelPerSec;
            public object airControlTorquePower;
            public object sprintTime;
            public object sprintRegenTime;
            public object sprintBoostPercent;
            public object playerDamageThreshold;
            public object playerDeathThreshold;

            public ModDefaults Init(Bike bike)
            {
                idleFuelPerSec = bike.idleFuelPerSec;
                maxFuelPerSec = bike.maxFuelPerSec;
                airControlTorquePower = bike.airControlTorquePower;
                sprintTime = bike.sprintTime;
                sprintRegenTime = bike.sprintRegenTime;
                sprintBoostPercent = bike.sprintBoostPercent;
                playerDamageThreshold = bike.playerDamageThreshold;
                playerDeathThreshold = bike.playerDeathThreshold;
                return this;
            }
        }

        #region Config
        private Configuration config;
        private class Configuration
        {
            [JsonProperty("Jump Cool Down (Sec)")]
            public int JumpCoolDown = 2;

            [JsonProperty("Jump Force (Mass X ThisValue)")]
            public float JumpForce = 4;

            [JsonProperty("[Super] Jump Force (Mass X ThisValue)")]
            public float SuperJumpForce = 6;

            [JsonProperty("Disable Bike Collision Damage")]
            public bool NoBikeDamage = false;

            [JsonProperty("Jump Sound Effect (Blank = Disabled)")]
            public string JumpFX = "assets/bundled/prefabs/fx/player/gutshot_scream.prefab";

            [JsonProperty("Third Person View Distance")]
            public float ViewDistance = 4.5f;

            [JsonProperty("Enable Speed Boost On Motor Bikes (Applies To All Motor Bikes)")]
            public bool motorBikeSprint = false;

            [JsonProperty("[MotorBike] Air Control Shift/Ctrl Torque")]
            public float airControlTorquePowerMotorBike = 0.075f;

            [JsonProperty("[MotorBike] How Many Seconds Of Sprint Pedal Bike Has")]
            public float sprintTimeMotorBike = 5f;

            [JsonProperty("[MotorBike] How Many Seconds To Regen Sprint")]
            public float sprintRegenTimeMotorBike = 10f;

            [JsonProperty("[MotorBike] Sprint Speed Boost Amount")]
            public float sprintBoostPercentMotorBike = 0.3f;

            [JsonProperty("[MotorBike] Player Damage Threshold (Amount Of Collision Damage For Bike To Hurt Player)")]
            public float playerDamageThresholdMotorBike = 40;

            [JsonProperty("[MotorBike] Player Death Threshold (Amount Of Collision Damage For Bike To Kill Player)")]
            public float playerDeathThresholdMotorBike = 80;

            [JsonProperty("[MotorBike_Sidecar] Air Control Shift/Ctrl Torque")]
            public float airControlTorquePowerMotorBike_Sidecar = 0.075f;

            [JsonProperty("[MotorBike_Sidecar] How Many Seconds Of Sprint Pedal Bike Has")]
            public float sprintTimeMotorBike_Sidecar = 5f;

            [JsonProperty("[MotorBike_Sidecar] How Many Seconds To Regen Sprint")]
            public float sprintRegenTimeMotorBike_Sidecar = 10f;

            [JsonProperty("[MotorBike_Sidecar] Sprint Speed Boost Amount")]
            public float sprintBoostPercentMotorBike_Sidecar = 0.3f;

            [JsonProperty("[MotorBike_Sidecar] Player Damage Threshold (Amount Of Collision Damage For Bike To Hurt Player)")]
            public float playerDamageThresholdMotorBike_Sidecar = 40;

            [JsonProperty("[MotorBike_Sidecar] Player Death Threshold (Amount Of Collision Damage For Bike To Kill Player)")]
            public float playerDeathThresholdMotorBike_Sidecar = 80;

            [JsonProperty("[PedalBike] Air Control Shift/Ctrl Torque")]
            public float airControlTorquePowerPedalBike = 0.075f;

            [JsonProperty("[PedalBike] How Many Seconds Of Sprint Pedal Bike Has")]
            public float sprintTimePedalBike = 5f;

            [JsonProperty("[PedalBike] How Many Seconds To Regen Sprint")]
            public float sprintRegenTimePedalBike = 10f;

            [JsonProperty("[PedalBike] Sprint Speed Boost Amount")]
            public float sprintBoostPercentPedalBike = 0.3f;

            [JsonProperty("[PedalBike] Player Damage Threshold (Amount Of Collision Damage For Bike To Hurt Player)")]
            public float playerDamageThresholdPedalBike = 40;

            [JsonProperty("[PedalBike] Player Death Threshold (Amount Of Collision Damage For Bike To Kill Player)")]
            public float playerDeathThresholdPedalBike = 75;

            [JsonProperty("[PedalTrike] Air Control Shift/Ctrl Torque")]
            public float airControlTorquePowerPedalTrike = 0.075f;

            [JsonProperty("[PedalTrike] How Many Seconds Of Sprint Pedal Bike Has")]
            public float sprintTimePedalTrike = 5f;

            [JsonProperty("[PedalTrike] How Many Seconds To Regen Sprint")]
            public float sprintRegenTimePedalTrike = 10f;

            [JsonProperty("[PedalTrike] Sprint Speed Boost Amount")]
            public float sprintBoostPercentPedalTrike = 0.3f;

            [JsonProperty("[PedalTrike] Player Damage Threshold (Amount Of Collision Damage For Bike To Hurt Player)")]
            public float playerDamageThresholdPedalTrike = 40;

            [JsonProperty("[PedalTrike] Player Death Threshold (Amount Of Collision Damage For Bike To Kill Player)")]
            public float playerDeathThresholdPedalTrike = 75;

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) { throw new JsonException(); }
                if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    PrintWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                PrintWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            PrintWarning($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }
        #endregion

        #region Chat Commands
        [ChatCommand("mybike")]
        private void MyBike(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, permMyBike)) { return; }
            if (args.Length != 1)
            {
                player.ChatMessage("Command /mybike set   -  Set A Bike");
                player.ChatMessage("Command /mybike get   -  Get A Bike");
                return;
            }
            if (args[0] == "set")
            {
                if (player?.GetMountedVehicle() is not Bike)
                {
                    player.ChatMessage("You Must Be Sitting On A Bike");
                    return;
                }
                player?.SetInfo("MyBike", player?.GetMountedVehicle()?.net?.ID.Value.ToString());
                player.ChatMessage("Temporary Bike ID Set!");
            }
            if (args[0] == "get")
            {
                ulong ID = 0;
                if (ulong.TryParse(player.GetInfoString("MyBike", "0"), out ID))
                {
                    if (ID != 0)
                    {
                        BaseNetworkable baseNetworkable = BaseNetworkable.serverEntities.Find(new NetworkableId(ID));
                        if (baseNetworkable == null || baseNetworkable.IsDestroyed)
                        {
                            player.ChatMessage("No /mybike set");
                            return;
                        }
                        Bike bike = baseNetworkable as Bike;
                        if (bike != null)
                        {
                            TPBike(bike, player);
                            return;
                        }
                    }
                }
            }
        }
        #endregion

        #region Oxide Hooks
        private void Init() { plugin = this; permission.RegisterPermission(permUse, this); permission.RegisterPermission(permUnlimitedFuel, this); permission.RegisterPermission(permMyBike, this); permission.RegisterPermission(permView, this); permission.RegisterPermission(permSuper, this); }

        private void Unload()
        {
            foreach (var ID in jumpBikes)
            {
                BaseNetworkable baseNetworkable = BaseNetworkable.serverEntities.Find(new NetworkableId(ID.Key));
                if (baseNetworkable != null)
                {
                    Bike bike = baseNetworkable as Bike;
                    if (bike != null && !bike.IsDestroyed) { bike.DismountAllPlayers(); }
                }
            }
            foreach (var ID in TPView)
            {
                BasePlayer player = BasePlayer.FindAwakeOrSleepingByID(ID);
                if (player != null) { ToggleThirdPerson(player, false); }
            }
            plugin = null;
        }

        private void OnPlayerSleepEnded(BasePlayer player)
        {
            if (TPView.Contains(player.userID))
            {
                ToggleThirdPerson(player, false);
                TPView.Remove(player.userID);
            }
        }

        private void OnEntityMounted(BaseMountable mountable, BasePlayer player)
        {
            Bike bike = mountable?.VehicleParent() as Bike;
            if (bike != null)
            {
                if (!jumpBikes.ContainsKey(bike.net.ID.Value) && permission.UserHasPermission(player.UserIDString, permUse))
                {
                    jumpBikes.Add(bike.net.ID.Value, new ModDefaults().Init(bike));
                    ModBike(bike, player);
                }
            }
        }

        private void OnEntityDismounted(BaseMountable mountable, BasePlayer player)
        {
            Bike bike = mountable?.VehicleParent() as Bike;
            if (bike != null)
            {
                if (jumpBikes.ContainsKey(bike.net.ID.Value))
                {
                    ModBike(bike, player, false);
                    jumpBikes.Remove(bike.net.ID.Value);
                }
                if (TPView.Contains(player.userID))
                {
                    ToggleThirdPerson(player, false);
                    TPView.Remove(player.userID);
                }
            }
        }
        #endregion       

        #region Harmony
        [AutoPatch]
        [HarmonyPatch(typeof(Bike), "AwakeBikePhysicsTick")]
        internal class Bike_AwakeBikePhysicsTick
        {
            [HarmonyPostfix]
            static void Postfix(Bike __instance, CarPhysics<Bike> ___carPhysics)
            {
                BasePlayer player = __instance?.GetDriver();
                if (player == null) { return; }
                if (player.serverInput.IsDown(BUTTON.FIRE_SECONDARY) && player.GetInfoInt("BikeView", 0) <= Time.realtimeSinceStartup && plugin.permission.UserHasPermission(player.UserIDString, plugin.permView))
                {
                    player.SetInfo("BikeView", (((int)Time.realtimeSinceStartup + 2)).ToString());
                    if (plugin.TPView.Contains(player.userID))
                    {
                        plugin.ToggleThirdPerson(player, false);
                        plugin.TPView.Remove(player.userID);
                    }
                    else
                    {
                        plugin.ToggleThirdPerson(player, true);
                        plugin.TPView.Add(player.userID);
                    }
                }
                if (___carPhysics.IsGrounded() && player.serverInput.IsDown(BUTTON.RELOAD) && player.GetInfoInt("BikeJump", 0) <= Time.realtimeSinceStartup && plugin.permission.UserHasPermission(player.UserIDString, plugin.permUse))
                {
                    bool isKinematic = __instance.rigidBody.isKinematic;
                    float jumpforce = plugin.permission.UserHasPermission(player.UserIDString, plugin.permSuper) ? plugin.config.SuperJumpForce : plugin.config.JumpForce;
                    __instance.rigidBody.isKinematic = false;
                    player.SetInfo("BikeJump", (((int)Time.realtimeSinceStartup + plugin.config.JumpCoolDown)).ToString());
                    __instance.rigidBody.AddForce(__instance.transform.up * __instance.rigidBody.mass * jumpforce, ForceMode.Impulse);
                    __instance.rigidBody.isKinematic = isKinematic;
                    if (!string.IsNullOrEmpty(plugin.config.JumpFX)) { Effect.server.Run(plugin.config.JumpFX, player.transform.position); }
                }
                else if (plugin.config.motorBikeSprint && __instance.poweredBy == Bike.PoweredBy.Fuel && player.serverInput.IsDown(BUTTON.SPRINT)) { __instance.SetFlag(BaseEntity.Flags.Reserved9, true, false, false); }
            }
        }

        [AutoPatch]
        [HarmonyPatch(typeof(Bike), "DoCollisionDamage", typeof(BaseEntity), typeof(float))]
        internal class Bike_DoCollisionDamage
        {
            [HarmonyPrefix]
            static bool HarmonyPrefix() { return !plugin.config.NoBikeDamage; }
        }
        #endregion

        #region Methods
        private void TPBike(Bike bike, BasePlayer player)
        {
            if (bike.AnyMounted())
            {
                player.ChatMessage("Can't Teleport Bike While Its Mounted!");
                return;
            }
            if (player.IsBuildingBlocked())
            {
                player.ChatMessage("You Are Building Blocked!");
                return;
            }
            if (player.transform.position.y < -5)
            {
                player.ChatMessage("You Can't Be Below Ground!");
                return;
            }
            Vector3 pos = player.eyes.transform.position + (player.eyes.BodyForward() * 1.7f);
            float terrainheight = TerrainMeta.HeightMap.GetHeight(pos);
            if (pos.y < terrainheight) { pos.y = terrainheight + 0.2f; }
            string Colliders = CheckColliders(pos, 1.7f);
            if (!string.IsNullOrEmpty(Colliders))
            {
                player.ChatMessage("Bike Teleport Blocked By Prefab: " + Colliders);
                return;
            }
            if (PlayersNearby(player, pos, 1.8f))
            {
                player.ChatMessage("Bike Teleport Blocked By Player");
                return;
            }
            player.ChatMessage("Teleported Bike To You");
            bike.transform.position = pos;
            bike.SendNetworkUpdate();
        }

        public bool PlayersNearby(BasePlayer owner, Vector3 pos, float distance)
        {
            List<BasePlayer> list = Facepunch.Pool.Get<List<BasePlayer>>();
            Vis.Entities<BasePlayer>(pos, distance, list, 131072, QueryTriggerInteraction.Collide);
            bool flag = false;
            foreach (BasePlayer basePlayer in list)
            {
                if (basePlayer.IsAlive())
                {
                    if (basePlayer == owner) { continue; }
                    flag = true;
                    break;
                }
            }
            Facepunch.Pool.FreeUnmanaged(ref list);
            return flag;
        }

        private string CheckColliders(Vector3 position, float distance)
        {
            foreach (Collider col in Physics.OverlapSphere(position, distance, LayerMask.GetMask("Construction", "Default", "Deployed", "Resource", "Terrain", "Water", "World", "Tree", "Vehicle_Large", "Vehicle_World", "Vehicle_Detailed", "Physics_Debris", "Clutter", "TransparentFX", "Harvestable")))
            {
                if (col.gameObject.name != "Terrain") { return col.gameObject.name; }
            }
            return string.Empty;
        }

        private void ToggleThirdPerson(BasePlayer player, bool enable)
        {
            //Check for admin toggle plugins
            if (!player.IsAdmin && !player.IsDeveloper && player.IsFlying) { return; }// BasePlayer => FinalizeTick => NoteAdminHack => Ban => Cheat Detected!
            bool _isadmin = player.IsAdmin; //Check if is admin
            if (_isadmin)
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                player.SendNetworkUpdateImmediate();
            }
            timer.Once(0.1f, () =>
            {
                if (!player.isMounted) { enable = false; }
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, enable);
                player.Command("camoffset", 0.0, 4.5f, 0.0);
                player.Command("camdist", config.ViewDistance);
                player.Command("camfov", 100);
                if (_isadmin)
                {
                    timer.Once(0.1f, () =>
                    {
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                        player.SendNetworkUpdateImmediate();
                    });
                }
                player.SendNetworkUpdateImmediate();
            });
        }

        private void ModBike(Bike bike, BasePlayer player, bool doMod = true)
        {
            ulong ID = bike.net.ID.Value;
            if (player != null && bike?.poweredBy == Bike.PoweredBy.Fuel)
            {
                if (permission.UserHasPermission(player.UserIDString, permUnlimitedFuel))
                {
                    bike.idleFuelPerSec = doMod ? 0 : (float)jumpBikes[ID].idleFuelPerSec;
                    bike.maxFuelPerSec = doMod ? 0 : (float)jumpBikes[ID].maxFuelPerSec;
                    StorageContainer fuelContainer = (bike?.GetFuelSystem() as EntityFuelSystem)?.GetFuelContainer();
                    if (fuelContainer != null)
                    {
                        if (doMod)
                        {
                            fuelContainer.inventory.AddItem(fuelContainer.allowedItem, 1);
                            fuelContainer.SetFlag(BaseEntity.Flags.Locked, true);
                        }
                        else
                        {
                            if (fuelContainer.HasFlag(BaseEntity.Flags.Locked))
                            {
                                fuelContainer.inventory.Clear();
                                fuelContainer.SetFlag(BaseEntity.Flags.Locked, false);
                            }
                        }
                    }
                    fuelContainer.SendNetworkUpdateImmediate();
                }
            }
            switch (bike.prefabID)
            {
                case 248647596: //motorbike
                    bike.airControlTorquePower = doMod ? config.airControlTorquePowerMotorBike : (float)jumpBikes[ID].airControlTorquePower;
                    bike.sprintTime = doMod ? config.sprintTimeMotorBike : (float)jumpBikes[ID].sprintTime;
                    bike.sprintRegenTime = doMod ? config.sprintRegenTimeMotorBike : (float)jumpBikes[ID].sprintRegenTime;
                    bike.sprintBoostPercent = doMod ? config.sprintBoostPercentMotorBike : (float)jumpBikes[ID].sprintBoostPercent;
                    bike.playerDamageThreshold = doMod ? config.playerDamageThresholdMotorBike : (float)jumpBikes[ID].playerDamageThreshold;
                    bike.playerDeathThreshold = doMod ? config.playerDeathThresholdMotorBike : (float)jumpBikes[ID].playerDeathThreshold;
                    break;
                case 573812: //motorbike_sidecar
                    bike.airControlTorquePower = doMod ? config.airControlTorquePowerMotorBike_Sidecar : (float)jumpBikes[ID].airControlTorquePower;
                    bike.sprintTime = doMod ? config.sprintTimeMotorBike_Sidecar : (float)jumpBikes[ID].sprintTime;
                    bike.sprintRegenTime = doMod ? config.sprintRegenTimeMotorBike_Sidecar : (float)jumpBikes[ID].sprintRegenTime;
                    bike.sprintBoostPercent = doMod ? config.sprintBoostPercentMotorBike_Sidecar : (float)jumpBikes[ID].sprintBoostPercent;
                    bike.playerDamageThreshold = doMod ? config.playerDamageThresholdMotorBike_Sidecar : (float)jumpBikes[ID].playerDamageThreshold;
                    bike.playerDeathThreshold = doMod ? config.playerDeathThresholdMotorBike_Sidecar : (float)jumpBikes[ID].playerDeathThreshold;
                    break;
                case 226383098: //pedalbike
                    bike.airControlTorquePower = doMod ? config.airControlTorquePowerPedalBike : (float)jumpBikes[ID].airControlTorquePower;
                    bike.sprintTime = doMod ? config.sprintTimePedalBike : (float)jumpBikes[ID].sprintTime;
                    bike.sprintRegenTime = doMod ? config.sprintRegenTimePedalBike : (float)jumpBikes[ID].sprintRegenTime;
                    bike.sprintBoostPercent = doMod ? config.sprintBoostPercentPedalBike : (float)jumpBikes[ID].sprintBoostPercent;
                    bike.playerDamageThreshold = doMod ? config.playerDamageThresholdPedalBike : (float)jumpBikes[ID].playerDamageThreshold;
                    bike.playerDeathThreshold = doMod ? config.playerDeathThresholdPedalBike : (float)jumpBikes[ID].playerDeathThreshold;
                    break;
                case 383359455: //pedaltrike
                    bike.airControlTorquePower = doMod ? config.airControlTorquePowerPedalTrike : (float)jumpBikes[ID].airControlTorquePower;
                    bike.sprintTime = doMod ? config.sprintTimePedalTrike : (float)jumpBikes[ID].sprintTime;
                    bike.sprintRegenTime = doMod ? config.sprintRegenTimePedalTrike : (float)jumpBikes[ID].sprintRegenTime;
                    bike.sprintBoostPercent = doMod ? config.sprintBoostPercentPedalTrike : (float)jumpBikes[ID].sprintBoostPercent;
                    bike.playerDamageThreshold = doMod ? config.playerDamageThresholdPedalTrike : (float)jumpBikes[ID].playerDamageThreshold;
                    bike.playerDeathThreshold = doMod ? config.playerDeathThresholdPedalTrike : (float)jumpBikes[ID].playerDeathThreshold;
                    break;
            }
            bike.SendNetworkUpdateImmediate();
        }
        #endregion
    }
}