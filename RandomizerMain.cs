﻿using System;
using System.Collections.Generic;
using System.IO;
using MessengerRando.Archipelago;
using MessengerRando.Overrides;
using MessengerRando.Utils;
using MessengerRando.RO;
using Mod.Courier;
using Mod.Courier.Module;
using Mod.Courier.UI;
using MonoMod.Cil;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Mod.Courier.UI.TextEntryButtonInfo;
using MessengerRando.Exceptions;
using TMPro;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using MessengerRando.GameOverrideManagers;

namespace MessengerRando 
{
    /// <summary>
    /// Where it all begins! This class defines and injects all the necessary for the mod.
    /// </summary>
    public class RandomizerMain : CourierModule
    {
        private const string RANDO_OPTION_KEY = "minous27RandoSeeds";
        private const int MAX_BEATABLE_SEED_ATTEMPTS = 1;

        private float updateTimer;
        private float updateTime = 3.0f;

        private RandomizerStateManager randoStateManager;
        private RandomizerSaveMethod randomizerSaveMethod;

        TextEntryButtonInfo loadRandomizerFileForFileSlotButton;
        TextEntryButtonInfo resetRandoSaveFileButton;
  
        SubMenuButtonInfo versionButton;
        SubMenuButtonInfo seedNumButton;

        SubMenuButtonInfo windmillShurikenToggleButton;
        SubMenuButtonInfo teleportToHqButton;
        SubMenuButtonInfo teleportToNinjaVillage;

        //Archipelago buttons
        SubMenuButtonInfo archipelagoHostButton;
        SubMenuButtonInfo archipelagoPortButton;
        SubMenuButtonInfo archipelagoNameButton;
        SubMenuButtonInfo archipelagoPassButton;
        SubMenuButtonInfo archipelagoConnectButton;
        SubMenuButtonInfo archipelagoReleaseButton;
        SubMenuButtonInfo archipelagoCollectButton;
        SubMenuButtonInfo archipelagoHintButton;
        SubMenuButtonInfo archipelagoToggleMessagesButton;
        SubMenuButtonInfo archipelagoStatusButton;
        SubMenuButtonInfo archipelagoDeathLinkButton;
        SubMenuButtonInfo archipelagoMessageTimerButton;

        private TextMeshProUGUI apTextDisplay8;
        private TextMeshProUGUI apTextDisplay16;
        private TextMeshProUGUI apMessagesDisplay8;
        private TextMeshProUGUI apMessagesDisplay16;

        //Set up save data
        public override Type ModuleSaveType => typeof(RandoSave);
        public RandoSave Save => (RandoSave)ModuleSave;

        public override void Load()
        {
            Console.WriteLine("Randomizer loading and ready to try things!");
          
            //Initialize the randomizer state manager
            RandomizerStateManager.Initialize();
            randoStateManager = RandomizerStateManager.Instance;

            //Set up save data utility
            randomizerSaveMethod = new RandomizerSaveMethod();

            //Add Randomizer Version button
            versionButton = Courier.UI.RegisterSubMenuModOptionButton(() => "Messenger AP Randomizer: v" + ItemRandomizerUtil.GetModVersion(), null);

            //Add current seed number button
            seedNumButton = Courier.UI.RegisterSubMenuModOptionButton(() => "Current seed number: " + GetCurrentSeedNum(), null);

            //Add load seed file button
            loadRandomizerFileForFileSlotButton = Courier.UI.RegisterTextEntryModOptionButton(() => "Load Randomizer File For File Slot", (entry) => OnEnterFileSlot(entry), 1, () => "Which save slot would you like to start a rando seed?(1/2/3)", () => "1", CharsetFlags.Number);

            //Add Reset rando mod button
            resetRandoSaveFileButton = Courier.UI.RegisterTextEntryModOptionButton(() => "Reset Randomizer File Slot", (entry) => OnRandoFileResetConfirmation(entry), 1, () => "Are you sure you wish to reset your save file for randomizer play?(y/n)", () => "n", CharsetFlags.Letter);

            //Add windmill shuriken toggle button
            windmillShurikenToggleButton = Courier.UI.RegisterSubMenuModOptionButton(() => Manager<ProgressionManager>.Instance.useWindmillShuriken ? "Active Regular Shurikens" : "Active Windmill Shurikens", OnToggleWindmillShuriken);

            //Add teleport to HQ button
            teleportToHqButton = Courier.UI.RegisterSubMenuModOptionButton(() => "Teleport to HQ", OnSelectTeleportToHq);

            //Add teleport to Ninja Village button
            teleportToNinjaVillage = Courier.UI.RegisterSubMenuModOptionButton(() => "Teleport to Ninja Village", OnSelectTeleportToNinjaVillage);

            //Add Archipelago host button
            archipelagoHostButton = Courier.UI.RegisterTextEntryModOptionButton(() => "Enter Archipelago Host Name", (entry) => OnSelectArchipelagoHost(entry), 30, () => "Enter the Archipelago host name. Use spaces for periods", () => "archipelago.gg");

            //Add Archipelago port button
            archipelagoPortButton = Courier.UI.RegisterTextEntryModOptionButton(() => "Enter Archipelago Port", (entry) => OnSelectArchipelagoPort(entry), 5, () => "Enter the port for the Archipelago session", () => "38281", CharsetFlags.Number);

            //Add archipelago name button
            archipelagoNameButton = Courier.UI.RegisterTextEntryModOptionButton(() => "Enter Archipelago Slot Name", (entry) => OnSelectArchipelagoName(entry), 16, () => "Enter player name:");

            //Add archipelago password button
            archipelagoPassButton = Courier.UI.RegisterTextEntryModOptionButton(() => "Enter Archipelago Password", (entry) => OnSelectArchipelagoPass(entry), 30, () => "Enter session password:");

            //Add Archipelago connection button
            archipelagoConnectButton = Courier.UI.RegisterSubMenuModOptionButton(() => "Connect to Archipelago", OnSelectArchipelagoConnect);

            //Add Archipelago release button
            archipelagoReleaseButton = Courier.UI.RegisterSubMenuModOptionButton(() => "Release remaining items", OnSelectArchipelagoRelease);

            //Add Archipelago collect button
            archipelagoCollectButton = Courier.UI.RegisterSubMenuModOptionButton(() => "Collect remaining items", OnSelectArchipelagoCollect);

            //Add Archipelago hint button
            archipelagoHintButton = Courier.UI.RegisterTextEntryModOptionButton(() => "Hint for an item", (entry) => OnSelectArchipelagoHint(entry), 30, () => "Enter item name:");

            //Add Archipelago status button
            archipelagoStatusButton = Courier.UI.RegisterSubMenuModOptionButton(() => ArchipelagoClient.DisplayStatus ? "Hide status information" : "Display status information", OnToggleAPStatus);
            
            //Add Archipelago message button
            archipelagoToggleMessagesButton = Courier.UI.RegisterSubMenuModOptionButton(() => ArchipelagoClient.DisplayAPMessages ? "Hide server messages" : "Display server messages", OnToggleAPMessages);
            
            //Add Archipelago message display timer button
            archipelagoMessageTimerButton = Courier.UI.RegisterTextEntryModOptionButton(() => "AP Message Display Time", entry => OnSelectMessageTimer(entry), 1, () => "Enter amount of time to display Archipelago messages, in seconds", () => updateTime.ToString(), CharsetFlags.Number);

            //Add Archipelago death link button
            archipelagoDeathLinkButton = Courier.UI.RegisterSubMenuModOptionButton(() => ArchipelagoData.DeathLink ? "Disable Death Link" : "Enable Death Link", OnToggleDeathLink);

            //Plug in my code :3
            On.InventoryManager.AddItem += InventoryManager_AddItem;
            On.InventoryManager.GetItemQuantity += InventoryManager_GetItemQuantity;
            On.ProgressionManager.SetChallengeRoomAsCompleted += ProgressionManager_SetChallengeRoomAsCompleted;
            On.HasItem.IsTrue += HasItem_IsTrue;
            On.AwardNoteCutscene.ShouldPlay += AwardNoteCutscene_ShouldPlay;
            On.CutsceneHasPlayed.IsTrue += CutsceneHasPlayed_IsTrue;
            On.SaveGameSelectionScreen.OnLoadGame += SaveGameSelectionScreen_OnLoadGame;
            On.SaveGameSelectionScreen.OnNewGame += SaveGameSelectionScreen_OnNewGame;
            On.BackToTitleScreen.GoBackToTitleScreen += PauseScreen_OnQuitToTitle;
            On.NecrophobicWorkerCutscene.Play += NecrophobicWorkerCutscene_Play;
            IL.RuxxtinNoteAndAwardAmuletCutscene.Play += RuxxtinNoteAndAwardAmuletCutscene_Play;
            On.DialogCutscene.Play += DialogCutscene_Play;
            On.CatacombLevelInitializer.OnBeforeInitDone += CatacombLevelInitializer_OnBeforeInitDone;
            On.DialogManager.LoadDialogs_ELanguage += DialogChanger.LoadDialogs_Elanguage;
            On.UpgradeButtonData.IsStoryUnlocked += UpgradeButtonData_IsStoryUnlocked;
            // boss management
            On.ProgressionManager.HasDefeatedBoss +=
                (orig, self, bossName) => RandoBossManager.HasBossDefeated(bossName);
            On.ProgressionManager.HasEverDefeatedBoss +=
                (orig, self, bossName) => RandoBossManager.HasBossDefeated(bossName);
            On.ProgressionManager.SetBossAsDefeated +=
                (orig, self, bossName) => RandoBossManager.SetBossAsDefeated(bossName);
            // level teleporting etc management
            On.Level.ChangeRoom += RandoLevelManager.Level_ChangeRoom;
            //These functions let us override and manage power seals ourselves with 'fake' items
            On.ProgressionManager.TotalPowerSealCollected += ProgressionManager_TotalPowerSealCollected;
            On.ShopChestOpenCutscene.OnChestOpened += (orig, self) =>
                RandomizerStateManager.Instance.PowerSealManager?.OnShopChestOpen(orig, self);
            On.ShopChestChangeShurikenCutscene.Play += (orig, self) =>
                RandomizerStateManager.Instance.PowerSealManager?.OnShopChestOpen(orig, self);
            //update loops for Archipelago
            Courier.Events.PlayerController.OnUpdate += PlayerController_OnUpdate;
            On.InGameHud.OnGUI += InGameHud_OnGUI;
            On.SaveManager.DoActualSaving += SaveManager_DoActualSave;
            On.Quarble.OnPlayerDied += Quarble_OnPlayerDied;
            //temp add
            #if DEBUG
            On.Cutscene.Play += Cutscene_Play;
            On.PhantomIntroCutscene.OnEnterRoom += PhantomIntro_OnEnterRoom; //this lets us skip the phantom fight
            On.UIManager.ShowView += UIManager_ShowView;
            On.MusicBox.SetNotesState += MusicBox_SetNotesState;
            On.PowerSeal.OnEnterRoom += PowerSeal_OnEnterRoom;
            On.LevelManager.LoadLevel += LevelManager_LoadLevel;
            On.LevelManager.OnLevelLoaded += LevelManager_onLevelLoaded;
            #endif
            On.MegaTimeShard.OnBreakDone += MegaTimeShard_OnBreakDone;
            On.DialogSequence.GetDialogList += DialogSequence_GetDialogList;
            On.LevelManager.EndLevelLoading += LevelManager_EndLevelLoading;
            
            Console.WriteLine("Randomizer finished loading!");
        }
        
        public override void Initialize()
        {
            //I only want the generate seed/enter seed mod options available when not in the game.
            loadRandomizerFileForFileSlotButton.IsEnabled = () => Manager<LevelManager>.Instance.GetCurrentLevelEnum() == ELevel.NONE && !ArchipelagoClient.Authenticated;
            resetRandoSaveFileButton.IsEnabled = () => Manager<LevelManager>.Instance.GetCurrentLevelEnum() == ELevel.NONE;
            //Also the AP buttons
            archipelagoHostButton.IsEnabled = () => Manager<LevelManager>.Instance.GetCurrentLevelEnum() == ELevel.NONE && !ArchipelagoClient.Authenticated;
            archipelagoPortButton.IsEnabled = () => Manager<LevelManager>.Instance.GetCurrentLevelEnum() == ELevel.NONE && !ArchipelagoClient.Authenticated;
            archipelagoNameButton.IsEnabled = () => Manager<LevelManager>.Instance.GetCurrentLevelEnum() == ELevel.NONE && !ArchipelagoClient.Authenticated;
            archipelagoPassButton.IsEnabled = () => Manager<LevelManager>.Instance.GetCurrentLevelEnum() == ELevel.NONE && !ArchipelagoClient.Authenticated;
            archipelagoConnectButton.IsEnabled = () => Manager<LevelManager>.Instance.GetCurrentLevelEnum() == ELevel.NONE && !ArchipelagoClient.Authenticated;
            //These AP buttons can exist in or out of game
            archipelagoReleaseButton.IsEnabled = () => ArchipelagoClient.CanRelease();
            archipelagoCollectButton.IsEnabled = () => ArchipelagoClient.CanCollect();
            archipelagoHintButton.IsEnabled = () => ArchipelagoClient.CanHint();
            archipelagoToggleMessagesButton.IsEnabled = () => ArchipelagoClient.Authenticated;
            archipelagoStatusButton.IsEnabled = () => ArchipelagoClient.Authenticated;
            archipelagoDeathLinkButton.IsEnabled = () => ArchipelagoClient.Authenticated;
            archipelagoMessageTimerButton.IsEnabled = () => ArchipelagoClient.DisplayStatus;

            //Options I only want working while actually in the game
            windmillShurikenToggleButton.IsEnabled = () => (Manager<LevelManager>.Instance.GetCurrentLevelEnum() != ELevel.NONE && Manager<InventoryManager>.Instance.GetItemQuantity(EItems.WINDMILL_SHURIKEN) > 0);
            teleportToHqButton.IsEnabled = () => (Manager<LevelManager>.Instance.GetCurrentLevelEnum() != ELevel.NONE && randoStateManager.IsSafeTeleportState());
            teleportToNinjaVillage.IsEnabled = () => (Manager<LevelManager>.Instance.GetCurrentLevelEnum() != ELevel.NONE && Manager<ProgressionManager>.Instance.HasCutscenePlayed("ElderAwardSeedCutscene") && randoStateManager.IsSafeTeleportState());
            seedNumButton.IsEnabled = () => (Manager<LevelManager>.Instance.GetCurrentLevelEnum() != ELevel.NONE);

            SceneManager.sceneLoaded += OnSceneLoadedRando;

            //Options always available
            versionButton.IsEnabled = () => true;
            
            //Save loading
            Debug.Log("Start loading seeds from save");
            randomizerSaveMethod.Load(Save.seedData);
            Debug.Log($"Save data after change: '{Save.seedData}'");
            Debug.Log("Finished loading seeds from save");
        }

        //temp function for seal research
        void PowerSeal_OnEnterRoom(On.PowerSeal.orig_OnEnterRoom orig, PowerSeal self, bool teleportedInRoom)
        {
            //just print out some info for me
            Console.WriteLine($"Entered power seal room: {Manager<Level>.Instance.GetRoomAtPosition(self.transform.position).roomKey}");
            orig(self, teleportedInRoom);
        }

        List<DialogInfo> DialogSequence_GetDialogList(On.DialogSequence.orig_GetDialogList orig, DialogSequence self)
        {
            Console.WriteLine($"Starting dialogue {self.dialogID}");
            //Using this function to add some of my own dialog stuff to the game.
            if (!randoStateManager.IsRandomizedFile) return orig(self);
            if (!new[] { "RANDO_ITEM", "ARCHIPELAGO_ITEM", "DEATH_LINK"}.Contains(self.dialogID))
                return orig(self);
            Console.WriteLine("Trying some rando dialog stuff.");
            List<DialogInfo> dialogInfoList = new List<DialogInfo>();
            DialogInfo dialog = new DialogInfo();
            switch (self.dialogID)
            {
                case "RANDO_ITEM":
                    Console.WriteLine($"Item is {self.name}");
                    dialog.text = $"You have received item: '{self.name}'";
                    break;
                case "ARCHIPELAGO_ITEM":
                    Console.WriteLine($"Item is {self.name}");
                    dialog.text = $"You have found {self.name}";
                    break;
                case "DEATH_LINK":
                    dialog.text = $"Deathlink: {self.name}";
                    break;
                default:
                    //dialog.text = "???";
                    break;
            }
                    
                
            dialogInfoList.Add(dialog);

            return dialogInfoList;

        }

        void InventoryManager_AddItem(On.InventoryManager.orig_AddItem orig, InventoryManager self, EItems itemId, int quantity)
        {

            LocationRO randoItemCheck;

            if (itemId != EItems.TIME_SHARD) //killing the timeshard noise in the logs
            {
                Console.WriteLine($"Called InventoryManager_AddItem method. Looking to give x{quantity} amount of item '{itemId}'.");
                if (quantity == ItemsAndLocationsHandler.APQuantity)
                {
                    //We received this item from the AP server so grant it
                    orig(self, itemId, 1);
                    return;
                }
                if (ArchipelagoClient.HasConnected && randoStateManager.IsLocationRandomized(itemId, out randoItemCheck))
                {
                    ItemsAndLocationsHandler.SendLocationCheck(randoItemCheck);
                    if (string.IsNullOrEmpty(randoStateManager.CurrentLocationToItemMapping[randoItemCheck].RecipientName))
                    {

                        //This isn't our item so we add it to rando state and exit, otherwise the existing rando code can resolve it fine
                        randoStateManager.GetSeedForFileSlot(randoStateManager.CurrentFileSlot).CollectedItems.Add(ArchipelagoClient.ServerData.LocationToItemMapping[randoItemCheck]);
                        return;
                    }
                    Console.WriteLine("Need to grant item");
                    Console.WriteLine($"{randoStateManager.IsRandomizedFile} | {!RandomizerStateManager.Instance.HasTempOverrideOnRandoItem(itemId)} | {randoStateManager.IsLocationRandomized(itemId, out randoItemCheck)}");
                }
            }

            //Wierd Ruxxtin logic stuff
            if(EItems.NONE.Equals(itemId))
            {
                Console.WriteLine("Looks like Ruxxtin has a timeshard.");
            }

            //Lets make sure that the item they are collecting is supposed to be randomized
            if (randoStateManager.IsRandomizedFile && !RandomizerStateManager.Instance.HasTempOverrideOnRandoItem(itemId) && randoStateManager.IsLocationRandomized(itemId, out randoItemCheck))
            {
                //Based on the item that is attempting to be added, determine what SHOULD be added instead
                RandoItemRO randoItemId = randoStateManager.CurrentLocationToItemMapping[randoItemCheck];
                
                Console.WriteLine($"Randomizer magic engage! Game wants item '{itemId}', giving it rando item '{randoItemId}' with a quantity of '{quantity}'");
                
                //If that item is the windmill shuriken, immediately activate it and the mod option
                if(EItems.WINDMILL_SHURIKEN.Equals(randoItemId.Item))
                {
                    OnToggleWindmillShuriken();
                }
                else if (EItems.TIME_SHARD.Equals(randoItemId.Item)) //Handle timeshards
                {
                    Manager<InventoryManager>.Instance.CollectTimeShard(quantity);
                    randoStateManager.GetSeedForFileSlot(randoStateManager.CurrentFileSlot).CollectedItems.Add(randoItemId);
                    return; //Collecting timeshards internally call add item so I dont need to do it again.
                }

                //Set the itemId to the new item
                itemId = randoItemId.Item;
                //Set this item to have been collected in the state manager
                randoStateManager.GetSeedForFileSlot(randoStateManager.CurrentFileSlot).CollectedItems.Add(randoItemId);

                //Save
                Save.seedData = randomizerSaveMethod.GenerateSaveData();
            }
            
            //Call original add with items
            orig(self, itemId, quantity);
            
        }

        public void SaveModData(RandoSave randoSave)
        {
            
        }
        
        void ProgressionManager_SetChallengeRoomAsCompleted(On.ProgressionManager.orig_SetChallengeRoomAsCompleted orig, ProgressionManager self, string roomKey)
        {
            Console.WriteLine($"Marking {roomKey} as completed.");
            //if this is a rando file, go ahead and give the item we expect to get
            if (randoStateManager.IsRandomizedFile)
            {
                // if (!randoStateManager.GetSeedForFileSlot(randoStateManager.CurrentFileSlot)
                //         .Settings[SettingType.Difficulty]
                //         .Equals(SettingValue.Advanced) ||
                //     !ArchipelagoClient.ServerData.GameSettings[SettingType.Difficulty].Equals(SettingValue.Advanced))
                // {
                //     Console.WriteLine("Power Seals not shuffled so calling original.");
                //     orig(self, roomKey);
                // }
                LocationRO powerSealLocation = null;
                foreach(LocationRO location in RandomizerConstants.GetAdvancedRandoLocationList())
                {
                    if(location.LocationName.Equals(roomKey))
                    {
                        powerSealLocation = location;
                    }
                }

                if(powerSealLocation == null)
                {
                    throw new RandomizerException($"Challenge room with room key '{roomKey}' was not found in the list of locations. This will need to be corrected for this challenge room to work.");
                }

                RandoItemRO challengeRoomRandoItem;
                if (RandomizerStateManager.Instance.CurrentLocationToItemMapping.TryGetValue(powerSealLocation,
                        out challengeRoomRandoItem))
                {
                }
                else if (ArchipelagoClient.HasConnected &&
                         ArchipelagoClient.ServerData.LocationToItemMapping.TryGetValue(powerSealLocation,
                             out challengeRoomRandoItem))
                {
                }
                else
                {
                    orig(self, roomKey);
                    return;
                }
                
                Console.WriteLine($"Challenge room '{powerSealLocation.PrettyLocationName}' completed. Providing rando item '{challengeRoomRandoItem}'.");
                if (ArchipelagoClient.HasConnected)
                {
                    ItemsAndLocationsHandler.SendLocationCheck(powerSealLocation);
                }
                //Handle timeshards
                else if (EItems.TIME_SHARD.Equals(challengeRoomRandoItem.Item))
                {
                    Manager<InventoryManager>.Instance.CollectTimeShard(1);
                    //Set this item to have been collected in the state manager
                    randoStateManager.GetSeedForFileSlot(randoStateManager.CurrentFileSlot).CollectedItems.Add(challengeRoomRandoItem);
                }
                else
                {
                    //Before adding the item to the inventory, add this item to the override
                    RandomizerStateManager.Instance.AddTempRandoItemOverride(challengeRoomRandoItem.Item);
                    Manager<InventoryManager>.Instance.AddItem(challengeRoomRandoItem.Item, 1);
                    //Now remove the override
                    RandomizerStateManager.Instance.RemoveTempRandoItemOverride(challengeRoomRandoItem.Item);
                }
                
                //I want to try to have a dialog popup say what the player got.
                DialogSequence challengeSequence = ScriptableObject.CreateInstance<DialogSequence>();
                if (EItems.NONE.Equals(challengeRoomRandoItem.Item))
                {
                    challengeSequence.dialogID = "ARCHIPELAGO_ITEM";
                    challengeSequence.name =
                        challengeRoomRandoItem.RecipientName.Equals(ArchipelagoClient.ServerData.SlotName)
                            ? $"{challengeRoomRandoItem.Name}"
                            : $"{challengeRoomRandoItem.Name} for {challengeRoomRandoItem.RecipientName}";
                }
                else
                {
                    challengeSequence.dialogID = "RANDO_ITEM";
                    challengeSequence.name = challengeRoomRandoItem.Item.ToString();
                }
                challengeSequence.choices = new List<DialogSequenceChoice>();
                Console.WriteLine($"Adding params: {challengeSequence.name}");
                AwardItemPopupParams challengeAwardItemParams = new AwardItemPopupParams(challengeSequence, true);
                Manager<UIManager>.Instance.ShowView<AwardItemPopup>(EScreenLayers.PROMPT, challengeAwardItemParams, true);

            }


            //For now calling the orig method once we are done so the game still things we are collecting seals. We can change this later.
            orig(self, roomKey);
        }

        bool HasItem_IsTrue(On.HasItem.orig_IsTrue orig, HasItem self)
        {
            bool hasItem = false;
            //Check to make sure this is an item that was randomized and make sure we are not ignoring this specific trigger check
            if (randoStateManager.IsRandomizedFile && randoStateManager.IsLocationRandomized(self.item, out var check) && !RandomizerConstants.GetSpecialTriggerNames().Contains(self.Owner.name))
            {
                if (self.transform.parent != null && "InteractionZone".Equals(self.Owner.name) && RandomizerConstants.GetSpecialTriggerNames().Contains(self.transform.parent.name) && EItems.KEY_OF_LOVE != self.item)
                {
                    //Special triggers that need to use normal logic, call orig method. This also includes the trigger check for the key of love on the sunken door because yeah.
                    Console.WriteLine($"While checking if player HasItem in an interaction zone, found parent object '{self.transform.parent.name}' in ignore logic. Calling orig HasItem logic.");
                    return orig(self);
                }

                //OLD WAY
                //Don't actually check for the item i have, check to see if I have the item that was at it's location.
                //int itemQuantity = Manager<InventoryManager>.Instance.GetItemQuantity(randoStateManager.CurrentLocationToItemMapping[check].Item);


                //NEW WAY
                //Don't actually check for the item I have, check to see if I have done this check before. We'll do this by seeing if the item at its location has been collected yet or not
                int itemQuantity = randoStateManager.GetSeedForFileSlot(randoStateManager.CurrentFileSlot).CollectedItems.Contains(randoStateManager.CurrentLocationToItemMapping[check]) ? randoStateManager.CurrentLocationToItemMapping[check].Quantity : 0;
                if (ArchipelagoClient.HasConnected)
                {
                    var locationID = ItemsAndLocationsHandler.LocationsLookup.FirstOrDefault(location => location.Key.Equals(check)).Value;
                    itemQuantity = ArchipelagoClient.ServerData.CheckedLocations.Contains(locationID) ? 1 : 0;
                }
                
                switch (self.conditionOperator)
                {
                    case EConditionOperator.LESS_THAN:
                        hasItem = itemQuantity < self.quantityToHave;
                        break;
                    case EConditionOperator.LESS_OR_EQUAL:
                        hasItem = itemQuantity <= self.quantityToHave;
                        break;
                    case EConditionOperator.EQUAL:
                        hasItem = itemQuantity == self.quantityToHave;
                        break;
                    case EConditionOperator.GREATER_OR_EQUAL:
                        hasItem = itemQuantity >= self.quantityToHave;
                        break;
                    case EConditionOperator.GREATER_THAN:
                        hasItem = itemQuantity > self.quantityToHave;
                        break;
                }

                Console.WriteLine($"Rando inventory check complete for check '{self.Owner.name}'. Item '{self.item}' || Actual Item Check '{randoStateManager.CurrentLocationToItemMapping[check]}' || Current Check '{self.conditionOperator}' || Expected Quantity '{self.quantityToHave}' || Actual Quantity '{itemQuantity}' || Condition Result '{hasItem}'.");
                
                return hasItem;
            }
            else //Call orig method
            {
                Console.WriteLine("HasItem check was not randomized. Doing vanilla checks.");
                Console.WriteLine($"Is randomized file : '{randoStateManager.IsRandomizedFile}' | Is location '{self.item}' randomized: '{randoStateManager.IsLocationRandomized(self.item, out check)}' | Not in the special triggers list: '{!RandomizerConstants.GetSpecialTriggerNames().Contains(self.Owner.name)}'|");
                return orig(self);
            }
            
        }
        
        int InventoryManager_GetItemQuantity(On.InventoryManager.orig_GetItemQuantity orig, InventoryManager self, EItems item)
        {
            //Just doing some logging here
            if (EItems.NONE.Equals(item))
            {
                Console.WriteLine($"INVENTORYMANAGER_GETITEMQUANTITY CALLED! Let's learn some stuff. Item: '{item}' | Quantity of said item: '{orig(self, item)}'");
            }
            //Manager<LevelManager>.Instance.onLevelLoaded
            return orig(self, item);
        }

        void LevelManager_LoadLevel(On.LevelManager.orig_LoadLevel orig, LevelManager self, LevelLoadingInfo levelInfo)
        {
            Console.WriteLine($"Loading Level: {levelInfo.levelName}");
            Console.WriteLine($"Entrance ID: {levelInfo.levelEntranceId}, Dimension: {levelInfo.dimension}, Scene Mode: {levelInfo.loadSceneMode}");
            Console.WriteLine($"Position Player: {levelInfo.positionPlayer}, Show Transition: {levelInfo.showTransition}, Transition Type: {levelInfo.transitionType}");
            Console.WriteLine($"Pooled Level Instance: {levelInfo.pooledLevelInstance}, Show Intro: {levelInfo.showLevelIntro}, Close Transition On Level Loaded: {levelInfo.closeTransitionOnLevelLoaded}");
            Console.WriteLine($"Set Scene as Active Scene: {levelInfo.setSceneAsActiveScene}");
            orig(self, levelInfo);
        }

        System.Collections.IEnumerator LevelManager_onLevelLoaded(On.LevelManager.orig_OnLevelLoaded orig,
            LevelManager self, Scene scene)
        {
            return orig(self, scene);
        }

        void LevelManager_EndLevelLoading(On.LevelManager.orig_EndLevelLoading orig, LevelManager self)
        {
            #if DEBUG
            Console.WriteLine($"Finished loading into {self.GetCurrentLevelEnum()}. " +
                              $"player position: {Manager<PlayerManager>.Instance.Player.transform.position.x}, " +
                              $"{Manager<PlayerManager>.Instance.Player.transform.position.y}, " +
                              $"last level: {self.lastLevelLoaded}, " +
                              $"scene: {self.CurrentSceneName}");
            #endif
            orig(self);
            // put the region we just loaded into in AP data storage for tracking
            if (ArchipelagoClient.Authenticated)
            {
                if (self.lastLevelLoaded.Equals(ELevel.Level_13_TowerOfTimeHQ + "_Build"))
                    ArchipelagoClient.Session.DataStorage[Scope.Slot, "CurrentRegion"] =
                        ELevel.Level_13_TowerOfTimeHQ.ToString();
                else
                    ArchipelagoClient.Session.DataStorage[Scope.Slot, "CurrentRegion"] =
                        self.GetCurrentLevelEnum().ToString();
            }
            if (Manager<LevelManager>.Instance.GetCurrentLevelEnum().Equals(ELevel.Level_11_B_MusicBox) &&
                randoStateManager.SkipMusicBox && randoStateManager.IsSafeTeleportState())
            {
                RandoLevelManager.SkipMusicBox();
            }
        }


        bool AwardNoteCutscene_ShouldPlay(On.AwardNoteCutscene.orig_ShouldPlay orig, AwardNoteCutscene self)
        {
            //Need to handle note cutscene triggers so they will play as long as I dont have the actual item it grants
            LocationRO noteCheck;
            if (randoStateManager.IsRandomizedFile && randoStateManager.IsLocationRandomized(self.noteToAward, out noteCheck)) //Double checking to prevent errors
            {
                Console.WriteLine($"Note cutscene check! Handling note '{self.noteToAward}' | Linked item: '{randoStateManager.CurrentLocationToItemMapping[noteCheck]}'");
                //bool shouldPlay = Manager<InventoryManager>.Instance.GetItemQuantity(randoStateManager.CurrentLocationToItemMapping[noteCheck].Item) <= 0 && !randoStateManager.IsNoteCutsceneTriggered(self.noteToAward);
                bool shouldPlay = !randoStateManager.IsNoteCutsceneTriggered(self.noteToAward);

                Console.WriteLine($"Should '{self.noteToAward}' cutscene play? '{shouldPlay}'");
                
                randoStateManager.SetNoteCutsceneTriggered(self.noteToAward);
                return shouldPlay;
            }
            else //Call orig method if for some reason the note I am checking for is not randomized
            {
                return orig(self);
            }
        }

        bool CutsceneHasPlayed_IsTrue(On.CutsceneHasPlayed.orig_IsTrue orig, CutsceneHasPlayed self)
        {
            LocationRO cutsceneCheck;
            if (randoStateManager.IsRandomizedFile && RandomizerConstants.GetCutsceneMappings().ContainsKey(self.cutsceneId) && randoStateManager.IsLocationRandomized(RandomizerConstants.GetCutsceneMappings()[self.cutsceneId], out cutsceneCheck))
            {

                //Check to make sure this is a cutscene i am configured to check, then check to make sure I actually have the item that is mapped to it
                Console.WriteLine($"Rando cutscene magic ahoy! Handling rando cutscene '{self.cutsceneId}' | Linked Item: {RandomizerConstants.GetCutsceneMappings()[self.cutsceneId]} | Rando Item: {randoStateManager.CurrentLocationToItemMapping[cutsceneCheck]}");
                if (self.cutsceneId.Equals("RuxxtinNoteAndAwardAmuletCutscene") && ArchipelagoClient.HasConnected) return ArchipelagoClient.ServerData.RuxxCutscene;
                

                //Check to see if I have the item that is at this check.
                //if (Manager<InventoryManager>.Instance.GetItemQuantity(randoStateManager.CurrentLocationToItemMapping[cutsceneCheck].Item) >= 1)
                if(randoStateManager.GetSeedForFileSlot(randoStateManager.CurrentFileSlot).CollectedItems.Contains(randoStateManager.CurrentLocationToItemMapping[cutsceneCheck]))
                {
                    //Return true, this cutscene has "been played"
                    Console.WriteLine($"Have rando item '{randoStateManager.CurrentLocationToItemMapping[cutsceneCheck]}' for cutscene '{self.cutsceneId}'. Progress Manager on if cutscene has played: '{Manager<ProgressionManager>.Instance.HasCutscenePlayed(self.cutsceneId)}'. Returning that we have already seen cutscene.");
                    return self.mustHavePlayed == true;
                }
                else
                {
                    //Havent seen the cutscene yet. Play it so i can get the item!
                    Console.WriteLine($"Do not have rando item '{randoStateManager.CurrentLocationToItemMapping[cutsceneCheck]}' for cutscene '{self.cutsceneId}'. Progress Manager on if cutscene has played: '{Manager<ProgressionManager>.Instance.HasCutscenePlayed(self.cutsceneId)}'. Returning that we have not seen cutscene yet.");
                    return self.mustHavePlayed == false;
                }
            }
            else //call the orig method
            {
                return orig(self);
            }

            
        }

        void SaveGameSelectionScreen_OnLoadGame(On.SaveGameSelectionScreen.orig_OnLoadGame orig, SaveGameSelectionScreen self, int slotIndex)
        {
            //slotIndex is 0-based, going to increment it locally to keep things simple.
            int fileSlot = slotIndex + 1;

            //This is probably a bad way to do this
            try
            {
                if (ArchipelagoData.LoadData(fileSlot))
                {
                    //The player is connected to an Archipelago server and trying to load a save file so check it's valid
                    Console.WriteLine($"Successfully loaded Archipelago seed {fileSlot}");
                    //I need to reload these after we connect so they take
                    Manager<DialogManager>.Instance.LoadDialogs(Manager<LocalizationManager>.Instance.CurrentLanguage);
                }
                else if (ArchipelagoClient.Authenticated && Manager<SaveManager>.Instance.GetSaveSlot(slotIndex).SecondsPlayed <= 100)
                {
                    randoStateManager.ResetRandomizerState();
                    randoStateManager.ResetSeedForFileSlot(fileSlot);
                    //Hopefully this ensures this is a clean rando slot so the player doesn't just connect with an invalid slot
                    randoStateManager.AddSeed(ArchipelagoClient.ServerData.StartNewSeed(fileSlot));
                    randoStateManager.CurrentLocationToItemMapping = ArchipelagoClient.ServerData.LocationToItemMapping;
                }
                else if (randoStateManager.HasSeedForFileSlot(fileSlot))
                {
                    //There's a valid seed and mapping available and Archipelago isn't involved
                    randoStateManager.CurrentLocationToItemMapping =
                        ItemRandomizerUtil.ParseLocationToItemMappings(randoStateManager.GetSeedForFileSlot(fileSlot));
                    RandoBossManager.DefeatedBosses = randoStateManager.DefeatedBosses[fileSlot];
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                orig(self, slotIndex);
            }
            //Generate the mappings based on the seed for the game if a seed was generated.
            if (randoStateManager.HasSeedForFileSlot(fileSlot) || ArchipelagoClient.HasConnected)
            {
                Console.WriteLine($"Seed exists for file slot {fileSlot}. Generating mappings.");
                //Load mappings
                randoStateManager.CurrentLocationDialogtoRandomDialogMapping = DialogChanger.GenerateDialogMappingforItems();

                randoStateManager.IsRandomizedFile = true;
                randoStateManager.CurrentFileSlot = fileSlot;
                //Log spoiler log
                randoStateManager.LogCurrentMappings();

                //We force a reload of all dialog when loading the game
                Manager<DialogManager>.Instance.LoadDialogs(Manager<LocalizationManager>.Instance.CurrentLanguage);
                Manager<ProgressionManager>.Instance.bossesDefeated =
                    Manager<ProgressionManager>.Instance.allTimeBossesDefeated = new List<string>();
            }
            else
            {
                //This save file does not have a seed associated with it or is not a randomized file. Reset the mappings so everything is back to normal.
                Console.WriteLine($"This file slot ({fileSlot}) has no seed generated or is not a randomized file. Resetting the mappings and putting game items back to normal.");
                Console.WriteLine($"Seed Info: {randoStateManager.GetSeedForFileSlot(fileSlot).Seed}");
                randoStateManager.ResetRandomizerState();
            }

            orig(self, slotIndex);
        }

        void SaveGameSelectionScreen_OnNewGame(On.SaveGameSelectionScreen.orig_OnNewGame orig, SaveGameSelectionScreen self, SaveSlotUI slot)
        {
            //Right now I am not randomizing game slots that are brand new.
            Console.WriteLine($"This file slot is brand new. Resetting the mappings and putting game items back to normal.");
            randoStateManager.ResetRandomizerState();
            randoStateManager.ResetSeedForFileSlot(slot.slotIndex + 1);

            orig(self, slot);
        }

        void PauseScreen_OnQuitToTitle(On.BackToTitleScreen.orig_GoBackToTitleScreen orig)
        {
            if (ArchipelagoClient.HasConnected)
            {
                if (ArchipelagoClient.Authenticated)
                    ArchipelagoClient.Disconnect();
                ArchipelagoClient.HasConnected = false;
                ArchipelagoClient.ServerData = null;
            }

            orig();
        }

        //Fixing necro cutscene check
        void CatacombLevelInitializer_OnBeforeInitDone(On.CatacombLevelInitializer.orig_OnBeforeInitDone orig, CatacombLevelInitializer self)
        {
            LocationRO necroLocation;
            if(randoStateManager.IsRandomizedFile && randoStateManager.IsLocationRandomized(EItems.NECROPHOBIC_WORKER, out necroLocation))
            {
                //check to see if we already have the item at Necro check
                if (ArchipelagoClient.HasConnected &&
                    !ArchipelagoClient.ServerData.CheckedLocations.Contains(
                        ItemsAndLocationsHandler.LocationsLookup[necroLocation]))
                    self.necrophobicWorkerCutscene.Play();
                
                //if (Manager<InventoryManager>.Instance.GetItemQuantity(randoStateManager.CurrentLocationToItemMapping[new LocationRO(EItems.NECROPHOBIC_WORKER.ToString())].Item) <= 0 && !Manager<DemoManager>.Instance.demoMode)
                if (!randoStateManager.GetSeedForFileSlot(randoStateManager.CurrentFileSlot).CollectedItems.Contains(randoStateManager.CurrentLocationToItemMapping[necroLocation]) && !Manager<DemoManager>.Instance.demoMode)
                {
                    //Run the cutscene if we dont
                    Console.WriteLine($"Have not received item '{randoStateManager.CurrentLocationToItemMapping[necroLocation]}' from Necro check. Playing cutscene.");
                    self.necrophobicWorkerCutscene.Play();
                }
                //if (Manager<InventoryManager>.Instance.GetItemQuantity(randoStateManager.CurrentLocationToItemMapping[new LocationRO(EItems.NECROPHOBIC_WORKER.ToString())].Item) >= 1 || Manager<DemoManager>.Instance.demoMode)
                if (randoStateManager.GetSeedForFileSlot(randoStateManager.CurrentFileSlot).CollectedItems.Contains(randoStateManager.CurrentLocationToItemMapping[necroLocation]) || Manager<DemoManager>.Instance.demoMode)
                {
                    //set necro inactive if we do
                    Console.WriteLine($"Already have item '{randoStateManager.CurrentLocationToItemMapping[necroLocation]}' from Necro check. Will not play cutscene.");
                    self.necrophobicWorkerCutscene.phobekin.gameObject.SetActive(false);
                }
                //Call our overriden fixing function
                RandoCatacombLevelInitializer.FixPlayerStuckInChallengeRoom();
            }
            else
            {
                //we are not rando here, call orig method
                orig(self);
            }
            
        }

        // Breaking into Necro cutscene to fix things
        void NecrophobicWorkerCutscene_Play(On.NecrophobicWorkerCutscene.orig_Play orig, NecrophobicWorkerCutscene self)
        {
            //Cutscene moves Ninja around, lets see if i can stop it by making that "location" the current location the player is.
            self.playerStartPosition = UnityEngine.Object.FindObjectOfType<PlayerController>().transform;
            orig(self);
        }

        void RuxxtinNoteAndAwardAmuletCutscene_Play(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            while(cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(55)))
            {
                cursor.EmitDelegate<Func<EItems, EItems>>(GetRandoItemByItem);
            }
            
        }

        bool UpgradeButtonData_IsStoryUnlocked(On.UpgradeButtonData.orig_IsStoryUnlocked orig, UpgradeButtonData self)
        {
            bool isUnlocked;

            //Checking if this particular upgrade is the glide attack
            if(EShopUpgradeID.GLIDE_ATTACK.Equals(self.upgradeID))
            {
                //Unlock the glide attack (no need to keep it hidden, player can just buy it whenever they want.
                isUnlocked = true;
            }
            else
            {
                isUnlocked = orig(self);
            }

            //I think there is where I can catch things like checks for the wingsuit attack upgrade.
            Console.WriteLine($"Checking upgrade '{self.upgradeID}'. Is story unlocked: {isUnlocked}");

            return isUnlocked;
        }
        
        
        void MegaTimeShard_OnBreakDone(On.MegaTimeShard.orig_OnBreakDone orig, MegaTimeShard self)
        {
            var currentLevel = Manager<LevelManager>.Instance.GetCurrentLevelEnum();
            var currentRoom = Manager<Level>.Instance.CurrentRoom.roomKey;
            Console.WriteLine($"Broke Mega Time shard in {currentLevel}, {currentRoom}");
            if (randoStateManager.MegaShards)
                RandoTimeShardManager.BreakShard(new RandoTimeShardManager.MegaShard(currentLevel, currentRoom));
            orig(self);
        }

        int ProgressionManager_TotalPowerSealCollected(On.ProgressionManager.orig_TotalPowerSealCollected orig,
            ProgressionManager self)
        {
            return randoStateManager.PowerSealManager?.AmountPowerSealsCollected() ?? orig(self);
        }

        void Cutscene_Play(On.Cutscene.orig_Play orig, Cutscene self)
        {
            Console.WriteLine($"Playing cutscene: {self}");
            // if (randoStateManager)
            orig(self);
        }

        void PhantomIntro_OnEnterRoom(On.PhantomIntroCutscene.orig_OnEnterRoom orig, PhantomIntroCutscene self,
            bool teleportedInRoom)
        {
            if (randoStateManager.SkipPhantom)
            {
                Manager<AudioManager>.Instance.StopMusic();
                UnityEngine.Object.FindObjectOfType<PhantomOutroCutscene>().Play();
            }
            else
            {
                orig(self, teleportedInRoom);
            }
        }


        View UIManager_ShowView(On.UIManager.orig_ShowView orig, UIManager self, Type viewType,
            EScreenLayers layer, IViewParams screenParams, bool transitionIn, AnimatorUpdateMode animUpdateMode)
        {
            Console.WriteLine($"viewType {viewType}");
            Console.WriteLine($"layer {layer}");
            Console.WriteLine($"params {screenParams}");
            Console.WriteLine($"transition {transitionIn}");
            Console.WriteLine($"updateMode {animUpdateMode}");
            return orig(self, viewType, layer, screenParams, transitionIn, animUpdateMode);
        }

        void DialogCutscene_Play(On.DialogCutscene.orig_Play orig, DialogCutscene self)
        {
            Console.WriteLine($"Playing dialog cutscene: {self}");
            //ruxxtin cutscene is being a bitch so just gonna hard code around it here.
            if (ArchipelagoClient.HasConnected && self.name.Equals("ReadNote"))
            {
                if (randoStateManager.IsLocationRandomized(EItems.RUXXTIN_AMULET, out var check))
                {
                    var locID = ItemsAndLocationsHandler.LocationsLookup
                        .FirstOrDefault(location => location.Key.Equals(check)).Value;
                    if (!ArchipelagoClient.ServerData.CheckedLocations.Contains(locID))
                    {
                        var sendItem = randoStateManager.CurrentLocationToItemMapping[check];
                        ItemsAndLocationsHandler.SendLocationCheck(check);
                        DialogSequence sendItemDialog = ScriptableObject.CreateInstance<DialogSequence>();
                        sendItemDialog.dialogID = "ARCHIPELAGO_ITEM";
                        sendItemDialog.name =
                            sendItem.RecipientName.Equals(ArchipelagoClient.ServerData.SlotName)
                                ? $"{sendItem.Name}"
                                : $"{sendItem.Name} for {sendItem.RecipientName}";
                        sendItemDialog.choices = new List<DialogSequenceChoice>();
                        var sendItemParams = new AwardItemPopupParams(sendItemDialog, true);
                        Manager<UIManager>.Instance.ShowView<AwardItemPopup>(EScreenLayers.PROMPT, sendItemParams);
                    }
                }
            }
            orig(self);
        }

        void MusicBox_SetNotesState(On.MusicBox.orig_SetNotesState orig, MusicBox self)
        {
            // this determines which notes should be shown present in the music box
            orig(self);
        }


        ///On submit of rando file location
        bool OnEnterFileSlot(string fileSlot)
        {
            Console.WriteLine($"In Method: OnEnterFileSlot. Provided value: '{fileSlot}'");
            Console.WriteLine($"Received file slot number: {fileSlot}");
            int slot = Convert.ToInt32(fileSlot);
            if (slot < 1 || slot > 3)
            {
                Console.WriteLine($"Invalid slot number provided: {slot}");
                return false;
            }

            //Load in mappings and save them to the state

            //Load encoded seed information
            string encodedSeedInfo = ItemRandomizerUtil.LoadMappingsFromFile(slot);
            Console.WriteLine($"File reading complete. Received the following encoded seed info: '{encodedSeedInfo}'");
            string decodedSeedInfo = ItemRandomizerUtil.DecryptSeedInfo(encodedSeedInfo);
            Console.WriteLine($"Decryption complete. Received the following seed info: '{decodedSeedInfo}'");
            SeedRO seedRO = ItemRandomizerUtil.ParseSeed(slot, decodedSeedInfo);

            randoStateManager.AddSeed(seedRO);

            //Save
            Save.seedData = randomizerSaveMethod.GenerateSaveData();

            return true;
        }

        bool OnRandoFileResetConfirmation(string answer)
        {
            Console.WriteLine($"In Method: OnResetRandoFileSlot. Provided value: '{answer}'");
            
            if(!"y".Equals(answer.ToLowerInvariant()))
            {
                return true;
            }

            ArchipelagoData.ClearData();
            randoStateManager.ResetRandomizerState();
            for (int i = 1; i <= 3; i++)
            {
                randoStateManager.ResetSeedForFileSlot(i);
            }
            Save.seedData = string.Empty;
            string path = Application.persistentDataPath + "/SaveGame.txt";
            using (StreamWriter sw = File.CreateText(path))
            {
                sw.WriteLine(RandomizerConstants.SAVE_FILE_STRING);
            }
            
            Console.WriteLine("Save file written. Now loading file.");
            Manager<SaveManager>.Instance.LoadSaveGame();
            //Delete the existing save file selection ui since it really wants to hold on to the previous saves data.
            GameObject.Destroy(Manager<UIManager>.Instance.GetView<SaveGameSelectionScreen>().gameObject);
            //Reinit the save file selection ui.
            SaveGameSelectionScreen selectionScreen = Manager<UIManager>.Instance.ShowView<SaveGameSelectionScreen>(EScreenLayers.MAIN, null, false, AnimatorUpdateMode.Normal);
            selectionScreen.GoOffscreenInstant();

            return true;
        }

        public static void OnToggleWindmillShuriken()
        {
            //Toggle Shuriken
            Manager<ProgressionManager>.Instance.useWindmillShuriken = !Manager<ProgressionManager>.Instance.useWindmillShuriken;
            //Update UI
            InGameHud view = Manager<UIManager>.Instance.GetView<InGameHud>();
            if (view != null)
            {
                view.UpdateShurikenVisibility();
            }
        }

        void OnSelectTeleportToHq()
        {

            //Properly close out of the mod options and get the game state back together
            Manager<PauseManager>.Instance.Resume();
            Manager<UIManager>.Instance.GetView<OptionScreen>().Close(false);                
            Console.WriteLine("Teleporting to HQ!");
            Courier.UI.ModOptionScreen.Close(false);

            //Fade the music out because musiception is annoying
            Manager<AudioManager>.Instance.FadeMusicVolume(1f, 0f, true);

            //Load the HQ
            Manager<TowerOfTimeHQManager>.Instance.TeleportInToTHQ(true, ELevelEntranceID.ENTRANCE_A, null, null, true);
        }

        void OnSelectTeleportToNinjaVillage()
        {
            Console.WriteLine("Attempting to teleport to Ninja Village.");
            
            // Properly close out of the mod options and get the game state back together
            Manager<PauseManager>.Instance.Resume();
            Manager<UIManager>.Instance.GetView<OptionScreen>().Close(false);
            Courier.UI.ModOptionScreen.Close(false);
            EBits dimension = Manager<DimensionManager>.Instance.currentDimension;

            //Fade the music out because musiception is annoying
            Manager<AudioManager>.Instance.FadeMusicVolume(1f, 0f, true);

            //Load to Ninja Village
            Manager<ProgressionManager>.Instance.checkpointSaveInfo.loadedLevelPlayerPosition = new Vector2(-153.3f, -56.5f);
            LevelLoadingInfo levelLoadingInfo = new LevelLoadingInfo("Level_01_NinjaVillage_Build", false, true, LoadSceneMode.Single, ELevelEntranceID.NONE, dimension);
            Manager<LevelManager>.Instance.LoadLevel(levelLoadingInfo);
            
            Console.WriteLine("Teleport to Ninja Village complete.");
        }

        bool OnSelectArchipelagoHost(string answer)
        {
            if (answer == null) return true;
            if (ArchipelagoClient.ServerData == null) ArchipelagoClient.ServerData = new ArchipelagoData();
            var uri = answer;
            if (answer.Contains(" "))
            {
                var splits = answer.Split(' ');
                uri = String.Join(".", splits.ToArray());
            }
            ArchipelagoClient.ServerData.Uri = uri;
            return true;
        }

        bool OnSelectArchipelagoPort(string answer)
        {
            if (answer == null) return true;
            if (ArchipelagoClient.ServerData == null) ArchipelagoClient.ServerData = new ArchipelagoData();
            int.TryParse(answer, out var port);
            ArchipelagoClient.ServerData.Port = port;
            return true;
        }

        bool OnSelectArchipelagoName(string answer)
        {
            if (answer == null) return true;
            if (ArchipelagoClient.ServerData == null) ArchipelagoClient.ServerData = new ArchipelagoData();
            ArchipelagoClient.ServerData.SlotName = answer;
            return true;
        }

        bool OnSelectArchipelagoPass(string answer)
        {
            if (answer == null) return true;
            if (ArchipelagoClient.ServerData == null) ArchipelagoClient.ServerData = new ArchipelagoData();
            ArchipelagoClient.ServerData.Password = answer;
            return true;
        }

        void OnSelectArchipelagoConnect()
        {
            if (ArchipelagoClient.ServerData == null) return;
            if (ArchipelagoClient.ServerData.SlotName == null) return;
            if (ArchipelagoClient.ServerData.Uri == null) ArchipelagoClient.ServerData.Uri = "archipelago.gg";
            if (ArchipelagoClient.ServerData.Port == 0) ArchipelagoClient.ServerData.Port = 38281;

            ArchipelagoClient.ConnectAsync(archipelagoConnectButton);
        }

        void OnSelectArchipelagoRelease()
        {
            ArchipelagoClient.Session.Socket.SendPacket(new SayPacket { Text = "!release" });
        }

        void OnSelectArchipelagoCollect()
        {
            ArchipelagoClient.Session.Socket.SendPacket(new SayPacket { Text = "!collect" });
        }

        bool OnSelectArchipelagoHint(string answer)
        {
            if (!string.IsNullOrEmpty(answer))
            {
                ArchipelagoClient.Session.Socket.SendPacket(new SayPacket { Text = $"!hint {answer}" });
            }
            return true;
        }

        static void OnToggleAPStatus()
        {
            ArchipelagoClient.DisplayStatus = !ArchipelagoClient.DisplayStatus;
        }

        static void OnToggleAPMessages()
        {
            ArchipelagoClient.DisplayAPMessages = !ArchipelagoClient.DisplayAPMessages;
        }

        static void OnToggleDeathLink()
        {
            ArchipelagoData.DeathLink = !ArchipelagoData.DeathLink;
            if (ArchipelagoData.DeathLink) ArchipelagoClient.DeathLinkHandler.DeathLinkService.EnableDeathLink();
            else ArchipelagoClient.DeathLinkHandler.DeathLinkService.DisableDeathLink();
        }

        bool OnSelectMessageTimer(string answer)
        {
            if (answer == null) return true;
            if (ArchipelagoClient.ServerData == null) ArchipelagoClient.ServerData = new ArchipelagoData();
            int.TryParse(answer, out var newTime);
            updateTime = newTime;
            return true;
        }

        /// <summary>
        /// Delegate function for getting rando item. This can be used by IL hooks that need to make this call later.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private EItems GetRandoItemByItem(EItems item)
        {
            LocationRO ruxxAmuletLocation;
            
            if(randoStateManager.IsLocationRandomized(item, out ruxxAmuletLocation))
            {
                Console.WriteLine($"IL Wackiness -- Checking for Item '{item}' | Rando item to return '{randoStateManager.CurrentLocationToItemMapping[ruxxAmuletLocation]}'");

                if (ArchipelagoClient.HasConnected) ArchipelagoClient.ServerData.RuxxCutscene = true;
                EItems randoItem = randoStateManager.CurrentLocationToItemMapping[ruxxAmuletLocation].Item;
                
                if(EItems.TIME_SHARD.Equals(randoItem))
                {
                    /* Having a lot of problems with timeshards and the ruxxtin check due to it having some checks behind the scenes.
                     * What I am trying is to change the item to the NONE value since that is expected to have no quantity. This will trick the cutscene into playing correctly the first time.
                     * Checks after the first time rely on the collected items list so it shouldn't have any impact...
                     */
                    randoItem = EItems.NONE;
                }
                return randoItem;
            }
            else
            {
                return item;
            }
        }

        private string GetCurrentSeedNum()
        {
            string seedNum = "Unknown";

            if(randoStateManager != null && randoStateManager.GetSeedForFileSlot(randoStateManager.CurrentFileSlot).Seed > 0)
            {
                seedNum = randoStateManager.GetSeedForFileSlot(randoStateManager.CurrentFileSlot).Seed.ToString();
            }
            if (ArchipelagoClient.HasConnected)
            {
                seedNum = ArchipelagoClient.ServerData.SeedName;
            }

            return seedNum;
        }

        private void OnSceneLoadedRando(Scene scene, LoadSceneMode mode)
        {
            Console.WriteLine($"Scene loaded: '{scene.name}'");
        }

        private void PlayerController_OnUpdate(PlayerController controller)
        {
            if (!ArchipelagoClient.HasConnected) return;
            ArchipelagoClient.DeathLinkHandler.Player = controller;
            if (randoStateManager.IsSafeTeleportState() && !Manager<PauseManager>.Instance.IsPaused)
                ArchipelagoClient.DeathLinkHandler.KillPlayer();
            //This updates every {updateTime} seconds
            updateTimer += Time.deltaTime;
            if (!(updateTimer >= updateTime)) return;
            apMessagesDisplay16.text = apMessagesDisplay8.text = ArchipelagoClient.UpdateMessagesText();
            updateTimer = 0;
            if (!randoStateManager.IsSafeTeleportState()) return;
            ArchipelagoClient.UpdateArchipelagoState();
        }

        private void InGameHud_OnGUI(On.InGameHud.orig_OnGUI orig, InGameHud self)
        {
            orig(self);
            if (apTextDisplay8 == null)
            {
                apTextDisplay8 = UnityEngine.Object.Instantiate(self.hud_8.coinCount, self.hud_8.gameObject.transform);
                apTextDisplay16 = UnityEngine.Object.Instantiate(self.hud_16.coinCount, self.hud_16.gameObject.transform);
                apTextDisplay8.transform.Translate(0f, -110f, 0f);
                apTextDisplay16.transform.Translate(0f, -110f, 0f);
                apTextDisplay16.fontSize = apTextDisplay8.fontSize = 4f;
                apTextDisplay16.alignment = apTextDisplay8.alignment = TextAlignmentOptions.TopRight;
                apTextDisplay16.enableWordWrapping = apTextDisplay8.enableWordWrapping = true;
                apTextDisplay16.color = apTextDisplay8.color = Color.white;

                apMessagesDisplay8 = UnityEngine.Object.Instantiate(self.hud_8.coinCount, self.hud_8.gameObject.transform);
                apMessagesDisplay16 = UnityEngine.Object.Instantiate(self.hud_16.coinCount, self.hud_16.gameObject.transform);
                apMessagesDisplay8.transform.Translate(0f, -200f, 0f);
                apMessagesDisplay16.transform.Translate(0f, -200f, 0f);
                apMessagesDisplay16.fontSize = apMessagesDisplay8.fontSize = 4.2f;
                apMessagesDisplay16.alignment = apMessagesDisplay8.alignment = TextAlignmentOptions.BottomRight;
                apMessagesDisplay16.enableWordWrapping = apMessagesDisplay16.enableWordWrapping = true;
                apMessagesDisplay16.color = apMessagesDisplay8.color = Color.green;
                apMessagesDisplay16.text = apMessagesDisplay8.text = string.Empty;
            }
            //This updates every frame
            apTextDisplay16.text = apTextDisplay8.text = ArchipelagoClient.UpdateStatusText();
        }

        private void SaveManager_DoActualSave(On.SaveManager.orig_DoActualSaving orig, SaveManager self, bool applySaveDelay = true)
        {
            Console.WriteLine($"checking if saveSlot {self.GetSaveGameSlotIndex()} name matches {randoStateManager.CurrentFileSlot} name");
            if (ArchipelagoClient.HasConnected)
            {
                // The game calls the save method after the ending cutscene before rolling credits
                if (ArchipelagoClient.Authenticated
                    && Manager<LevelManager>.Instance.GetCurrentLevelEnum().Equals(ELevel.Level_Ending))
                {
                    ArchipelagoClient.UpdateClientStatus(ArchipelagoClientState.ClientGoal);
                }
                ArchipelagoClient.ServerData.UpdateSave();
                var saveSlot = self.GetCurrentSaveGameSlot();
                if (!saveSlot.SlotName.Equals(ArchipelagoClient.ServerData.SlotName))
                {
                    // FieldInfo saveGameField = typeof(SaveManager).GetField("saveGame", BindingFlags.NonPublic | BindingFlags.Instance);
                    // SaveGame saveGame = saveGameField.GetValue(self) as SaveGame;
                    // saveGame.saveSlots[self.GetSaveGameSlotIndex()].SlotName = ArchipelagoClient.ServerData.SlotName;
                    saveSlot.SlotName = ArchipelagoClient.ServerData.SlotName;
                }
            }
            orig(self, applySaveDelay);
        }

        private void Quarble_OnPlayerDied(On.Quarble.orig_OnPlayerDied orig, Quarble self, EDeathType deathType, bool fastReload)
        {
            orig(self, deathType, fastReload);
            ArchipelagoClient.DeathLinkHandler.SendDeathLink(deathType);
        }
    }
}
