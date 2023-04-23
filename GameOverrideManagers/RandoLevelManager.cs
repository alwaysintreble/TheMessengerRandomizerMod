﻿using System;
using MessengerRando.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MessengerRando.GameOverrideManagers
{
    public class RandoLevelManager
    {
        private static bool teleporting;
        public static void SkipMusicBox()
        {
            if (teleporting)
            {
                teleporting = false;
                return;
            }
            Manager<AudioManager>.Instance.StopMusic();
            var playerPosition = RandomizerStateManager.Instance.SkipMusicBox
                ? new Vector2(125, 40)
                : new Vector2(-428, -55);

            TeleportInArea(ELevel.Level_11_B_MusicBox, playerPosition, EBits.BITS_16);
        }

        public static void TeleportInArea(ELevel area, Vector2 position, EBits dimension)
        {
            if (teleporting)
            {
                teleporting = false;
                return;
            }
            Manager<AudioManager>.Instance.StopMusic();
            Manager<ProgressionManager>.Instance.checkpointSaveInfo.loadedLevelPlayerPosition = position;
            LevelLoadingInfo levelLoadingInfo = new LevelLoadingInfo(area + "_Build",
                true, true, LoadSceneMode.Single,
                ELevelEntranceID.NONE, dimension);
            teleporting = true;
            Manager<LevelManager>.Instance.LoadLevel(levelLoadingInfo);
        }


        public static void Level_ChangeRoom(On.Level.orig_ChangeRoom orig, Level self,
            ScreenEdge newRoomLeftEdge, ScreenEdge newRoomRightEdge,
            ScreenEdge newRoomBottomEdge, ScreenEdge newRoomTopEdge,
            bool teleportedInRoom)
        {
            string GetRoomKey()
            {
                return newRoomLeftEdge.edgeIdX + newRoomRightEdge.edgeIdX
                                               + newRoomBottomEdge.edgeIdY + newRoomTopEdge.edgeIdY;
            }
            #if DEBUG
            Console.WriteLine("new room params:" +
                              $"{newRoomLeftEdge.edgeIdX} " +
                              $"{newRoomRightEdge.edgeIdX} " +
                              $"{newRoomBottomEdge.edgeIdY} " +
                              $"{newRoomTopEdge.edgeIdY} ");
            Console.WriteLine($"new roomKey: {GetRoomKey()}");
            Console.WriteLine(self.CurrentRoom != null
                ? $"currentRoom roomKey: {self.CurrentRoom.roomKey}"
                : "currentRoom does not exist.");
            Console.WriteLine($"teleported: {teleportedInRoom}");
            #endif
            var position = Manager<PlayerManager>.Instance.Player.transform.position;
            #if DEBUG
            Console.WriteLine("Player position: " +
                              $"{position.x} " +
                              $"{position.y} " +
                              $"{position.z}");
            #endif


            //This func checks if the new roomKey exists within levelRooms before changing and checks if currentRoom exists
            //if we're in a room, it leaves the current room then enters the new room with the teleported bool
            //no idea what the teleported bool does currently
            orig(self, newRoomLeftEdge, newRoomRightEdge, newRoomBottomEdge, newRoomTopEdge, teleportedInRoom);
            RandoBossManager.ShouldFightBoss(GetRoomKey());
        }
    }
}