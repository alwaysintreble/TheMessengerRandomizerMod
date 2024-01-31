﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Archipelago.MultiClient.Net.Models;
using MessengerRando.GameOverrideManagers;
using MessengerRando.Utils;
using Newtonsoft.Json;
using UnityEngine;

namespace MessengerRando.Archipelago
{
    public class ArchipelagoData
    {
        public string Uri = "archipelago.gg";
        public int Port = 38281;
        public string SlotName = "";
        public string Password = "";
        public int Index;
        public string SeedName = "Unknown";
        public Dictionary<string, object> SlotData;
        public static bool DeathLink = false;
        public int PowerSealsCollected;
        public List<string> DefeatedBosses;
        public List<long> CheckedLocations;
        public Dictionary<long, int> ReceivedItems;
        public Dictionary<long, NetworkItem> ScoutedLocations;
        public Dictionary<long, Dictionary<long, string>> LocationData;

        public void StartNewSeed()
        {
            Console.WriteLine("Creating new seed data");
            Index = 0;
            PowerSealsCollected = 0;
            DefeatedBosses = new List<string>();
            CheckedLocations = new List<long>();
            ReceivedItems = new Dictionary<long, int>();
        }
        
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static bool LoadData(int slot)
        {
            Console.WriteLine($"Loading Archipelago data for slot {slot}");
            if (ArchipelagoClient.ServerData == null) ArchipelagoClient.ServerData = new ArchipelagoData();
            return ArchipelagoClient.ServerData.loadData(slot);
        }

        // ReSharper disable once InconsistentNaming
        private bool loadData(int slot)
        {
            if (!RandomizerStateManager.Instance.APSave.TryGetValue(slot, out var tempServerData) ||
                tempServerData.SeedName == null || tempServerData.SeedName.Equals("Unknown"))
                return false;
            try
            {
                var i = 0;
                if (ArchipelagoClient.Authenticated)
                {
                    //we're already connected to an archipelago server so check if the file is valid
                    if (tempServerData.SeedName.Equals(SeedName) && tempServerData.SlotName.Equals(SlotName))
                    {
                        //We're continuing an existing multiworld so likely a port change. Save the new data
                        Index = tempServerData.Index;
                        PowerSealsCollected = tempServerData.PowerSealsCollected;
                        CheckedLocations = tempServerData.CheckedLocations ?? new List<long>();
                        RandoBossManager.DefeatedBosses = DefeatedBosses = 
                            tempServerData.DefeatedBosses ?? new List<string>();
                        ReceivedItems = tempServerData.ReceivedItems ?? new Dictionary<long, int>();
                        RandomizerStateManager.Instance.ScoutedLocations = 
                            ScoutedLocations = tempServerData.ScoutedLocations ?? new Dictionary<long, NetworkItem>();

                        ThreadPool.QueueUserWorkItem(o =>
                            ArchipelagoClient.Session.Locations.CompleteLocationChecksAsync(null,
                                CheckedLocations.ToArray()));
                        while (ArchipelagoClient.ItemQueue.Count > 0 && i < Index)
                        {
                            i += 1;
                            ArchipelagoClient.ItemQueue.Dequeue();
                        }
                        return true;
                    }
                    //There was archipelago save data and it doesn't match our current connection so abort.
                    ArchipelagoClient.Disconnect();
                    return ArchipelagoClient.HasConnected = false;
                }
                //We aren't connected to an Archipelago server so attempt to use the found data
                Uri = tempServerData.Uri;
                Port = tempServerData.Port;
                SlotName = tempServerData.SlotName;
                Password = tempServerData.Password;
                SeedName = tempServerData.SeedName;
                Index = tempServerData.Index;
                PowerSealsCollected = tempServerData.PowerSealsCollected;
                CheckedLocations = tempServerData.CheckedLocations ?? new List<long>();
                RandoBossManager.DefeatedBosses = DefeatedBosses = tempServerData.DefeatedBosses ?? new List<string>();
                ReceivedItems = tempServerData.ReceivedItems ?? new Dictionary<long, int>();
                RandomizerStateManager.Instance.ScoutedLocations = 
                    ScoutedLocations = tempServerData.ScoutedLocations ?? new Dictionary<long, NetworkItem>();
                SlotData = tempServerData.SlotData;
                LocationData = tempServerData.LocationData ?? new Dictionary<long, Dictionary<long, string>>();

                //Attempt to connect to the server and save the new data
                Console.WriteLine("Rando save found!");
                if (LocationData != null && LocationData.Count > 0)
                {
                    Console.WriteLine("continuing offline seed");
                    RandomizerStateManager.InitializeSeed();
                    return ArchipelagoClient.HasConnected = ArchipelagoClient.offline = true;
                }
                ArchipelagoClient.Connect();
                while (ArchipelagoClient.ItemQueue.Count > 0 && i < Index)
                {
                    i += 1;
                    ArchipelagoClient.ItemQueue.Dequeue();
                }
                return ArchipelagoClient.HasConnected = true;
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.ToString());
                return false; 
            }
        }

        public static void ClearData()
        {
            for (int slot = 0; slot <= 3; slot++)
            {
                var filePath = Application.persistentDataPath + "ArchipelagoSlot{slot}.map";
                if (File.Exists(filePath)) { File.Delete(filePath); }
                var nextPath = Application.persistentDataPath + $"/ArchipelagoSlot{slot}.map";
                if (File.Exists(nextPath)) { File.Delete(nextPath); }
            }
        }
    }
}