﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Mod.Courier.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace MessengerRando.Utils
{
    public static class OptionsExporter
    {
        private delegate void OnExport(bool result);
        private static bool exporting;
        private static string messageText = "";

        public static void ExportAsync(SubMenuButtonInfo exportButton)
        {
            if (exporting) return;
            exporting = true;
            Console.WriteLine("Exporting options");
            Export(result => OnExported(result, exportButton));
        }

        private static void Export(OnExport attempt)
        {
            attempt(Export());
        }

        private static bool Export()
        {
            try
            {
                var playerOptions = RandomizerOptions.GetOptions();
                var data = new JObject
                {
                    ["name"] = RandomizerOptions.Name,
                    ["description"] = $"Generated by The Messenger Randomizer AP v{ItemRandomizerUtil.GetModVersion()}",
                    ["game"] = "The Messenger",
                    ["The Messenger"] = new JObject(),
                };
                foreach (var kvp in playerOptions)
                {
                    if (int.TryParse(kvp.Value, out var num))
                        data["The Messenger"][kvp.Key] = num;
                    else
                        data["The Messenger"][kvp.Key] = kvp.Value;
                }
                Console.WriteLine(data);

                var folder = Directory.GetCurrentDirectory() + "\\Archipelago";
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                foreach (var oldFile in Directory.GetFiles(folder))
                {
                    if (oldFile.EndsWith("json"))
                        File.Delete(oldFile);
                }
                var file = folder;
                file += $"\\{RandomizerOptions.Name}.json";
                if (!File.Exists(file))
                    File.Create(file).Dispose();
                File.WriteAllText(file, data.ToString());
            }
            catch (Exception e)
            {
                messageText = e.ToString();
                Console.WriteLine(e);
                return false;
            }
            return true;
        }

        private static void OnExported(bool result, SubMenuButtonInfo exportButton)
        {
            TextEntryPopup generatePopup = TextEntryButtonInfo.InitTextEntryPopup(
                exportButton.addedTo,
                string.Empty,
                entry => true,
                0,
                null,
                TextEntryButtonInfo.CharsetFlags.Space);
            
            generatePopup.Init(result ? "Options successfully exported!" : $"Options export failed: {messageText}");
            generatePopup.gameObject.SetActive(true);
            exporting = false;
        }
    }
}