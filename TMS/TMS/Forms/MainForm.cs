﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace TMS
{
    public partial class MainForm : Form
    {
        // Controllers

        /// <summary>
        /// Controls for all miscelaneous functions
        /// </summary>
        MasterController _masterController;

        /// <summary>
        /// Controls for all messaging functions
        /// </summary>
        MessagingController _messagingController;

        /// <summary>
        /// Controls for generating reports
        /// </summary>
        ReportsController _reportsController;

        /// <summary>
        /// Controller for all tracking functions
        /// </summary>
        TrackingController _trackingController;

        public MainForm(MasterController c)
        {
            InitializeComponent();

            _masterController = c;
            c._picMinePlan = picMinePlan;
        }

        public void AddNewCreatedRouter(Router router)
        {
            _trackingController.AddOneRouterToMap(router);

            router.OnUpdated += LoadRoutersToTree;
            router.OnDeleted += LoadRoutersToTree;
        }

        public void AddToLeftPanel(Form form)
        {
            form.TopLevel = false;
            splitMain.Panel1.Controls.Add(form);
            form.Show();
        }

        /// <summary>
        /// Initializes all components
        /// </summary>
        public void Init()
        {
            _trackingController = new TrackingController(this, picMinePlan);
            _messagingController = new MessagingController(this);
            _reportsController = new ReportsController();

            // Set labels
            llblSiteInfo.Text = MineSite.GetInstance().siteName;

            llblUsername.Text = User.GetInstance().username;
            lblUserType.Text = User.GetInstance().isAdmin ? "Administrator" : "Regular User";

            // Load routers into treeview
            LoadRoutersToTree();

            picMinePlan.Controls.Clear();
            _trackingController.AddAllRoutersToMap();

            LoadAllActiveMembers();

            // Set mapscale number incrementer
            nudMapScale.Value = (decimal)(MineSite.GetInstance().mapScale);
            nudMapScale.Font = new Font(nudMapScale.Font, FontStyle.Regular);
            btnSaveScale.Enabled = false;

            // Subscribe mainform to let it deal with member changes
            foreach (Member member in MineSite.GetInstance().siteMembers.Values)
            {
                member.OnInfoUpdated += LoadAllActiveMembers;
                member.OnPathUpdated += LoadRoutersToTree;
            }
        }

        /// <summary>
        /// Load all active miners to list
        /// </summary>
        public void LoadAllActiveMembers()
        {
            lstActiveMiners.Items.Clear();

            foreach (Member member in MineSite.GetInstance().siteMembers.Values)
            {
                if (!member.isVehicle && member.IsActive())
                {
                    lstActiveMiners.Items.Add(member);
                }
            }
        }

        /// <summary>
        /// Loads all the routers and members to the treeview
        /// </summary>
        public void LoadRoutersToTree()
        {
            tvAllRouters.BeginUpdate();

            tvAllRouters.Nodes[0].Nodes.Clear();

            foreach (Router router in MineSite.GetInstance().siteRouters)
            {
                TreeNode routerNode = new TreeNode(router.ToString());
                tvAllRouters.Nodes[0].Nodes.Add(routerNode);

                foreach (Member member in router.hasConnectedMembers)
                {
                    if (member.IsActive())
                    {
                        TreeNode memberNode = new TreeNode(member.ToString());
                        routerNode.Nodes.Add(memberNode);
                    }
                }

            }
            tvAllRouters.Nodes[0].ExpandAll();
            tvAllRouters.Nodes[0].Text = MineSite.GetInstance().siteName;

            tvAllRouters.EndUpdate();
        }

        public void MineSiteUpdated()
        {
            UpdateMineSiteList();
            LoadRoutersToTree();
            llblSiteInfo.Text = MineSite.GetInstance().siteName;
        }

        /// <summary>
        /// Updates the text in the bottom status bar
        /// </summary>
        /// <param name="text"></param>
        public void SetStatusText(string text)
        {
            statusTextConnected.Text = text;
        }

        public void UpdateMineSiteList()
        {
            this.siteTableAdapter.Fill(this.tMS_MineSiteDataSet.Site);
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }
        
        private void MainForm_LocationChanged(object sender, EventArgs e)
        {
            if (_trackingController != null)
                _trackingController.HideRouterForm();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (_trackingController != null)
                _trackingController.HideRouterForm();
            
        }

        private void miExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        
        private void miLoadMap_Click(object sender, EventArgs e)
        {
            _masterController.LoadMap(picMinePlan);
        }
        
        private void picMinePlan_Paint(object sender, PaintEventArgs e)
        {
            _trackingController.DrawGridLines(e.Graphics);
        }

        private void btnRouters_Click(object sender, EventArgs e)
        {
            _masterController.OpenRoutersForm();
        }

        private void btnLoadMap_Click(object sender, EventArgs e)
        {
            _masterController.LoadMap(picMinePlan);
        }

        private void picMinePlan_Click(object sender, EventArgs e)
        {
            _trackingController.HideRouterForm();
        }

        /// <summary>
        /// Open a user account form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void llblUsername_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            _masterController.OpenUsersForm();
        }

        /// <summary>
        /// Open form to enter miner details
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMember_Click(object sender, EventArgs e)
        {
            _masterController.OpenCreateMember();
        }

        private void tvAllRouters_AfterSelect(object sender, TreeViewEventArgs e)
        {
            Router router = MineSite.GetInstance().ContainsRouter(e.Node.Text.Split(',')[0]);

            if (router != null)
            {
                _trackingController.ShowMemberPosition(sender, null, router);
            }
            else
            {
                router = MineSite.GetInstance().ContainsRouter(e.Node.Parent.Text.Split(',')[0]);

                if (router != null)
                {
                    _trackingController.ShowMemberPosition(sender, null, router);
                }
            }
        }

        private void nudMapScale_ValueChanged(object sender, EventArgs e)
        {
            MineSite.GetInstance().UpdateScale((float)(nudMapScale.Value));

            nudMapScale.Font = new Font(nudMapScale.Font, FontStyle.Bold);
            btnSaveScale.Enabled = true;
        }

        private void btnSensor_Click(object sender, EventArgs e)
        {
            _masterController.OpenSensors();
        }

        /// <summary>
        /// Show the router form of where the member is after clicking on a member
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lstActiveMiners_SelectedIndexChanged(object sender, EventArgs e)
        {
            Member member = (Member)lstActiveMiners.SelectedItem;
            if (member != null)
            {
                _trackingController.HideRouterForm();

                _trackingController.SetCurrentDrawMember(member);

                alertToolStripMenuItem.Text = "Alert " + member.fName + " " + member.lName;
            }
        }

        private void btnTags_Click(object sender, EventArgs e)
        {
            _masterController.OpenCreateTag();
        }

        private void btnSaveScale_Click(object sender, EventArgs e)
        {
            bool didComplete = _masterController.MineSiteUpdate();

            if (didComplete)
            {
                btnSaveScale.Enabled = false;
                nudMapScale.Font = new Font(nudMapScale.Font, FontStyle.Regular);
            }
        }

        private void btnViewMessages_Click(object sender, EventArgs e)
        {
            _messagingController.OpenMessageView();
        }

        private void btnSendMessage_Click(object sender, EventArgs e)
        {
            _messagingController.OpenMessageSend();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // TODO: This line of code loads data into the 'tMS_MineSiteDataSet.Site' table. You can move, or remove it, as needed.
            this.siteTableAdapter.Fill(this.tMS_MineSiteDataSet.Site);
            // TODO: This line of code loads data into the 'tMS_DatabaseDataSet2.Members' table. You can move, or remove it, as needed.
            this.membersTableAdapter1.Fill(this.tMS_DatabaseDataSet2.Members);
            // TODO: This line of code loads data into the 'tMS_DatabaseDataSet.Members' table. You can move, or remove it, as needed.
            this.membersTableAdapter.Fill(this.tMS_DatabaseDataSet.Members);
            // TODO: This line of code loads data into the 'tMS_DatabaseDataSet1.Routers' table. You can move, or remove it, as needed.
            this.routersTableAdapter.Fill(this.tMS_DatabaseDataSet1.Routers);

        }

        private void llblSiteInfo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            _masterController.OpenMineSiteForm();
        }

        private void alertToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you would like to alert this member?", "Confirm Alert", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                _messagingController.SendMessage(false, "");
            }
        }

        private void broadcastAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Open message send window with broadcast mode set
            _messagingController.OpenMessageSend(true);
        }

        private void btnAllMiners_Click(object sender, EventArgs e)
        {
            _trackingController.OpenAllMinersView();
        }

        protected override void Dispose(bool disposing)
        {
            _trackingController.Dispose();
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            _trackingController.OpenTestForm();
        }

        private void btnRepMembers_Click(object sender, EventArgs e)
        {
            _reportsController.MembersReport();
        }

        private void btnRepRouter_Click(object sender, EventArgs e)
        {
            _reportsController.RoutersReport();
        }

        private void btnReptag_Click(object sender, EventArgs e)
        {
            _reportsController.TagsReport();
        }

        private void btnRepTWM_Click(object sender, EventArgs e)
        {
            _reportsController.TWMinersReport(cboMinerReport.SelectedValue.ToString());
        }

        private void btnLoadSite_Click(object sender, EventArgs e)
        {
            if (!cboSites.SelectedValue.ToString().Equals(MineSite.GetInstance().siteId.ToString()))
            {
                _masterController.OpenMainForm((int)cboSites.SelectedValue);
            }
            else
            {
                MessageBox.Show("This site is already loaded.");
            }
        }

        private void btnEDMessages_Click(object sender, EventArgs e)
        {
            _trackingController.OpenEDMessagesView();
        }
    }
}
