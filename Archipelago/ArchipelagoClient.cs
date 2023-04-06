﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Packets;
using MessengerRando.GameOverrideManagers;
using Mod.Courier.UI;
using Newtonsoft.Json;
using static Mod.Courier.UI.TextEntryButtonInfo;
using UnityEngine;

namespace MessengerRando.Archipelago
{
    public static class ArchipelagoClient
    {
        private const string ApVersion = "0.3.9";
        public static ArchipelagoData ServerData;

        private delegate void OnConnectAttempt(LoginResult result);
        public static bool Authenticated;
        public static bool HasConnected;

        public static bool DisplayAPMessages = true;
        public static bool DisplayStatus = true;

        public static ArchipelagoSession Session;
        public static DeathLinkInterface DeathLinkHandler;

        private static readonly List<string> MessageQueue = new List<string>();

        public static void ConnectAsync()
        {
            ThreadPool.QueueUserWorkItem(_ => Connect(OnConnected));
        }

        public static void ConnectAsync(SubMenuButtonInfo connectButton)
        {
            if (ServerData == null)
                ServerData = new ArchipelagoData();
            Console.WriteLine($"Connecting to {ServerData.Uri}:{ServerData.Port} as {ServerData.SlotName}");
            Connect(result => OnConnected(result, connectButton));
        }

        private static void OnConnected(LoginResult connectStats)
        {
            return;
        }

        private static void OnConnected(LoginResult connectResult, SubMenuButtonInfo connectButton)
        {
            TextEntryPopup successPopup = InitTextEntryPopup(connectButton.addedTo, string.Empty,
                entry => true, 0, null, CharsetFlags.Space);

            string outputText;
            if (connectResult.Successful)
                outputText = $"Successfully connected to {ServerData.Uri}:{ServerData.Port} as {ServerData.SlotName}!";
            else
            {
                outputText = $"Failed to connect to {ServerData.Uri}:{ServerData.Port} as {ServerData.SlotName}\n";
                foreach (var error in ((LoginFailure)connectResult).Errors)
                    outputText += error;
            }
            
            successPopup.Init(outputText);
            successPopup.gameObject.SetActive(true);
            // Object.Destroy(successPopup.transform.Find("BigFrame").Find("SymbolsGrid").gameObject);
            Console.WriteLine(outputText);
        }

        private static void Connect(OnConnectAttempt attempt)
        {
            if (Authenticated) return;
            if (ItemsAndLocationsHandler.ItemsLookup == null) ItemsAndLocationsHandler.Initialize();

            LoginResult result;

            Session = ArchipelagoSessionFactory.CreateSession(ServerData.Uri, ServerData.Port);
            Session.MessageLog.OnMessageReceived += OnMessageReceived;
            Session.Locations.CheckedLocationsUpdated += RemoteLocationChecked;
            Session.Socket.ErrorReceived += SessionErrorReceived;
            Session.Socket.SocketClosed += SessionSocketClosed;

            try
            {
                Console.WriteLine("Attempting Connection...");
                result = Session.TryConnectAndLogin(
                    "The Messenger",
                    ServerData.SlotName,
                    ItemsHandlingFlags.AllItems,
                    new Version(ApVersion),
                    null,
                    "",
                    ServerData.Password == "" ? null : ServerData.Password
                );
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.GetBaseException().Message}");
                result = new LoginFailure(e.GetBaseException().Message);
            }

            if (result.Successful)
            {
                var success = (LoginSuccessful)result;
                ServerData.SlotData = success.SlotData;
                ServerData.SeedName = Session.RoomState.Seed;
                Authenticated = true;

                if (ServerData.SlotData.TryGetValue("deathlink", out var deathLink))
                    ArchipelagoData.DeathLink = Convert.ToInt32(deathLink) == 1;
                else Console.WriteLine("Failed to get deathlink option");

                if (ServerData.SlotData.TryGetValue("goal", out var gameGoal))
                {
                    var goal = (string)gameGoal;
                    RandomizerStateManager.Instance.Goal = goal;
                    if (RandoPowerSealManager.Goals.Contains(goal))
                    {
                        if (ServerData.SlotData.TryGetValue("required_seals", out var requiredSeals))
                        {
                            RandomizerStateManager.Instance.PowerSealManager =
                                new RandoPowerSealManager(Convert.ToInt32(requiredSeals));
                        }
                    }

                    if (ServerData.SlotData.TryGetValue("music_box", out var doMusicBox))
                        RandomizerStateManager.Instance.SkipMusicBox = Convert.ToInt32(doMusicBox) == 0;
                    else Console.WriteLine("Failed to get music_box option");
                    
                }
                else Console.WriteLine("Failed to get goal option");

                if (ServerData.SlotData.TryGetValue("bosses", out var bosses))
                {
                    var bossMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(bosses.ToString());
                    Console.WriteLine("Bosses:");
                    foreach (var bossPair in bossMap)
                    {
                        Console.WriteLine($"{bossPair.Key}: {bossPair.Value}");
                    }
                    try
                    {
                        RandomizerStateManager.Instance.BossManager =
                            new RandoBossManager(bossMap);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                else Console.WriteLine("Failed to get bosses option");

                if (ServerData.SlotData.TryGetValue("settings", out var genSettings))
                {
                    var gameSettings = JsonConvert.DeserializeObject<Dictionary<string, string>>(genSettings.ToString());
                    if (gameSettings.TryGetValue("Mega Shards", out var shuffleShards))
                        if (Int32.TryParse(shuffleShards, out var shardsSetting) && shardsSetting == 1)
                            RandomizerStateManager.Instance.MegaShards = true;
                }

                DeathLinkHandler = new DeathLinkInterface();
                if (HasConnected)
                {
                    foreach (var location in ServerData.CheckedLocations.Where(location =>
                                 !Session.Locations.AllLocationsChecked.Contains(location)))
                        Session.Locations.CompleteLocationChecks(location);
                    ServerData.CheckedLocations = Session.Locations.AllLocationsChecked.ToList();
                    return;
                }

                ServerData.UpdateSave();
                HasConnected = true;
            }
            else
            {
                LoginFailure failure = (LoginFailure)result;
                string errorMessage = $"Failed to connect to {ServerData.Uri} as {ServerData.SlotName}:";
                errorMessage +=
                    failure.Errors.Aggregate(errorMessage, (current, error) => current + $"\n    {error}");
                errorMessage +=
                    failure.ErrorCodes.Aggregate(errorMessage, (current, error) => current + $"\n   {error}");

                Console.WriteLine($"Failed to connect: {errorMessage}");

                Authenticated = false;
                Disconnect();
            }

            attempt(result);
        }

        private static void OnMessageReceived(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            MessageQueue.Add(message.ToString());
        }

        private static void RemoteLocationChecked(ReadOnlyCollection<long> checkedLocations)
        {
            foreach (var checkedLocation in checkedLocations)
            {
                if (!ServerData.CheckedLocations.Contains(checkedLocation))
                    ServerData.CheckedLocations.Add(checkedLocation);
            }
        }

        private static void SessionErrorReceived(Exception e, string message)
        {
            Console.WriteLine(message);
            Console.WriteLine(e.GetBaseException().Message);
        }

        private static void SessionSocketClosed(string reason) 
        {
            Console.WriteLine($"Connection to Archipelago lost: {reason}");
            Disconnect();
        }

        public static void Disconnect()
        {
            Console.WriteLine("Disconnecting from server...");
            Session?.Socket.DisconnectAsync();
            Session = null;
            Authenticated = false;
        }

        public static void UpdateArchipelagoState()
        {
            Console.WriteLine("Updating Archipelago State");
            HasConnected = true;
            if (!Authenticated)
            {
                Console.WriteLine("Attempting to reconnect to Archipelago Server...");
                ThreadPool.QueueUserWorkItem(o => ConnectAsync());
                return;
            }
            if (ServerData.Index >= Session.Items.AllItemsReceived.Count) return;
            var currentItem = Session.Items.AllItemsReceived[Convert.ToInt32(ServerData.Index)];
            var currentItemId = currentItem.Item;
            ++ServerData.Index;
            ItemsAndLocationsHandler.Unlock(currentItemId);
            if (!currentItem.Player.Equals(Session.ConnectionInfo.Slot))
            {
                DialogSequence receivedItem = ScriptableObject.CreateInstance<DialogSequence>();
                receivedItem.dialogID = "RANDO_ITEM";
                receivedItem.name = Session.Items.GetItemName(currentItemId);
                receivedItem.choices = new List<DialogSequenceChoice>();
                AwardItemPopupParams receivedItemParams = new AwardItemPopupParams(receivedItem, true);
                Manager<UIManager>.Instance.ShowView<AwardItemPopup>(EScreenLayers.PROMPT, receivedItemParams);
            }
        }

        public static void UpdateClientStatus(ArchipelagoClientState newState)
        {
            if (newState == ArchipelagoClientState.ClientGoal) Session.DataStorage[Scope.Slot, "HasFinished"] = true;
            Console.WriteLine($"Updating client status to {newState}");
            var statusUpdatePacket = new StatusUpdatePacket { Status = newState };
            Session.Socket.SendPacket(statusUpdatePacket);
        }

        private static bool ClientFinished()
        {
            if (!Authenticated) return false;
            return Session.DataStorage[Scope.Slot, "HasFinished"].To<bool?>() == true;
        }

        public static bool CanRelease()
        {
            if (Authenticated)
            {
                Permissions releasePermission = Session.RoomState.ReleasePermissions;
                switch (releasePermission)
                {
                    case Permissions.Goal:
                        return ClientFinished();
                    case Permissions.Enabled:
                        return true;
                }
            }
            return false;
        }

        public static bool CanCollect()
        {
            if (Authenticated)
            {
                Permissions collectPermission = Session.RoomState.CollectPermissions;
                switch (collectPermission)
                {
                    case Permissions.Goal:
                        return ClientFinished();
                    case Permissions.Enabled:
                        return true;
                }
            }
            return false;
        }

        public static int GetHintCost()
        {
            var hintPercent = Session.RoomState.HintCostPercentage;
            if (hintPercent > 0)
            {
                var totalLocations = Session.Locations.AllLocations.Count;
                hintPercent = (int)Math.Round(totalLocations * (hintPercent * 0.01));
            }
            return hintPercent;
        }


        public static bool CanHint()
        {
            bool canHint = false;
            if (Authenticated)
            {
                canHint = GetHintCost() <= Session.RoomState.HintPoints;
            }
            return canHint;
        }

        public static string UpdateStatusText()
        {
            string text = string.Empty;
            if (DisplayStatus)
            {
                if (Authenticated)
                {
                    text = $"Connected to Archipelago server v{Session.RoomState.Version}";
                    var hintCost = GetHintCost();
                    if (hintCost > 0)
                    {
                        text += $"\nHint points available: {Session.RoomState.HintPoints}\nHint point cost: {hintCost}";
                    }
                    /*
                    TimeSpan t = new TimeSpan();
                    var playTime = ServerData.PlayTime;
                    if (ServerData.FinishTime > 0)
                    {
                        t = TimeSpan.FromMilliseconds(ServerData.FinishTime);
                    }
                    else if (playTime > 0)
                    {
                        t = TimeSpan.FromMilliseconds(playTime);
                    }
                    text += $"\nPlayTime: " + string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", t.Hours, t.Minutes, t.Seconds, t.Milliseconds);
                    */
                }
                else if (HasConnected)
                {
                    text = "Disconnected from Archipelago server.";
                }
            }            
            return text;
        }

        public static string UpdateMessagesText()
        {
            var text = string.Empty;
            if (MessageQueue.Count > 0)
            {
                if (DisplayAPMessages)
                    text = MessageQueue.First();
                MessageQueue.RemoveAt(0);
            }
            return text;
        }
    }
}