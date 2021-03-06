﻿/*
    Mace
    Copyright (C) 2011 Robson
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
using System.Windows.Forms;
using System.Diagnostics;

namespace Mace
{
    public partial class frmMace : Form
    {
        DateTime startTime;
        public frmMace()
        {
            InitializeComponent();
            try
            {
                picMace.Load(Path.Combine("Resources", "mace.png"));
                picNPC.Load(Path.Combine("Resources", "npc.jpg"));
            }
            catch (Exception)
            {
                MessageBox.Show("Could not find one of the resource files. Please close Mace and ensure you have extracted all of the files from the Mace archive.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            cmbCitySize.SelectedIndex = 0;
            cmbMoatType.SelectedIndex = 0;
            cmbCityEmblem.SelectedIndex = 0;
            cmbOutsideLights.SelectedIndex = 0;
            cmbTowerAddition.SelectedIndex = 0;
            cmbWallMaterial.SelectedIndex = 0;
            string[] strFiles = Directory.GetFiles("Resources", "Emblem*.txt");
            foreach (string strFile in strFiles)
            {
                string strFileName = strFile;
                strFileName = strFileName.Replace(".txt", String.Empty);
                strFileName = strFileName.Replace(Path.Combine("Resources", "Emblem "), String.Empty);
                cmbCityEmblem.Items.Add(strFileName);
            }
            Version ver = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            this.Text = String.Format("Mace v{0}.{1}.{2}", ver.Major, ver.Minor, ver.Build);
            ToolTip toolTip1 = new ToolTip();
            toolTip1.AutoPopDelay = 1000000;
            toolTip1.InitialDelay = 500;
            toolTip1.ReshowDelay = 500;
            toolTip1.ShowAlways = true;
            toolTip1.SetToolTip(this.lnkGhostdancerMobsForMace, "http://www.minecraftforum.net/topic/532831-173-mobs-for-mace-v051-npc-mod/");
            toolTip1.SetToolTip(this.lnkRisugamiModLoader, "http://www.minecraftforum.net/topic/75440-v173-risugamis-mods-recipe-book-updated/");
        }   
        private void tabOptions_KeyDown(object sender, KeyEventArgs e)
        {
            if (tabOptions.SelectedIndex == 1)
            {
                if (e.Alt)
                {
                    switch (e.KeyCode)
                    {
                        case Keys.C:
                            cmbCitySize.Focus();
                            break;
                        case Keys.M:
                            cmbMoatType.Focus();
                            break;
                        case Keys.E:
                            cmbCityEmblem.Focus();
                            break;
                        case Keys.O:
                            cmbOutsideLights.Focus();
                            break;
                        case Keys.F:
                            cmbTowerAddition.Focus();
                            break;
                        case Keys.A:
                            cmbWallMaterial.Focus();
                            break;
                        case Keys.N:
                            txtCityName.Focus();
                            break;
                        case Keys.S:
                            txtCitySeed.Focus();
                            break;
                        case Keys.W:
                            txtWorldSeed.Focus();
                            break;
                        case Keys.Z:
                            if (MessageBox.Show("World duplicater activated. Did you mean to?", "Look what has happened", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                GenerateCity.CropMaceWorld(this);
                                MessageBox.Show("Done");
                            }
                            break;
                    }
                }
            }
        }
        private void btnAbout_Click(object sender, EventArgs e)
        {
            frmAbout fA = new frmAbout();
            fA.ShowDialog();
        }
        private void btnGenerateCity_Click(object sender, EventArgs e)
        {
            tabOptions.SelectTab(tpLog);
            lblProgress.Visible = true;
            lblProgressBack.Visible = true;
            txtLog.Text = String.Empty;
            this.Cursor = Cursors.WaitCursor;
            UpdateProgress(0);
            this.Enabled = false;
            startTime = DateTime.Now;
            GenerateCity.Generate(this, txtCityName.Text, chkIncludeFarms.Checked, chkIncludeMoat.Checked, 
                                  chkIncludeWalls.Checked, chkIncludeDrawbridges.Checked,
                                  chkIncludeGuardTowers.Checked, chkIncludeBuildings.Checked, true,
                                  chkIncludeMineshaft.Checked, chkItemsInChests.Checked, chkValuableBlocks.Checked,
                                  chkIncludeSpawners.Checked, cmbCitySize.Text, cmbMoatType.Text, cmbCityEmblem.Text,
                                  cmbOutsideLights.Text, cmbTowerAddition.Text, cmbWallMaterial.Text,
                                  txtCitySeed.Text, txtWorldSeed.Text, chkExportSchematic.Checked);
            lblProgressBack.Visible = false;
            lblProgress.Visible = false;
            this.Enabled = true;
            this.Cursor = Cursors.Default;
        }
        public void UpdateLog(string strMessage)
        {
            TimeSpan duration = DateTime.Now - startTime;
//#if DEBUG
            //txtLog.Text += duration.TotalSeconds + "\r\n";
//#endif
            txtLog.Text += strMessage + "\r\n";
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.SelectionLength = 0;
            txtLog.Refresh();
            Application.DoEvents();
            startTime = DateTime.Now;
        }
        public void UpdateProgress(int intPercent)
        {
            lblProgress.Width = (lblProgressBack.Width * intPercent) / 100;
            lblProgress.Refresh();
            Application.DoEvents();
        }
        private void picMace_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                DateTime dtNow = new DateTime();
                dtNow = DateTime.Now;
                RandomHelper.SetSeed(dtNow.Millisecond);
                string strNames = String.Empty;
                for (int x = 0; x < 50; x++)
                {
                    string strStart = RandomHelper.RandomFileLine(Path.Combine("Resources", "CityAdj.txt"));
                    string strEnd = RandomHelper.RandomFileLine(Path.Combine("Resources", "CityNoun.txt"));
                    string strCityName = "City of " + strStart + strEnd;
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

        private void txtCityName_Enter(object sender, EventArgs e)
        {
            IntoCityNameBox();
        }
        private void txtCityName_Enter(object sender, MouseEventArgs e)
        {
            IntoCityNameBox();
        }
        private void IntoCityNameBox()
        {
            if (txtCityName.Text.ToLower() == "random")
            {
                txtCityName.SelectionStart = 0;
                txtCityName.SelectionLength = txtCityName.Text.Length;
            }
        }

        private void ShowHelp(object sender, HelpEventArgs hlpevent)
        {
            string strHelp = String.Empty;
            switch (((Control)sender).Name)
            {
                case "lblCitySize":
                case "cmbCitySize":
                    strHelp = "Excluding the farms, each edge of the city is roughly:\n\nVery small - 80 blocks\nSmall - 128 blocks\nMedium - 192 blocks\nLarge - 256 blocks\nVery large - 400 blocks\n\nRandomly sized cities will be between 128 and 256 blocks on each edge.";
                    break;
                case "lblMoatType":
                case "cmbMoatType":
                    strHelp = "The moat is outside the city walls and goes all around the city. Choose a lava moat to increase your \"Oh &$£#!\" moments.";
                    break;
                case "lblCityEmblem":
                case "cmbCityEmblem":
                    strHelp = "City emblems appear on the outside walls, next to the four city entrances.";
                    break;
                case "lblOutsideLights":
                case "cmbOutsideLights":
                    strHelp = "This refers to the lights on the outside of the city walls.";
                    break;
                case "lblTowerAddition":
                case "cmbTowerAddition":
                    strHelp = "Fire beacons or flags give more character to the city and help you see the city from a distance.";
                    break;
                case "lblWallMaterial":
                case "cmbWallMaterial":
                    strHelp = "This refers to the walls that go all around the city. Selecting \"Random\" will choose a random normal material (brick, cobblestone, sandstone, stone, wood planks).";
                    break;
                case "lblCityName":
                case "txtCityName":
                    strHelp = "Use all your available intelligence, sarcasm and humour to come up with a city name. Alternatively, leave blank to let Mace generate a name for you. Mace won't overwrite worlds with the specified name.";
                    break;
                case "lblCitySeed":
                case "txtCitySeed":
                case "lblWorldSeed":
                case "txtWorldSeed":
                    strHelp = "This can be anything or nothing. For example:\n\nYour pet's name (Middy).\nYour favourite colour (Purple).\nYour favourite Buffy character (Willow).\nThe amount of blueberries you've eaten today (Seven and a half).\nThe name of your parole officer (Victor).\nHow many unicorns you've taught to fly (3).\nThe last person you vomitted all over (Mike).\nYour favourite piano tuner from Ukraine (Mikhaylyna).";
                    break;

                case "chkIncludeFarms":
                    strHelp = "Mace creates a variety of farms outside the city. The farms types are wheat, cactus, mushroom, sugarcane and orchards.";
                    break;
                case "chkIncludeMoat":
                    strHelp = "The moat is outside the city walls and goes all around the city.";
                    break;
                case "chkIncludeDrawbridges":
                    strHelp = "Unchecking this will make it rather tricky to enter the city!";
                    break;
                case "chkIncludeGuardTowers":
                    strHelp = "There's a guard tower at each corner of the city walls. They provide a high vantage point to see into and away from the city. They also make it easier to see the city from a distance.";
                    break;
                case "chkIncludeWalls":
                    strHelp = "Mace will generate four walls around the city. Unfortunately the fourth wall is frequently broken.";
                    break;
                case "chkIncludeBuildings":
                    strHelp = "Want to create your own buildings? Uncheck this!";
                    break;
                case "chkIncludePaths":
                    strHelp = "The city will have paths between the buildings. There's two main routes through the city, which have lights on either side.";
                    break;
                case "chkIncludeMineshaft":
                    strHelp = "The mineshaft is a large network of tunnels under the city, which spans multiple levels. It is full of resources and monsters.";
                    break;
                case "chkValuableBlocks":
                    strHelp = "Unchecking this will turn gold, iron, diamond, obsidian and lapis blocks into wool blocks. No sheep will be hurt during this process.";
                    break;
                case "chkItemsInChests":
                    strHelp = "Usually chests will contain appropriate or random items, to add more flavour to the city. Uncheck this if you'd like all chests to be empty.";
                    break;
                case "txtLog":
                    strHelp = "Information about the city generation will appear here.";
                    break;

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
                    RandomHelper.SetSeed(randSeeds.Next());
                    MessageBox.Show("Thank you for submitting this request. We have now fired " + RandomHelper.RandomFileLine(Path.Combine("Resources", "HelpDepartment.txt")) + "\n\nYou monster.", "Requested granted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show(strHelp, "Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void lnkGhostdancerMobsForMace_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.minecraftforum.net/topic/532831-173-mobs-for-mace-v051-npc-mod/");
        }

        private void lnkRisugamiModLoader_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.minecraftforum.net/topic/75440-v173-risugamis-mods-recipe-book-updated/");
        }
    }
}
