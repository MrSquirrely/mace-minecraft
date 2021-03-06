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
    along with this program.  If not, see <http://www.gnu.org/licenses/>
*/

using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using Substrate;
using Substrate.Entities;
using Substrate.TileEntities;

namespace Mace
{
    public partial class frmMace : Form
    {
        private const string URL_PROJECT_SITE = "http://code.google.com/p/mace-minecraft/";
        private const string URL_FORUM_TOPIC = "http://www.minecraftforum.net/topic/357201-mace-v11-random-city-generator/";
        private const string URL_ROBSON = "http://iceyboard.no-ip.org";
        private const string URL_CREDITS = "http://code.google.com/p/mace-minecraft/wiki/Credits";
        private const string URL_MOBS_FOR_MACE = "http://www.minecraftforum.net/topic/532831-173-mobs-for-mace-v051-npc-mod/";
        private const string URL_MOD_LOADER = "http://www.minecraftforum.net/topic/75440-v173-risugamis-mods-recipe-book-updated/";
        private const string URL_MEDIEVAL_BUILDING_BUNDLE = "http://www.planetminecraft.com/project/medieval-building-bundle-98192/";

        DateTime startTime;

        public frmMace()
        {
            InitializeComponent();
            try
            {
                picMace.Load(Path.Combine("Resources", "mace.png"));
                picGhostdancerNPC.Load(Path.Combine("Resources", "npc.jpg"));
                picAbout.Load(Path.Combine("Resources", "about.jpg"));
            }
            catch (Exception)
            {
                MessageBox.Show("Could not find one of the resource files. Please close Mace and ensure you have extracted all of the files from the Mace archive.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            RNG.SetRandomSeed();
            CreateSplash();
            Version ver = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            this.Text = String.Format("Mace v{0}.{1}.{2}", ver.Major, ver.Minor, ver.Build);
            ToolTip toolTip1 = new ToolTip();
            toolTip1.AutoPopDelay = 1000000;
            toolTip1.InitialDelay = 500;
            toolTip1.ReshowDelay = 500;
            toolTip1.ShowAlways = true;
            toolTip1.SetToolTip(this.lnkGhostdancerMobsForMace, URL_MOBS_FOR_MACE);
            toolTip1.SetToolTip(this.lnkRisugamiModLoader, URL_MOD_LOADER);
            toolTip1.SetToolTip(this.lnkProjectSite, URL_PROJECT_SITE);
            toolTip1.SetToolTip(this.lnkForumTopic, URL_FORUM_TOPIC);
            toolTip1.SetToolTip(this.lnkRobson, URL_ROBSON);
            toolTip1.SetToolTip(this.lnkCredits, URL_CREDITS);
            toolTip1.SetToolTip(this.lnkMedievalBuildingBundle, URL_MEDIEVAL_BUILDING_BUNDLE);
            #if DEBUG
                cmbWorldType.SelectedIndex = 0;
                numAmountOfCities.Value = 1;
            #else
                cmbWorldType.SelectedIndex = 1;
            #endif
            cmbSpawnPoint.SelectedIndex = 2;
            cmbNPCs.SelectedIndex = 1;
            ShowThemes();
            cbUndergroundOres.SelectedIndex = 2;
        }
        private void tabOptions_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Alt)
            {
                switch (tabOptions.SelectedTab.Text)
                {
                    case "Cities":
                        switch (e.KeyCode)
                        {
                            case Keys.T:
                                clbCityThemesToUse.Focus();
                                break;
                            case Keys.A:
                                numAmountOfCities.Focus();
                                numAmountOfCities.Select(0, numAmountOfCities.Value.ToString().Length);
                                break;
                            case Keys.M:
                                numMinimumChunksBetweenCities.Focus();
                                numMinimumChunksBetweenCities.Select(0, numMinimumChunksBetweenCities.Value.ToString().Length);
                                break;
                            case Keys.Z:
                                if (MessageBox.Show("World duplicater activated. Did you mean to?", "Look what has happened",
                                                    MessageBoxButtons.YesNo) == DialogResult.Yes)
                                {
                                    Utils.CropMaceWorld(this);
                                    MessageBox.Show("Done");
                                }
                                break;
                        }
                        break;
                    case "World":
                        switch (e.KeyCode)
                        {
                            case Keys.S:
                                cmbSpawnPoint.Focus();
                                break;
                            case Keys.Y:
                                cmbWorldType.Focus();
                                break;
                            case Keys.N:
                                txtWorldName.Focus();
                                txtWorldName.SelectAll();
                                break;
                            case Keys.E:
                                txtWorldSeed.Focus();
                                txtWorldSeed.SelectAll();
                                break;
                        }
                        break;
                }
            }
        }
        private void btnGenerateWorld_Click(object sender, EventArgs e)
        {
            if (clbCityThemesToUse.CheckedItems.Count == 0)
            {
                MessageBox.Show("You must select at least one city theme.", "No themes selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // setup for city generation
                tabOptions.SelectTab(tpLog);
                lblProgress.Visible = true;
                lblProgressBack.Visible = true;
                txtLogNormal.Text = String.Empty;
                txtLogVerbose.Text = String.Empty;
                this.Cursor = Cursors.WaitCursor;
                City.ID = 0;
                UpdateProgress(0);
                this.ControlBox = false;
                btnGenerateWorld.Enabled = false;
                startTime = DateTime.Now;
                GenerateWorld.Generate(this, txtWorldName.Text, txtWorldSeed.Text, cmbWorldType.Text,
                                       chkMapFeatures.Checked, (int)numAmountOfCities.Value, 
                                       clbCityThemesToUse.CheckedItems.OfType<string>().ToArray(),
                                       (int)numMinimumChunksBetweenCities.Value,
                                       cmbSpawnPoint.Text, chkExportCities.Checked,
                                       cmbNPCs.Text, cbUndergroundOres.Text);
                TimeSpan duration = DateTime.Now - startTime;
                UpdateLog("Completed in " + Math.Round(duration.TotalSeconds, 2) + " seconds", false, true);
                btnSaveLogNormal.Enabled = true;
                btnSaveLogVerbose.Enabled = true;
                // restore
                Version ver = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
                this.Text = String.Format("Mace v{0}.{1}.{2}", ver.Major, ver.Minor, ver.Build);
                lblProgressBack.Visible = false;
                lblProgress.Visible = false;
                btnGenerateWorld.Enabled = true;
                this.ControlBox = true;
                this.Cursor = Cursors.Default;
            }
        }
        private void picMace_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                MessageBox.Show(BlockInfo.Stone.ID.ToString());
                Debug.WriteLine((int)FenceGateState.OPEN);
                Debug.WriteLine((int)DoorState.SWUNG);
                DateTime dtNow = new DateTime();
                dtNow = DateTime.Now;
                RNG.SetSeed(dtNow.Millisecond);
                string[] strFlagFiles = Directory.GetFiles("Resources", "Flag_*.txt");
                string strFlag = RNG.RandomItemFromArray(strFlagFiles);
                string[] strFlagLines = File.ReadAllLines(strFlag);

                MessageBox.Show(String.Join(" ", strFlagLines));

                string strNames = String.Empty;
                for (int x = 0; x < 50; x++)
                {
                    string strStart = RNG.RandomFileLine(Path.Combine("Resources", "Adjectives.txt"));
                    string strEnd = RNG.RandomFileLine(Path.Combine("Resources", "Nouns.txt"));
                    string strCityName = strStart + strEnd;
                    strNames += strCityName + "\r\n";
                }
                MessageBox.Show(strNames);
            }
        }       
        private void cb_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            if (cb.Text.StartsWith("--"))
            {
                cb.SelectedIndex = 0;
            }
        }
        private void lnkGhostdancerMobsForMace_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(URL_MOBS_FOR_MACE);
        }
        private void lnkRisugamiModLoader_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(URL_MOD_LOADER);
        }
        private void lnkProjectSite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(URL_PROJECT_SITE);
        }
        private void lnkForumTopic_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(URL_FORUM_TOPIC);
        }
        private void lnkRobson_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(URL_ROBSON);
        }
        private void lnkCredits_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(URL_CREDITS);
        }
        private void lnkMedievalBuildingBundle_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(URL_MEDIEVAL_BUILDING_BUNDLE);
        }
        private void btnGenerateRandomWorldName_Click(object sender, EventArgs e)
        {
            txtWorldName.Text = Utils.GenerateWorldName();
        }
        private void btnGenerateRandomSeedIdea_Click(object sender, EventArgs e)
        {
            RNG.SetRandomSeed();
            switch (RNG.Next(4))
            {
                case 0:
                    txtWorldSeed.Text = "{" + RNG.RandomItem("Your name", "A friend's name",
                        "Your pet's name", "Your shoe size", "Your lucky number") + "}";
                    break;
                case 1:
                    txtWorldSeed.Text = "{Your favourite " + RNG.RandomItem(
                        "food", "place", "activity", "Buffy character", "film", "book", "website", "game",
                        "mathematician", "tv show", "subject", "colour", "letter", "breed of hippo",
                        "celebrity", "c# hashtable key", "animal", "drink", "minecraft block", "potion",
                        "tv character", "colonel", "film character", "shade of green", "cluedo weapon",
                        "minecraft enemy", "capital", "ore", "keen commander", "dancing ghost") + "}";
                    break;
                case 2:
                    txtWorldSeed.Text = RNG.RandomFileLine(Path.Combine("Resources", "Adjectives.txt")).ToLower().Trim();
                    break;
                case 3:
                    txtWorldSeed.Text = RNG.RandomFileLine(Path.Combine("Resources", "Nouns.txt")).ToLower().Trim();
                    break;
                default:
                    txtWorldSeed.Text = "";
                    break;
            }
        }
        private void btnSaveLogNormal_Click(object sender, EventArgs e)
        {
            SaveLogFile(btnSaveLogNormal.Tag.ToString(), txtLogNormal.Text);
        }
        private void btnSaveLogVerbose_Click(object sender, EventArgs e)
        {
            SaveLogFile(btnSaveLogVerbose.Tag.ToString(), txtLogVerbose.Text);
        }
        private void lblSplash_Click(object sender, EventArgs e)
        {
            //lblSplash.Text = RNG.RandomFileLine(Path.Combine("Resources", "Splash.txt"));
        }
        private void chkNPCs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbNPCs.Text.StartsWith("---"))
            {
                cmbNPCs.SelectedIndex = 3;
            }
            if (cmbNPCs.Text == "None")
            {
                lblSplash.Text = "Forever alone!";
            }
            else if (lblSplash.Text == "Forever alone!")
            {
                CreateSplash();
            }
            picGhostdancerNPC.Visible = cmbNPCs.Text.Contains("Ghostdancer");
            lblGhostdancerHelp.Visible = cmbNPCs.Text.Contains("Ghostdancer");
            lnkGhostdancerMobsForMace.Visible = cmbNPCs.Text.Contains("Ghostdancer");
            lnkRisugamiModLoader.Visible = cmbNPCs.Text.Contains("Ghostdancer");
        }
        private void btnNewTheme_Click(object sender, EventArgs e)
        {
            frmTheme frmThemeInstance = new frmTheme();
            frmThemeInstance.ShowDialog();
            ShowThemes();
        }

        private void ShowThemes()
        {
            clbCityThemesToUse.Items.Clear();
            string[] strCityThemes = Directory.GetFiles(Path.Combine("Resources", "Themes"), "*.xml");
            foreach (string strCityTheme in strCityThemes)
            {
                string strFriendly = strCityTheme.Replace(".xml", String.Empty).
                                       Replace(Path.Combine("Resources", "Themes"), String.Empty).Substring(1);
                clbCityThemesToUse.Items.Add(strFriendly, isChecked: true);
            }
        }
        private void CreateSplash()
        {
            DateTime dtNow = DateTime.Now;
            if (dtNow.Day == 24 && dtNow.Month == 12)
            {
                lblSplash.Text = "Merry Christmas Eve!";
            }
            else if (dtNow.Day == 25 && dtNow.Month == 12)
            {
                lblSplash.Text = "Merry Christmas!";
            }
            else if (dtNow.Day == 1 && dtNow.Month == 1)
            {
                lblSplash.Text = "Happy New Year!";
            }
            else if (dtNow.Day == 19 && dtNow.Month == 12)
            {
                lblSplash.Text = "Happy Birthday Polly!";
            }
            else if (dtNow.Day == 20 && dtNow.Month == 12)
            {
                lblSplash.Text = "Happy Birthday Rayven!";
            }
            else if (dtNow.Day == 29 && dtNow.Month == 7)
            {
                lblSplash.Text = "Happy Birthday Robson!";
            }
            else if (dtNow.Day == 2 && dtNow.Month == 6)
            {
                lblSplash.Text = "Happy Birthday Mace!";
            }
            else
            {
                lblSplash.Text = RNG.RandomFileLine(Path.Combine("Resources", "Splash.txt"));
            }
        }
        public void UpdateLog(string strMessage, bool booIndent, bool booVerboseMessage)
        {
            if (booIndent)
            {
                strMessage = new string(' ', 4) + strMessage;
            }
            strMessage += "\r\n";
            if (!booVerboseMessage)
            {
                txtLogNormal.Text += strMessage;
                txtLogNormal.SelectionStart = txtLogNormal.Text.Length;
                txtLogNormal.SelectionLength = 0;
                txtLogNormal.ScrollToCaret();
            }
            txtLogVerbose.Text += strMessage;
            txtLogVerbose.SelectionStart = txtLogVerbose.Text.Length;
            txtLogVerbose.SelectionLength = 0;
            txtLogVerbose.ScrollToCaret();
            this.Refresh();
            Application.DoEvents();
        }
        public void UpdateProgress(double dblPercent)
        {
            dblPercent = Math.Round((100.00 / (int)numAmountOfCities.Value) * (City.ID + dblPercent));
            lblProgress.Width = (lblProgressBack.Width * (int)dblPercent) / 100;
            Version ver = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            if (dblPercent >= 100)
            {
                this.Text = String.Format("Mace v{0}.{1}.{2}", ver.Major, ver.Minor, ver.Build);
            }
            else
            {
                this.Text = String.Format("{0}% - Mace v{1}.{2}.{3}", dblPercent, ver.Major, ver.Minor, ver.Build);
            }
            lblProgress.Refresh();
            Application.DoEvents();
        }
        private void ShowHelp(object sender, HelpEventArgs hlpevent)
        {
            string strHelp = String.Empty;
            switch (((Control)sender).Name)
            {
                case "btnAbout":
                    strHelp = "You just clicked a question mark with a question mark. The world will now implode.";
                    break;
                case "btnGenerateCity":
                    strHelp = "This button will create a new world in your MineCraft saves directory, with a randomly generated city at the spawn point.\n\nMace doesn't interact with MineCraft directly, so you don't need MineCraft open.";
                    break;
                case "picMace":
                    strHelp = "Hiring all those graphics artists was definitely worth it.";
                    break;
            }
            if (strHelp.Length == 0)
            {
                if (MessageBox.Show("Sorry, no help is available for this control :(\n\nWould you like to fire a random member of the Help Department?", "Help", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    Random randSeeds = new Random();
                    RNG.SetSeed(randSeeds.Next());
                    MessageBox.Show("Thank you for submitting this request. We have now fired " + RNG.RandomFileLine(Path.Combine("Resources", "HelpDepartment.txt")) + "\n\nYou monster.", "Requested granted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show(strHelp, "Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void SaveLogFile(string Filename, string Contents)
        {
            try
            {
                if (File.Exists(Filename))
                {
                    File.Delete(Filename);
                }
                File.WriteAllText(Filename, Contents);
                MessageBox.Show("Log saved to " + Filename, "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save the log file to " + Filename + "\n\nError:" + ex.Message,
                                "Save filed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}