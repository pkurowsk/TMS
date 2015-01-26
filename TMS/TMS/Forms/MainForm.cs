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
        MasterController _masterController;
        TrackingController _trackingController;

        public MainForm(MasterController c)
        {
            InitializeComponent();

            _masterController = c;
            c._picMinePlan = picMinePlan;

            _trackingController = new TrackingController(this, picMinePlan);

            lblSiteInfo.Text = MineSite.GetInstance().siteName;

            llblUsername.Text = User.GetInstance().username;
            lblUserType.Text = User.GetInstance().isAdmin ? "Administrator" : "Regular User";

            // Load routers into treeview
            tvAllRouters.BeginUpdate();

            foreach (Router router in MineSite.GetInstance().siteRouters)
            {
                TreeNode routerNode = new TreeNode(router.ToString());
                tvAllRouters.Nodes[0].Nodes.Add(routerNode);

            }
            tvAllRouters.Nodes[0].ExpandAll();
            tvAllRouters.Nodes[0].Text = MineSite.GetInstance().siteName;

            tvAllRouters.EndUpdate();

            _trackingController.AddAllRoutersToMap();
        }


        public void AddToLeftPanel(Form form)
        {
            form.TopLevel = false;
            splitMain.Panel1.Controls.Add(form);
            form.Show();
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

        private void msMain_Click(object sender, EventArgs e)
        {

        }
        
        private void picMinePlan_Paint(object sender, PaintEventArgs e)
        {
            // Draw grid over the image
            Graphics g = e.Graphics;
            int cellSize = 50;
            int numOfCells = picMinePlan.Width / cellSize;
            Pen p = new Pen(Color.Black);
            p.Width = 0.1f;

            // Horizontal lines
            for (int i = 1; i < numOfCells; i++)
            {
                g.DrawLine(p, 0, i * cellSize, numOfCells * cellSize, i * cellSize);
            }

            // Vertical lines
            for (int i = 1; i < numOfCells; i++)
            {
                g.DrawLine(p, i * cellSize, 0, i * cellSize, numOfCells * cellSize);
            }
        }

        private void btnRouters_Click(object sender, EventArgs e)
        {
            _masterController.OpenRouters();
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
        /// TODO Open a user account form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void llblUsername_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

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
                _trackingController.ShowMinerPosition(sender, null, router);
            }
        }

    }
}
