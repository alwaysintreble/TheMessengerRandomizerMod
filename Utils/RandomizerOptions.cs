﻿using System;
using System.Collections.Generic;

namespace MessengerRando.Utils
{
    public static class RandomizerOptions
    {
        // name
        public static string Name = "Ninja";
        public static bool OnNameEntry(string input)
        {
            Name = input;
            return true;
        }
        
        // seed
        public static string Seed = "";
        public static bool OnSeedEntry(string input)
        {
            Seed = input;
            return true;
        }
        
        // spoiler
        public static int SpoilerLevel = 3;
        private static readonly List<string> SpoilerText = new List<string>
        {
            "No Spoiler",
            "Spoiler without playthrough or paths",
            "Spoiler with playthrough",
            "Spoiler with playthrough and paths",
        };
        public static string GetSpoilerText()
        {
            return SpoilerText[SpoilerLevel];
        }
        public static void ChangeSpoiler()
        {
            SpoilerLevel = SpoilerLevel == 3 ? 0 : SpoilerLevel + 1;
        }
        
        // accessibility
        private static int accessibility;
        private static readonly List<string> AccessibilityText = new List<string>
        {
            "Locations Accessibility",
            "Items Accessibility",
            "Minimal Accessibility",
        };
        public static string GetAccessibilityText()
        {
            return AccessibilityText[accessibility];
        }
        public static void ChangeAccessibility()
        {
            accessibility = accessibility == 2 ? 0 : accessibility + 1;
        }
        
        // logic
        private static int logic;
        private static readonly List<string> LogicText = new List<string>
        {
            "Normal Logic",
            "Hard Logic",
            "Challenging Logic",
            "oob Logic",
        };
        public static string GetLogicText()
        {
            return LogicText[logic];
        }
        public static void ChangeLogic()
        {
            switch (logic)
            {
                case 3:
                    logic = 0;
                    break;
                case 1:  // TODO reimplement challenging and oob logic
                    logic = 0;
                    break;
                default:
                    logic += 1;
                    break;
            }
        }
        
        // shards
        public static bool Shards;

        // limited movement
        public static bool LimMovement;

        // early meditation
        public static bool EarlyMed;
        
        // available portals
        private static int availablePortals = 6;
        public static string GetAvailablePortalsText()
        {
            return $"Available Portals: {availablePortals}";
        }
        public static void ChangeAvailablePortals()
        {
            availablePortals = availablePortals == 6 ? 3 : availablePortals + 1;
        }
        
        // portal shuffle
        private static int portalShuffle;
        private static readonly List<string> PortalShuffleText = new List<string>
        {
            "No Portal Shuffle",
            "Shuffle Portals with Shops",
            "Shuffle Portals with Shops and Checkpoints",
            "Shuffle Portals Anywhere",
        };
        public static string GetPortalShuffleText()
        {
            return PortalShuffleText[portalShuffle];
        }
        public static void ChangePortalShuffle()
        {
            portalShuffle = portalShuffle == 3 ? 0 : portalShuffle + 1;
        }
        
        // goal
        public static bool Goal;
        
        // music box gauntlet
        public static bool MusicBox;
        
        // needed notes
        private static int notes = 6;
        public static string GetNotesText()
        {
            return $"Notes Needed for Music Box: {notes}";
        }
        public static void ChangeNotes()
        {
            notes = notes == 6 ? 1 : notes + 1;
        }
        
        // total seals
        public static int TotalSeals = 85;
        public static bool OnTotalSealsEntry(string input)
        {
            TotalSeals = Math.Min(int.Parse(input), 85);
            return true;
        }
        
        // required seals
        public static int RequiredSeals = 100;
        public static bool OnRequiredSealsEntry(string input)
        {
            RequiredSeals = Math.Min(int.Parse(input), 100);
            return true;
        }
        
        // shop price modifier
        public static int ShopPriceMod = 100;
        public static bool OnShopPriceModEntry(string input)
        {
            ShopPriceMod = Math.Min(int.Parse(input), 400);
            return true;
        }

        public static Dictionary<string, string> GetOptions()
        {
            return new Dictionary<string, string>
            {
                {"accessibility", accessibility.ToString()},
                {"logic_level", logic.ToString()},
                {"shuffle_shards", Shards ? "true" : "false"},
                {"limited_movement", LimMovement ? "true" : "false"},
                {"early_meditation", EarlyMed ? "true" : "false"},
                {"available_portals", availablePortals.ToString()},
                {"shuffle_portals", portalShuffle.ToString()},
                {"goal", Goal ? "1" : "0"},
                {"music_box", MusicBox ? "true" : "false"},
                {"notes_needed", notes.ToString()},
                {"total_seals", TotalSeals.ToString()},
                {"percent_seals_required", RequiredSeals.ToString()},
                {"shop_price", ShopPriceMod.ToString()},
            };
        }
    }
}