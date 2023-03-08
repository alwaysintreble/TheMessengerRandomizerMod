﻿using System;
using System.Collections.Generic;
using MessengerRando.Utils.Constants;
using WebSocketSharp;

namespace MessengerRando.GameOverrideManagers
{
    public static class RandoRoomManager
    {
        public static bool RoomRando;
        public static bool RoomOverride;
        public static Dictionary<string, string> RoomMap; // old room name - new room name
        
        private static string GetRoomKey(ScreenEdge left, ScreenEdge right, ScreenEdge bottom, ScreenEdge top)
        {
            return $"{left.edgeIdX} {right.edgeIdX} {bottom.edgeIdY} {top.edgeIdY}";
        }

        private static void SetRoomKey(ScreenEdge left, ScreenEdge right, ScreenEdge bottom, ScreenEdge top,
            string roomKey)
        {
            var edges = roomKey.Split(' ');
            left.edgeIdX = edges[0]; right.edgeIdX = edges[1]; bottom.edgeIdY = edges[2]; top.edgeIdY = edges[3];
        }

        public static bool IsBossRoom(string roomKey, out string bossName)
        {
            return RandoBossManager.RoomToVanillaBoss.TryGetValue(roomKey, out bossName) &&
                   RandoBossManager.BossLocations.TryGetValue(bossName, out var bossLocation) &&
                   bossLocation.BossRegion.Equals(Manager<LevelManager>.Instance.GetCurrentLevelEnum());
        }
        
        public static void Level_ChangeRoom(On.Level.orig_ChangeRoom orig, Level self,
            ScreenEdge leftEdge, ScreenEdge rightEdge,
            ScreenEdge bottomEdge, ScreenEdge topEdge,
            bool teleportedInRoom)
        {
            var oldRoomKey = GetRoomKey(leftEdge, rightEdge, bottomEdge, topEdge);
            Console.WriteLine($"new roomKey: {oldRoomKey}");
            Console.WriteLine(self.CurrentRoom != null
                ? $"currentRoom roomKey: {self.CurrentRoom.roomKey}"
                : "currentRoom does not exist.");
            Console.WriteLine($"teleported: {teleportedInRoom}");
            var position = Manager<PlayerManager>.Instance.Player.transform.position;
            Console.WriteLine("Player position: " +
                              $"{position.x} " +
                              $"{position.y} " +
                              $"{position.z}");
            
            //This func checks if the new roomKey exists within levelRooms before changing and checks if currentRoom exists
            //if we're in a room, it leaves the current room then enters the new room with the teleported bool
            //no idea what the teleported bool does currently
            orig(self, leftEdge, rightEdge, bottomEdge, topEdge, teleportedInRoom);
            if (RoomOverride)
            {
                RoomOverride = false;
                return;
            }
            if (IsBossRoom(oldRoomKey, out var bossName))
                RandoBossManager.ShouldFightBoss(bossName);
            else if (RoomRando)
            {
                var currentLevel = Manager<LevelManager>.Instance.GetCurrentLevelEnum();
                var newRoom = PlaceInRoom(oldRoomKey, currentLevel, out var transition);
                if (newRoom.RoomKey.IsNullOrEmpty() || transition.Direction.IsNullOrEmpty()) return;
                
                SetRoomKey(leftEdge, rightEdge, bottomEdge, topEdge, newRoom.RoomKey);
                RoomOverride = true;
                if (newRoom.Region.Equals(currentLevel))
                    Manager<Level>.Instance.ChangeRoom(leftEdge, rightEdge, bottomEdge, topEdge, teleportedInRoom);
                else
                    RandoLevelManager.TeleportInArea(newRoom.Region, transition.Position, transition.Dimension);
            }
        }
        
        static bool WithinRange(float pos1, float pos2)
        {
            var comparison = pos2 - pos1;
            if (comparison < 0) comparison *= -1;
            return comparison <= 10;
        }

        public static RoomConstants.RandoRoom PlaceInRoom(string oldRoomName, ELevel currentLevel, out RoomConstants.RoomTransition newTransition)
        {
            newTransition = new RoomConstants.RoomTransition();
            if (!RoomConstants.RoomNameLookup.TryGetValue(new RoomConstants.RandoRoom(oldRoomName, currentLevel),
                    out var oldRoom)
                || !RoomConstants.TransitionLookup.TryGetValue(oldRoom, out var oldTransitions)
                || !RoomMap.TryGetValue(oldRoom, out var newName)
                || !RoomConstants.TransitionLookup.TryGetValue(newName, out var newTransitions))
                return new RoomConstants.RandoRoom();
            
            var currentPos = Manager<PlayerManager>.Instance.Player.transform.position.x;
            var currentTransition =
                oldTransitions.Find(transition => WithinRange(currentPos, transition.Position.x));
            newTransition = newTransitions.Find(transition => currentTransition.Equals(transition));

            return !RoomConstants.RoomLookup.TryGetValue(newName, out var newRoom)
                ? new RoomConstants.RandoRoom()
                : newRoom;
        }
    }
}