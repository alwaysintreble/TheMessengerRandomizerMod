﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MessengerRando.GameOverrideManagers;
using MessengerRando.RO;
using MessengerRando.Utils;
using Newtonsoft.Json;
using UnityEngine;

namespace MessengerRando.Archipelago
{
    public class ArchipelagoData
    {
        public string Uri;
        public int Port;
        public string SlotName;
        public string Password;
        public int Index = 0;
        public string SeedName;
        public Dictionary<string, object> SlotData;
        public static bool DeathLink = false;
        public int PowerSealsCollected;
        public readonly List<string> DefeatedBosses = new List<string>();
        public bool RuxxCutscene;
        public List<long> CheckedLocations = new List<long>();
        /*
        public float StartTime;
        public float PlayTime
        {
            get
            {
                if (StartTime > 0)
                {
                    return DateTime.UtcNow.Millisecond - StartTime;
                }
                return 0;
            }
        }
        public float FinishTime;
        */
        private Dictionary<LocationRO, RandoItemRO> _locationToItemMapping;
        public SeedRO MessengerSeed
        {
            get
            {
                var slot = Manager<SaveManager>.Instance.GetSaveGameSlotIndex();
                return new SeedRO(slot, SeedType.Archipelago, 0, GameSettings, new List<RandoItemRO>(), "");
            }
        }

        public Dictionary<LocationRO, RandoItemRO> LocationToItemMapping
        {
            get
            {
                if (_locationToItemMapping != null) return _locationToItemMapping;
                var mapping = new Dictionary<LocationRO, RandoItemRO>();
                try
                {
                    var locations = new Dictionary<long, List<string>>();

                    if (SlotData.TryGetValue("locations", out var otherLocations))
                    {
                        locations = JsonConvert.DeserializeObject<Dictionary<long, List<string>>>(otherLocations.ToString());
                    }

                    foreach (long locationID in ItemsAndLocationsHandler.LocationsLookup.Values)
                    {
                        LocationRO location = ItemsAndLocationsHandler.LocationsLookup
                            .FirstOrDefault(x => x.Value == locationID).Key;
                        if (locations.TryGetValue(locationID, out var otherItemID))
                        {
                            RandoItemRO item =
                                new RandoItemRO(otherItemID[0], EItems.NONE, 1, otherItemID[1]);
                            mapping.Add(location, item);
                        }
                        else
                        {
                            if (RandomizerConstants.GetAdvancedRandoLocationList().Contains(location)) continue;
                            if (SettingValue.Advanced.Equals(GameSettings[SettingType.Difficulty]))
                                Console.WriteLine($"Couldn't find {location.PrettyLocationName} in slot data");
                            else
                            {
                                //this *should* add time shards to seal locations in basic logic seeds
                                mapping.Add(location, ItemsAndLocationsHandler.ItemsLookup[locationID]);
                            }

                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error: {e}");
                }
                _locationToItemMapping = mapping;
                return mapping;
            }
        }

        public Dictionary<SettingType, SettingValue> GameSettings
        {
            get
            {
                var mapping = new Dictionary<SettingType, SettingValue>();
                try
                {
                    if (SlotData.TryGetValue("settings", out var settings))
                    {
                        var genSettings = JsonConvert.DeserializeObject<Dictionary<string, string>>(settings.ToString());
                        foreach (string settingType in genSettings.Keys)
                        {
                            mapping.Add((SettingType)Enum.Parse(typeof(SettingType), settingType), (SettingValue)Enum.Parse(typeof(SettingValue), genSettings[settingType]));
                        }
                    }
                }
                catch(Exception e) { Console.WriteLine(e.ToString()); }
                return mapping;
            }
        }
        private UTF8Encoding encoding = new UTF8Encoding();

        public SeedRO StartNewSeed(int slot)
        {
            Console.WriteLine("Creating new seed data");
            //Locally placed items from AP get added to the slot data so add those to the mapping here so we can
            //reward our own items. Locations with items for other players will just be empty
            Index = 0;
            //if we aren't able to create a save file fail here
            return !UpdateSave()
                ? new SeedRO(slot, SeedType.None, 0, null, null, null)
                : MessengerSeed;
        }

        public bool UpdateSave()
        {
            RandomizerStateManager.Instance.CurrentLocationToItemMapping = LocationToItemMapping;
            RandomizerStateManager.Instance.CurrentLocationDialogtoRandomDialogMapping = DialogChanger.GenerateDialogMappingforItems();
            RandomizerStateManager.Instance.IsRandomizedFile = true;
            int slot = RandomizerStateManager.Instance.CurrentFileSlot;
            
            return SaveData(slot);
        }

        private bool SaveData(int slot)
        {
            if (ArchipelagoClient.HasConnected)
            {
                const string pattern = "MessengerSeed";
                
                var filePath = Application.persistentDataPath + $"ArchipelagoSlot{slot}.map";
                string output = JsonConvert.SerializeObject(this);
                int cutoffIndex = output.IndexOf(pattern);
                output = output.Substring(0, cutoffIndex-2) + "}";
                File.WriteAllText(filePath, output, encoding);
                return true;
            }
            Console.WriteLine("Attempted to save Archipelago data but hasn't connected.");
            return false;
        }

        public static bool LoadData(int slot)
        {
            Console.WriteLine($"Loading Archipelago data for slot {slot}");
            if (ArchipelagoClient.ServerData == null) ArchipelagoClient.ServerData = new ArchipelagoData();
            return ArchipelagoClient.ServerData.loadData(slot);
        }

        private bool loadData(int slot)
        {
            string path = $"{Application.persistentDataPath}ArchipelagoSlot{slot}.map";
            Console.WriteLine($"Loading data from {path}");
            ArchipelagoData tempServerData = null;
            if (File.Exists(path))
            {
                using (StreamReader reader = new StreamReader(path))
                {
                    tempServerData = JsonConvert.DeserializeObject<ArchipelagoData>(reader.ReadToEnd());
                }
            }
            if(tempServerData == null)
            {
                Console.WriteLine("Didn't find Archipelago data.");
                return false;
            }
            if (ArchipelagoClient.Authenticated)
            {
                Console.WriteLine($"Loaded seed name: {tempServerData.SeedName}");
                Console.WriteLine($"Connected seed name: {SeedName}");
                //we're already connected to an archipelago server so check if the file is valid
                if (tempServerData.SeedName.Equals(SeedName) && tempServerData.SlotName.Equals(SlotName))
                {
                    //We're continuing an existing multiworld so likely a port change. Save the new data
                    Index = tempServerData.Index;
                    CheckedLocations.AddRange(tempServerData.CheckedLocations);

                    return true;
                }
                //There was archipelago save data and it doesn't match our current connection so abort.
                ArchipelagoClient.Disconnect();
                ArchipelagoClient.HasConnected = false;
                return false;
            }
            try
            {
                //We aren't connected to an Archipelago server so attempt to use the found data
                Uri = tempServerData.Uri;
                Port = tempServerData.Port;
                SlotName = tempServerData.SlotName;
                Password = tempServerData.Password;
                Index = tempServerData.Index;
                SeedName = tempServerData.SeedName;
                SlotData = tempServerData.SlotData;
                PowerSealsCollected = tempServerData.PowerSealsCollected;
                CheckedLocations.AddRange(tempServerData.CheckedLocations);
                //Attempt to connect to the server and save the new data
                ArchipelagoClient.ConnectAsync();
                if (!ArchipelagoClient.Authenticated) ItemsAndLocationsHandler.Initialize();
                RandoBossManager.DefeatedBosses.AddRange(tempServerData.DefeatedBosses);
                return ArchipelagoClient.HasConnected = true; //Doing this here because of race conditions and the actual connection being threaded
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.ToString());
                return false; 
            }
        }

        public static void ClearData()
        {
            string filePath = Application.persistentDataPath + $"ArchipelagoSlot";
            for (int slot = 1; slot <= 3; slot++)
            {
                var currentPath = filePath + $"{slot}.map";
                if (File.Exists(currentPath)) { File.Delete(currentPath); }
            }
        }

    }
}