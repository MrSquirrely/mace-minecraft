﻿/*
    Mace
    Copyright (C) 2011-2012 Robson
    http://iceyboard.no-ip.org

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Substrate;
using Substrate.Entities;
using Substrate.ImportExport;
using Substrate.TileEntities;

namespace Mace
{
    static class GenerateWorld
    {
        struct WorldCity
        {
            public string ThemeName;
            public int x;
            public int z;
            public int ChunkLength;
        }
        static WorldCity[] worldCities;
        public static List<string> lstCityNames = new List<string>();

        static public void Generate(frmMace frmLogForm, string UserWorldName, string strWorldSeed,
                                    string strWorldType, bool booWorldMapFeatures, int TotalCities, string[] strCheckedThemes,
                                    int ChunksBetweenCities, string strSpawnPoint, bool booExportSchematics,
                                    string strSelectedNPCs, string strUndergroundOres)
        {

            frmLogForm.UpdateLog("Started at " + DateTime.Now.ToLocalTime(), false, true);

            worldCities = new WorldCity[TotalCities];
            lstCityNames.Clear();

            RNG.SetRandomSeed();

            #region create minecraft world directory from a random unused world name
            string strFolder = String.Empty, strWorldName = String.Empty;

            UserWorldName = UserWorldName.ToSafeFilename();
            if (UserWorldName.Trim().Length == 0)
            {
                UserWorldName = "random";
            }

            if (UserWorldName.ToLower().Trim() != "random")
            {
                if (Directory.Exists(UserWorldName.ToMinecraftSaveDirectory()))
                {
                    if (MessageBox.Show("A world called \"" + UserWorldName + "\" already exists. " +
                                    "Would you like to use a random name instead?", "World already exists",
                                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.Cancel)
                    {
                        frmLogForm.UpdateLog("Cancelled, because a world with this name already exists.", false, false);
                        return;
                    }
                }
                else
                {
                    strWorldName = UserWorldName;
                    strFolder = strWorldName.ToMinecraftSaveDirectory();
                }
            }
            if (strWorldName.Length == 0)
            {
                strWorldName = Utils.GenerateWorldName();
                strFolder = strWorldName.ToMinecraftSaveDirectory();
            }
            Directory.CreateDirectory(strFolder);
            frmLogForm.btnSaveLogNormal.Tag = Path.Combine(strFolder, "LogNormal.txt");
            frmLogForm.btnSaveLogVerbose.Tag = Path.Combine(strFolder, "LogVerbose.txt");
            frmLogForm.UpdateLog("World name: " + strWorldName, false, true);
            #endregion
            
            #region get handles to world, chunk manager and block manager
            BetaWorld worldDest = BetaWorld.Create(@strFolder);
            worldDest.Level.LevelName = "Creating. Don't open until Mace is finished.";
            BetaChunkManager cmDest = worldDest.GetChunkManager();
            BlockManager bmDest = worldDest.GetBlockManager();
            bmDest.AutoLight = false;
            #endregion

            #region Determine themes
            // "how does this work, robson?"
            // well, I'm glad you asked!
            // we keep selecting a random unused checked theme, until they've all been used once.
            // after that, all other cities will have a random checked theme
            strCheckedThemes = RNG.ShuffleArray(strCheckedThemes);            
            for (int CurrentCityID = 0; CurrentCityID < TotalCities; CurrentCityID++)
            {
                if (CurrentCityID <= strCheckedThemes.GetUpperBound(0))
                {
                    worldCities[CurrentCityID].ThemeName = strCheckedThemes[CurrentCityID];
                }
                else
                {
                    worldCities[CurrentCityID].ThemeName = RNG.RandomItem(strCheckedThemes);
                }
                City.ThemeName = worldCities[CurrentCityID].ThemeName;
                worldCities[CurrentCityID].ChunkLength = GetThemeRandomXMLElementNumber("options", "city_size");
            }
            #endregion
            GenerateCityLocations(TotalCities, ChunksBetweenCities);

            int intRandomCity = RNG.Next(TotalCities);

            for (int CurrentCityID = 0; CurrentCityID < TotalCities; CurrentCityID++)
            {
                MakeCitySettings(frmLogForm, worldCities[CurrentCityID].ThemeName, CurrentCityID, strSelectedNPCs);
                GenerateCity.Generate(frmLogForm, worldDest, cmDest, bmDest, worldCities[CurrentCityID].x, worldCities[CurrentCityID].z, booExportSchematics, strUndergroundOres);
                #region set spawn point
                if (City.ID == intRandomCity)
                {
                    switch (strSpawnPoint)
                    {
                        case "Away from the cities":
                            worldDest.Level.Spawn = new SpawnPoint(0, 65, 0);
                            break;
                        case "Inside a random city":
                            worldDest.Level.Spawn = new SpawnPoint(((worldCities[intRandomCity].x + Chunks.CITY_RELOCATION_CHUNKS) * 16) + (City.MapLength / 2),
                                                                   65,
                                                                   ((worldCities[intRandomCity].z + Chunks.CITY_RELOCATION_CHUNKS) * 16) + (City.MapLength / 2));
                            break;
                        case "Outside a random city":
                            worldDest.Level.Spawn = new SpawnPoint(((worldCities[intRandomCity].x + Chunks.CITY_RELOCATION_CHUNKS) * 16) + (City.MapLength / 2),
                                                                    65,
                                                                    ((worldCities[intRandomCity].z + Chunks.CITY_RELOCATION_CHUNKS) * 16) + 2);
                            break;
                        default:
                            Debug.Fail("invalid spawn point");
                            break;
                    }
                    frmLogForm.UpdateLog("Spawn point set to " + worldDest.Level.Spawn.X + "," + worldDest.Level.Spawn.Y + "," + worldDest.Level.Spawn.Z, false, true);
                }
                #endregion
            }

            #region weather
#if RELEASE
            frmLogForm.UpdateLog("Setting weather", false, true);
            worldDest.Level.Time = RNG.Next(24000);
            if (RNG.NextDouble() < 0.2)
            {
                frmLogForm.UpdateLog("Rain", false, true);
                worldDest.Level.IsRaining = true;
                // one-quarter to three-quarters of a day
                worldDest.Level.RainTime = RNG.Next(6000, 18000);
                if (RNG.NextDouble() < 0.25)
                {
                    frmLogForm.UpdateLog("Thunder", false, true);
                    worldDest.Level.IsThundering = true;
                    worldDest.Level.ThunderTime = worldDest.Level.RainTime;
                }
            }
#endif
            #endregion

#if DEBUG
                MakeHelperChest(bmDest, worldDest.Level.Spawn.X + 2, worldDest.Level.Spawn.Y, worldDest.Level.Spawn.Z + 2);
#endif

            #region world details
            worldDest.Level.LevelName = strWorldName;
            frmLogForm.UpdateLog("Setting world type: " + strWorldType, false, true);
            switch (strWorldType.ToLower())
            {
                case "creative":
                    worldDest.Level.GameType = GameType.CREATIVE;
                    break;
                case "survival":
                    worldDest.Level.GameType = GameType.SURVIVAL;
                    break;
                case "hardcore":
                    worldDest.Level.GameType = GameType.SURVIVAL;
                    worldDest.Level.Hardcore = true;
                    break;
                default:
                    Debug.Fail("Invalidate world type selected.");
                    break;
            }
            frmLogForm.UpdateLog("World map features: " + booWorldMapFeatures.ToString(), false, true);
            worldDest.Level.UseMapFeatures = booWorldMapFeatures;
            if (strWorldSeed != String.Empty)
            {
                worldDest.Level.RandomSeed = strWorldSeed.ToJavaHashCode();
                frmLogForm.UpdateLog("Specified world seed: " + worldDest.Level.RandomSeed, false, true);
            }
            else
            {                
                worldDest.Level.RandomSeed = RNG.Next();
                frmLogForm.UpdateLog("Random world seed: " + worldDest.Level.RandomSeed, false, true);
            }
            worldDest.Level.LastPlayed = (DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks) / 10000;
            frmLogForm.UpdateLog("World time: " + worldDest.Level.LastPlayed, false, true);
            #endregion

            cmDest.Save();
            worldDest.Save();

            frmLogForm.UpdateLog("\nCreated the " + strWorldName + "!", false, false);
            frmLogForm.UpdateLog("It'll be at the top of your MineCraft world list.", false, false);

            frmLogForm.UpdateLog("Finished at " + DateTime.Now.ToLocalTime(), false, true);
        }
        private static void MakeCitySettings(frmMace frmLogForm, string strThemeName, int CityID, string strSelectedNPCs)
        {
            City.ClearAllCityData();

            City.ID = CityID;

            City.ThemeName = strThemeName;

            City.CityNamePrefix = GetThemeRandomXMLElement("options", "city_prefix");
            City.CityNamePrefixFilename = GetThemeRandomXMLElement("options", "city_prefix_file");
            City.CityNameSuffixFilename = GetThemeRandomXMLElement("options", "city_suffix_file");

            City.HasFarms = GetThemeRandomXMLElementBoolean("include", "farms");
            if (City.HasFarms)
            {
                City.FarmLength = Math.Max(2, GetThemeRandomXMLElementNumber("options", "farm_size"));
            }
            City.HasMoat = GetThemeRandomXMLElementBoolean("include", "moat");
            City.HasWalls = GetThemeRandomXMLElementBoolean("include", "walls");
            City.HasDrawbridges = GetThemeRandomXMLElementBoolean("include", "drawbridges");
            if (City.HasWalls)
            {
                City.HasGuardTowers = GetThemeRandomXMLElementBoolean("include", "guard_towers");
            }
            City.HasBuildings = GetThemeRandomXMLElementBoolean("include", "buildings");
            City.HasPaths = GetThemeRandomXMLElementBoolean("include", "paths");
            City.HasMineshaft = GetThemeRandomXMLElementBoolean("include", "mineshaft");
            City.HasEmblems = GetThemeRandomXMLElementBoolean("include", "emblems");
            City.HasOutsideLights = GetThemeRandomXMLElementBoolean("include", "outside_lights");
            City.HasGuardTowersAddition = GetThemeRandomXMLElementBoolean("include", "guard_towers_addition");
            City.HasFlowers = GetThemeRandomXMLElementBoolean("include", "flowers");
            City.HasSkyFeature = GetThemeRandomXMLElementBoolean("include", "sky_feature");
            City.HasStreetLights = GetThemeRandomXMLElementBoolean("include", "street_lights");
            City.HasTorchesOnWalkways = GetThemeRandomXMLElementBoolean("include", "torches_on_walkways");

            City.HasValuableBlocks = frmLogForm.chkValuableBlocks.Checked;
            City.HasItemsInChests = frmLogForm.chkItemsInChests.Checked;

            City.CityLength = Math.Max(5, GetThemeRandomXMLElementNumber("options", "city_size"));
            if (City.CityLength % 2 == 0)
            {
                City.CityLength++;
            }

            string strValue = String.Empty;
            strValue = GetThemeRandomXMLElement("options", "ground_block");
            City.GroundBlockID = Convert.ToInt32(strValue.Split('_')[0]);
            City.GroundBlockData = 0;
            if (strValue.Contains("_"))
            {
                City.GroundBlockData = Convert.ToInt32(strValue.Split('_')[1]);
            }

            if (City.HasMoat)
            {
                City.MoatType = GetThemeRandomXMLElement("options", "moat");
            }
            if (City.HasEmblems)
            {
                City.CityEmblemType = GetThemeRandomXMLElement("options", "emblem");
            }
            if (City.HasOutsideLights)
            {
                City.OutsideLightType = GetThemeRandomXMLElement("options", "outside_lights");
            }
            if (City.HasGuardTowers)
            {
                City.TowersAdditionType = GetThemeRandomXMLElement("options", "tower_addition");
            }
            if (City.HasFlowers)
            {
                City.FlowerSpawnPercent = GetThemeRandomXMLElementNumber("options", "flower_percent");
            }
            if (City.HasStreetLights)
            {
                City.StreetLightType = GetThemeRandomXMLElement("options", "street_lights");
            }
            if (City.HasWalls)
            {
                strValue = GetThemeRandomXMLElement("options", "wall_material");
                City.WallMaterialID = Convert.ToInt32(strValue.Split('_')[0]);
                City.WallMaterialData = 0;
                if (strValue.Contains("_"))
                {
                    City.WallMaterialData = Convert.ToInt32(strValue.Split('_')[1]);
                }
            }
            City.PathType = GetThemeRandomXMLElement("options", "path");
            City.NPCs = strSelectedNPCs;
        }
        private static bool GetThemeRandomXMLElementBoolean(string strSection, string strKey)
        {
            return Utils.RandomValueFromXMLElement(Path.Combine("Resources", "Themes", City.ThemeName + ".xml"), strSection, strKey).IsAffirmative();
        }
        private static int GetThemeRandomXMLElementNumber(string strSection, string strKey)
        {
            return Convert.ToInt32(Utils.RandomValueFromXMLElement(Path.Combine("Resources", "Themes", City.ThemeName + ".xml"), strSection, strKey));
        }
        private static string GetThemeRandomXMLElement(string strSection, string strKey)
        {
            return Utils.RandomValueFromXMLElement(Path.Combine("Resources", "Themes", City.ThemeName + ".xml"), strSection, strKey);
        }
        private static void MakeHelperChest(BlockManager bm, int x, int y, int z)
        {
            TileEntityChest tec = new TileEntityChest();
            tec.Items[0] = BlockHelper.MakeItem(ItemInfo.DiamondSword.ID, 1);
            tec.Items[1] = BlockHelper.MakeItem(ItemInfo.DiamondPickaxe.ID, 1);
            tec.Items[2] = BlockHelper.MakeItem(ItemInfo.DiamondShovel.ID, 1);
            tec.Items[3] = BlockHelper.MakeItem(ItemInfo.DiamondAxe.ID, 1);
            tec.Items[4] = BlockHelper.MakeItem(BlockInfo.Ladder.ID, 64);
            tec.Items[5] = BlockHelper.MakeItem(BlockInfo.Dirt.ID, 64);
            tec.Items[6] = BlockHelper.MakeItem(BlockInfo.Sand.ID, 64);
            tec.Items[7] = BlockHelper.MakeItem(BlockInfo.CraftTable.ID, 64);
            tec.Items[8] = BlockHelper.MakeItem(BlockInfo.Furnace.ID, 64);
            tec.Items[9] = BlockHelper.MakeItem(ItemInfo.Bread.ID, 64);
            tec.Items[10] = BlockHelper.MakeItem(BlockInfo.Torch.ID, 64);
            tec.Items[11] = BlockHelper.MakeItem(BlockInfo.Stone.ID, 64);
            tec.Items[12] = BlockHelper.MakeItem(BlockInfo.Chest.ID, 64);
            tec.Items[13] = BlockHelper.MakeItem(BlockInfo.Glass.ID, 64);
            tec.Items[14] = BlockHelper.MakeItem(BlockInfo.Wood.ID, 64);
            tec.Items[15] = BlockHelper.MakeItem(ItemInfo.Cookie.ID, 64);
            tec.Items[16] = BlockHelper.MakeItem(ItemInfo.RedstoneDust.ID, 64);
            tec.Items[17] = BlockHelper.MakeItem(BlockInfo.IronBlock.ID, 64);
            tec.Items[18] = BlockHelper.MakeItem(BlockInfo.DiamondBlock.ID, 64);
            tec.Items[19] = BlockHelper.MakeItem(BlockInfo.GoldBlock.ID, 64);
            bm.SetID(x, y, z, BlockInfo.Chest.ID);
            bm.SetTileEntity(x, y, z, tec);
        }

        static void GenerateCityLocations(int TotalCities, int ChunksBetweenCities)
        {
            int MapChunksLength = 50;

            for (int CityID = 0; CityID < TotalCities; CityID++)
            {
                bool IsValidCity = false;
                int Attempts = 0;
                do
                {
                    worldCities[CityID].x = RNG.Next(2, (MapChunksLength - worldCities[CityID].ChunkLength) - 1);
                    worldCities[CityID].z = RNG.Next(2, (MapChunksLength - worldCities[CityID].ChunkLength) - 1);
                    IsValidCity = true;
                    for (int CheckCityID = 0; CheckCityID < CityID && IsValidCity; CheckCityID++)
                    {
                        IsValidCity = !CitiesIntersect(worldCities[CityID], worldCities[CheckCityID], ChunksBetweenCities);
                    }
                    if (++Attempts > 50)
                    {
                        MapChunksLength += 10;
                        Attempts = 0;
                    }
                } while (!IsValidCity);
            }
        }
        static bool CitiesIntersect(WorldCity city1, WorldCity city2, int ChunksOfPadding)
        {
            return new Rectangle(city1.x, city1.z, city1.ChunkLength, city1.ChunkLength).IntersectsWith(
                   new Rectangle(city2.x - ChunksOfPadding, city2.z - ChunksOfPadding,
                                 city2.ChunkLength + (2 * ChunksOfPadding), city2.ChunkLength + (2 * ChunksOfPadding)));
        }
    
    }
}
