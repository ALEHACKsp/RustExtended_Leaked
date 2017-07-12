﻿namespace Magma
{
    using Magma.Events;
    using RustExtended;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using UnityEngine;

    public class Hooks
    {
        private static System.Collections.Generic.List<object> decayList = new System.Collections.Generic.List<object>();
        private static Hashtable talkerTimers = new Hashtable();

        public static  event BlueprintUseHandlerDelagate OnBlueprintUse;

        public static  event ChatHandlerDelegate OnChat;

        public static  event CommandHandlerDelegate OnCommand;

        public static  event ConsoleHandlerDelegate OnConsoleReceived;

        public static  event DoorOpenHandlerDelegate OnDoorUse;

        public static  event EntityDecayDelegate OnEntityDecay;

        public static  event EntityDeployedDelegate OnEntityDeployed;

        public static  event EntityHurtDelegate OnEntityHurt;

        public static  event ItemsDatablocksLoaded OnItemsLoaded;

        public static  event HurtHandlerDelegate OnNPCHurt;

        public static  event KillHandlerDelegate OnNPCKilled;

        public static  event ConnectionHandlerDelegate OnPlayerConnected;

        public static  event DisconnectionHandlerDelegate OnPlayerDisconnected;

        public static  event PlayerGatheringHandlerDelegate OnPlayerGathering;

        public static  event HurtHandlerDelegate OnPlayerHurt;

        public static  event KillHandlerDelegate OnPlayerKilled;

        public static  event PlayerSpawnHandlerDelegate OnPlayerSpawned;

        public static  event PlayerSpawnHandlerDelegate OnPlayerSpawning;

        public static  event PluginInitHandlerDelegate OnPluginInit;

        public static  event ServerInitDelegate OnServerInit;

        public static  event ServerShutdownDelegate OnServerShutdown;

        public static  event LootTablesLoaded OnTablesLoaded;

        public static bool BlueprintUse(IBlueprintItem item, BlueprintDataBlock bdb)
        {
            Magma.Player player = Magma.Player.FindByPlayerClient(item.controllable.playerClient);
            if (player != null)
            {
                BPUseEvent ae = new BPUseEvent(bdb);
                if (OnBlueprintUse != null)
                {
                    OnBlueprintUse(player, ae);
                }
                if (!ae.Cancel)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool ChatReceived(ref ConsoleSystem.Arg arg, ref string chatText)
        {
            if (OnChat != null)
            {
                ChatString text = new ChatString(chatText);
                OnChat(Magma.Player.FindByPlayerClient(arg.argUser.playerClient), ref text);
                chatText = text.NewText;
            }
            return (chatText != "");
        }

        public static bool CheckOwner(DeployableObject obj, Controllable controllable)
        {
            DoorEvent de = new DoorEvent(new Entity(obj));
            if (obj.ownerID == controllable.playerClient.userID)
            {
                de.Open = true;
            }
            if ((obj.GetComponent<BasicDoor>() != null) && (OnDoorUse != null))
            {
                OnDoorUse(Magma.Player.FindByPlayerClient(controllable.playerClient), de);
            }
            return de.Open;
        }

        public static bool ConsoleReceived(ref ConsoleSystem.Arg a)
        {
            if (((a.argUser == null) && (a.Class == "magmaweb")) && (a.Function == "handshake"))
            {
                a.ReplyWith("All Good !");
                return true;
            }
            bool external = a.argUser == null;
            if (OnConsoleReceived != null)
            {
                OnConsoleReceived(ref a, external);
            }
            if ((a.Class == "magma") && (a.Function.ToLower() == "reload"))
            {
                if ((a.argUser != null) && a.argUser.admin)
                {
                    PluginEngine.GetPluginEngine().ReloadPlugins(Magma.Player.FindByPlayerClient(a.argUser.playerClient));
                    a.ReplyWith("Magma: Reloaded");
                }
                else if (external)
                {
                    PluginEngine.GetPluginEngine().ReloadPlugins(null);
                    a.ReplyWith("Magma: Reloaded");
                }
            }
            if ((a.Reply != null) && (a.Reply != ""))
            {
                a.ReplyWith("Magma: " + a.Class + "." + a.Function + " was executed!");
                return true;
            }
            return false;
        }

        public static float EntityDecay(object entity, float dmg)
        {
            DecayEvent de = new DecayEvent(new Entity(entity), ref dmg);
            if (OnEntityDecay != null)
            {
                OnEntityDecay(de);
            }
            if (decayList.Contains(entity))
            {
                decayList.Remove(entity);
            }
            decayList.Add(entity);
            return de.DamageAmount;
        }

        public static void EntityDeployed(object entity)
        {
            Entity e = new Entity(entity);
            Magma.Player creator = e.Creator;
            if (OnEntityDeployed != null)
            {
                OnEntityDeployed(creator, e);
            }
        }

        public static void EntityHurt(object entity, ref DamageEvent e)
        {
            try
            {
                HurtEvent he = new HurtEvent(ref e, new Entity(entity));
                if (decayList.Contains(entity))
                {
                    he.IsDecay = true;
                }
                if (he.Entity.IsStructure() && !he.IsDecay)
                {
                    StructureComponent component = entity as StructureComponent;
                    if ((component.IsType(StructureComponent.StructureComponentType.Ceiling) || component.IsType(StructureComponent.StructureComponentType.Foundation)) || component.IsType(StructureComponent.StructureComponentType.Pillar))
                    {
                        he.DamageAmount = 0f;
                    }
                }
                TakeDamage takeDamage = he.Entity.GetTakeDamage();
                takeDamage.health += he.DamageAmount;
                if (OnEntityHurt != null)
                {
                    OnEntityHurt(he);
                }
                Zone3D zoned = Zone3D.GlobalContains(he.Entity);
                if (((zoned == null) || !zoned.Protected) && ((he.Entity.GetTakeDamage().health - he.DamageAmount) > 0f))
                {
                    TakeDamage damage3 = he.Entity.GetTakeDamage();
                    damage3.health -= he.DamageAmount;
                }
            }
            catch (Exception exception)
            {
                Helper.LogError(exception.ToString(), true);
            }
        }

        public static void handleCommand(ref ConsoleSystem.Arg arg, string cmd, string[] args)
        {
            if (OnCommand != null)
            {
                OnCommand(Magma.Player.FindByPlayerClient(arg.argUser.playerClient), cmd, args);
            }
        }

        public static ItemDataBlock[] ItemsLoaded(System.Collections.Generic.List<ItemDataBlock> items, Dictionary<string, int> stringDB, Dictionary<int, int> idDB)
        {
            ItemsBlocks blocks = new ItemsBlocks(items);
            if (OnItemsLoaded != null)
            {
                OnItemsLoaded(blocks);
            }
            int num = 0;
            foreach (ItemDataBlock block in blocks)
            {
                stringDB.Add(block.name, num);
                idDB.Add(block.uniqueID, num);
                num++;
            }
            Magma.Server.GetServer().Items = blocks;
            return blocks.ToArray();
        }

        public static void NPCHurt(ref DamageEvent e)
        {
            try
            {
                HurtEvent he = new HurtEvent(ref e);
                if ((he.Victim as NPC).Health > 0f)
                {
                    NPC victim = he.Victim as NPC;
                    victim.Health += he.DamageAmount;
                    if (OnNPCHurt != null)
                    {
                        OnNPCHurt(he);
                    }
                    if (((he.Victim as NPC).Health - he.DamageAmount) <= 0f)
                    {
                        (he.Victim as NPC).Kill();
                    }
                    else
                    {
                        NPC npc2 = he.Victim as NPC;
                        npc2.Health -= he.DamageAmount;
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
            }
        }

        public static void NPCKilled(ref DamageEvent e)
        {
            try
            {
                DeathEvent de = new DeathEvent(ref e);
                if (OnNPCKilled != null)
                {
                    OnNPCKilled(de);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
            }
        }

        public static bool PlayerConnect(NetUser user)
        {
            Magma.Player item = new Magma.Player(user.playerClient);
            Magma.Server.GetServer().Players.Add(item);
            bool connected = user.connected;
            if (OnPlayerConnected != null)
            {
                OnPlayerConnected(item);
            }
            return connected;
        }

        public static void PlayerDisconnect(NetUser user)
        {
            Magma.Player item = Magma.Player.FindByPlayerClient(user.playerClient);
            if (item != null)
            {
                Magma.Server.GetServer().Players.Remove(item);
            }
            if (OnPlayerDisconnected != null)
            {
                OnPlayerDisconnected(item);
            }
        }

        public static void PlayerGather(Inventory rec, ResourceTarget rt, ResourceGivePair rg, ref int amount)
        {
            Magma.Player player = Magma.Player.FindByNetworkPlayer(rec.networkView.owner);
            GatherEvent ge = new GatherEvent(rt, rg, amount);
            if (OnPlayerGathering != null)
            {
                OnPlayerGathering(player, ge);
            }
            amount = ge.Quantity;
            if (!ge.Override)
            {
                amount = Mathf.Min(amount, rg.AmountLeft());
            }
            rg._resourceItemDatablock = ge.Item;
            rg.ResourceItemName = ge.Item;
        }

        public static void PlayerGatherWood(IMeleeWeaponItem rec, ResourceTarget rt, ref ItemDataBlock db, ref int amount, ref string name)
        {
            Magma.Player player = Magma.Player.FindByNetworkPlayer(rec.inventory.networkView.owner);
            GatherEvent ge = new GatherEvent(rt, db, amount) {
                Item = "Wood"
            };
            if (OnPlayerGathering != null)
            {
                OnPlayerGathering(player, ge);
            }
            db = Magma.Server.GetServer().Items.Find(ge.Item);
            amount = ge.Quantity;
            name = ge.Item;
        }

        public static void PlayerHurt(ref DamageEvent e)
        {
            HurtEvent he = new HurtEvent(ref e);
            if (!(he.Attacker is NPC) && !(he.Victim is NPC))
            {
                Magma.Player attacker = he.Attacker as Magma.Player;
                Magma.Player victim = he.Victim as Magma.Player;
                Zone3D zoned = Zone3D.GlobalContains(attacker);
                if ((zoned != null) && !zoned.PVP)
                {
                    attacker.Message("You are in a PVP restricted area.");
                    he.DamageAmount = 0f;
                    e = he.DamageEvent;
                    return;
                }
                zoned = Zone3D.GlobalContains(victim);
                if ((zoned != null) && !zoned.PVP)
                {
                    attacker.Message(victim.Name + " is in a PVP restricted area.");
                    he.DamageAmount = 0f;
                    e = he.DamageEvent;
                    return;
                }
            }
            if (OnPlayerHurt != null)
            {
                OnPlayerHurt(he);
            }
            e = he.DamageEvent;
        }

        public static bool PlayerKilled(ref DamageEvent de)
        {
            try
            {
                DeathEvent event2 = new DeathEvent(ref de);
                if (OnPlayerKilled != null)
                {
                    OnPlayerKilled(event2);
                }
                return event2.DropItems;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
                return true;
            }
        }

        public static void PlayerSpawned(PlayerClient pc, Vector3 pos, bool camp)
        {
            Magma.Player player = Magma.Player.FindByPlayerClient(pc);
            SpawnEvent se = new SpawnEvent(pos, camp);
            if ((OnPlayerSpawned != null) && (player != null))
            {
                OnPlayerSpawned(player, se);
            }
        }

        public static Vector3 PlayerSpawning(PlayerClient pc, Vector3 pos, bool camp)
        {
            Magma.Player player = Magma.Player.FindByPlayerClient(pc);
            SpawnEvent se = new SpawnEvent(pos, camp);
            if ((OnPlayerSpawning != null) && (player != null))
            {
                OnPlayerSpawning(player, se);
            }
            return new Vector3(se.X, se.Y, se.Z);
        }

        public static void PluginInit()
        {
            if (OnPluginInit != null)
            {
                OnPluginInit();
            }
        }

        public static void ResetHooks()
        {
            OnPluginInit = delegate {
            };
            OnChat = delegate (Magma.Player param0, ref ChatString param1) {
            };
            OnCommand = delegate (Magma.Player param0, string param1, string[] param2) {
            };
            OnPlayerConnected = delegate (Magma.Player param0) {
            };
            OnPlayerDisconnected = delegate (Magma.Player param0) {
            };
            OnNPCKilled = delegate (DeathEvent param0) {
            };
            OnNPCHurt = delegate (HurtEvent param0) {
            };
            OnPlayerKilled = delegate (DeathEvent param0) {
            };
            OnPlayerHurt = delegate (HurtEvent param0) {
            };
            OnPlayerSpawned = delegate (Magma.Player param0, SpawnEvent param1) {
            };
            OnPlayerSpawning = delegate (Magma.Player param0, SpawnEvent param1) {
            };
            OnPlayerGathering = delegate (Magma.Player param0, GatherEvent param1) {
            };
            OnEntityHurt = delegate (HurtEvent param0) {
            };
            OnEntityDecay = delegate (DecayEvent param0) {
            };
            OnEntityDeployed = delegate (Magma.Player param0, Entity param1) {
            };
            OnConsoleReceived = delegate (ref ConsoleSystem.Arg param0, bool param1) {
            };
            OnBlueprintUse = delegate (Magma.Player param0, BPUseEvent param1) {
            };
            OnDoorUse = delegate (Magma.Player param0, DoorEvent param1) {
            };
            OnTablesLoaded = delegate (Dictionary<string, LootSpawnList> param0) {
            };
            OnItemsLoaded = delegate (ItemsBlocks param0) {
            };
            OnServerInit = delegate {
            };
            OnServerShutdown = delegate {
            };
            foreach (Magma.Player player in Magma.Server.GetServer().Players)
            {
                player.FixInventoryRef();
            }
        }

        public static void ServerShutdown()
        {
            if (OnServerShutdown != null)
            {
                OnServerShutdown();
            }
            DataStore.GetInstance().Save();
        }

        public static void ServerStarted()
        {
            DataStore.GetInstance().Load();
            if (OnServerInit != null)
            {
                OnServerInit();
            }
        }

        public static Dictionary<string, LootSpawnList> TablesLoaded(Dictionary<string, LootSpawnList> lists)
        {
            if (OnTablesLoaded != null)
            {
                OnTablesLoaded(lists);
            }
            return lists;
        }

        public delegate void BlueprintUseHandlerDelagate(Magma.Player player, BPUseEvent ae);

        public delegate void ChatHandlerDelegate(Magma.Player player, ref ChatString text);

        public delegate void CommandHandlerDelegate(Magma.Player player, string text, string[] args);

        public delegate void ConnectionHandlerDelegate(Magma.Player player);

        public delegate void ConsoleHandlerDelegate(ref ConsoleSystem.Arg arg, bool external);

        public delegate void DisconnectionHandlerDelegate(Magma.Player player);

        public delegate void DoorOpenHandlerDelegate(Magma.Player p, DoorEvent de);

        public delegate void EntityDecayDelegate(DecayEvent de);

        public delegate void EntityDeployedDelegate(Magma.Player player, Entity e);

        public delegate void EntityHurtDelegate(HurtEvent he);

        public delegate void HurtHandlerDelegate(HurtEvent he);

        public delegate void ItemsDatablocksLoaded(ItemsBlocks items);

        public delegate void KillHandlerDelegate(DeathEvent de);

        public delegate void LootTablesLoaded(Dictionary<string, LootSpawnList> lists);

        public delegate void PlayerGatheringHandlerDelegate(Magma.Player player, GatherEvent ge);

        public delegate void PlayerSpawnHandlerDelegate(Magma.Player player, SpawnEvent se);

        public delegate void PluginInitHandlerDelegate();

        public delegate void ServerInitDelegate();

        public delegate void ServerShutdownDelegate();
    }
}

