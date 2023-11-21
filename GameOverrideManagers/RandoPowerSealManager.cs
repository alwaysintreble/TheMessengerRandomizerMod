﻿using System;
using System.Collections.Generic;
using MessengerRando.Archipelago;
using MessengerRando.Utils;
using Object = UnityEngine.Object;

namespace MessengerRando.GameOverrideManagers
{
    public class RandoPowerSealManager
    {
        public RandoPowerSealManager(int requiredPowerSeals)
        {
            if (requiredPowerSeals == 0)
                requiredPowerSeals = 45;
            Manager<ProgressionManager>.Instance.powerSealTotal = requiredPowerSeals;
        }

        public void AddPowerSeal() => ArchipelagoClient.ServerData.PowerSealsCollected++;


        public void OnShopChestOpen(On.ShopChestOpenCutscene.orig_OnChestOpened orig, ShopChestOpenCutscene self)
        {
            try
            {
                //going to attempt to teleport the player to the ending sequence when they open the chest
                OnShopChestOpen();
                self.EndCutScene();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                orig(self);
            }
        }

        public void OnShopChestOpen(On.ShopChestChangeShurikenCutscene.orig_Play orig, ShopChestChangeShurikenCutscene self)
        {
            try
            {
                //going to attempt to teleport the player to the ending sequence when they open the chest
                OnShopChestOpen();
                self.EndCutScene();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                orig(self);
            }
        }

        private void OnShopChestOpen()
        {
            try
            {
                Object.FindObjectOfType<Shop>().LeaveToCurrentLevel();
                RandoLevelManager.SkipMusicBox();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Assigns our total power seal count to the game and then returns the value. Unsure if the assignment is safe
        /// here, but trying it so it'll show the required count in the dialog.
        /// </summary>
        /// <returns></returns>
        public int AmountPowerSealsCollected() => ArchipelagoClient.ServerData.PowerSealsCollected;
    }
}