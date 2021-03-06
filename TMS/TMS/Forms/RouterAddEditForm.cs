﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TMS
{
    public partial class RouterAddEditForm : Form
    {
        MasterController _controller;

        Router router;

        public RouterAddEditForm(MasterController mc)
        {
            InitializeComponent();

            _controller = mc;

            LoadRouterIDs();

            btnEnter.Click += NewRouter;
        }

        public RouterAddEditForm(MasterController mc, Router r) : this(mc)
        {
            router = r;

            // Set to update mode with the router selected
            rbUpdate.Checked = true;
            lstRouters.SelectedIndex = lstRouters.Items.IndexOf(r);

            btnEnter.Click -= NewRouter;
            btnEnter.Click += UpdateRouter;


        }

        private void ClearInputs()
        {
            router = null;
            mtxtId.Text = "";
            txtAddr.Text = "";
            txtLocation.Text = "";
            mtxtX.Text = "000";
            mtxtY.Text = "000";
            chkBlocked.Checked = false;

            rbNew.Checked = true;
        }

        private void LoadRouterIDs() 
        {
            foreach (Router router in MineSite.GetInstance().siteRouters)   {
                lstRouters.Items.Add(router);
            }
           
        }

        private void NewRouter(object sender, EventArgs e)
        {
            if (mtxtId.Text == "" || mtxtX.Text == "" || mtxtY.Text == "")   
            {
                MessageBox.Show("Please enter all the required fields.", "Required Fields", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool didComplete = _controller.RouterCreate(   mtxtId.Text,
                                                        txtAddr.Text, txtLocation.Text,
                                                        Int32.Parse(mtxtX.Text), Int32.Parse(mtxtY.Text),
                                                        chkBlocked.Checked);

            if (didComplete)
            {
                ClearInputs();
                lstRouters.Items.Clear();
                LoadRouterIDs();
            }
        }

        public void SetRouterPosFromMap(int x, int y)
        {
            mtxtX.Text = "" + x;
            mtxtY.Text = "" + y;
        }

        private void UpdateRouter(object sender, EventArgs e)   
        {
            bool didComplete = _controller.RouterUpdate(   router,
                                                        mtxtId.Text,
                                                        txtAddr.Text, txtLocation.Text,
                                                        Int32.Parse(mtxtX.Text), Int32.Parse(mtxtY.Text),
                                                        chkBlocked.Checked);
            if (didComplete)
            {
                ClearInputs();
                lstRouters.Items.Clear();
                LoadRouterIDs();
            }
        }

        private void txtId_Leave(object sender, EventArgs e)
        {
            if (MineSite.GetInstance().ContainsRouter(mtxtId.Text) != null)
            {
                epRID.SetError(mtxtId, "This ID already exists.");
            }
            else
            {
                epRID.Clear();
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            _controller.ClosedRouterForm();
        }

        private void rbUpdate_CheckedChanged(object sender, EventArgs e)
        {
            if (rbUpdate.Checked)
            {
                mtxtId.Enabled = false;
                btnDelete.Enabled = true;
                btnEnter.Click -= NewRouter;
                btnEnter.Click += UpdateRouter;
            }
        }

        private void rbNew_CheckedChanged(object sender, EventArgs e)
        {
            if (rbNew.Checked)
            {
                mtxtId.Enabled = true;
                btnDelete.Enabled = false;
                btnEnter.Click -= UpdateRouter;
                btnEnter.Click += NewRouter;
            }
        }

        private void lstRouters_SelectedIndexChanged(object sender, EventArgs e)
        {
            router = (Router)(lstRouters.SelectedItem);
            if (router != null)
            {

                mtxtId.Text = router.routerId;
                txtAddr.Text = router.address;
                txtLocation.Text = router.location;
                mtxtX.Text = router.posX.ToString();
                mtxtY.Text = router.posY.ToString();
                chkBlocked.Checked = router.isBlocked;

                rbUpdate.Checked = true;
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            bool didComplete = _controller.RouterDelete((Router)lstRouters.SelectedItem);

            if (didComplete)
            {
                lstRouters.Items.RemoveAt(lstRouters.SelectedIndex);
            }
        }

        private void btnSelPos_Click(object sender, EventArgs e)
        {
            _controller.SetRouterPositionSetable();
        }

    }
}
