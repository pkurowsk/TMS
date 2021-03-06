﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Windows.Forms;
using System.Drawing;

using System.Data.SqlClient;
using System.Security.Cryptography;

namespace TMS
{
    public class MasterController
    {
        LoginForm _loginForm;
        MainForm _mainForm;

        // Forms used for data entry
        MemberAddEditForm _minerForm;
        MineSiteEditForm _mineSiteForm;
        RouterAddEditForm _routerForm;
        SensorAddEditForm _sensorForm;
        ShiftAddEditForm _shiftForm;
        TagAddEditForm _tagForm;
        UserEditForm _userEditForm;
        UserAddForm _userAddForm;

        public PictureBox _picMinePlan { private get;  set; }

        bool isSelectingPos = false;

        public MasterController(LoginForm lf)
        {
            _loginForm = lf;

        }

        /// <summary>
        /// Updates the three shifts of the member to in the database.
        /// </summary>
        /// <param name="member">The member whom to assign the shifts to.</param>
        /// <param name="shift">The three shifts to be assigned</param>
        /// <returns></returns>
        public bool AssignShift(Member member, Shift[] shift)
        {
            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdDelete = "DELETE FROM Shifts WHERE memberNo=@memberNo";
                string cmdString = "INSERT INTO Shifts VALUES(@start, @end, @memberNo)";

                sqlCon.Open();

                SqlCommand[] oCmd = new SqlCommand[shift.Length + 1];

                oCmd[0] = new SqlCommand(cmdDelete, sqlCon);
                oCmd[0].Parameters.AddWithValue("@memberNo", member.memberId);

                for (int i = 1; i < shift.Length + 1; i++)
                {
                    oCmd[i] = new SqlCommand(cmdString, sqlCon);
                    oCmd[i].Parameters.AddWithValue("@start", shift[i-1].startTime.ToString("HH:mm:00"));
                    oCmd[i].Parameters.AddWithValue("@end", shift[i-1].endTime.ToString("HH:mm:00"));
                    oCmd[i].Parameters.AddWithValue("@memberNo", member.memberId);
                }

                try
                {
                    for (int i = 0; i < shift.Length + 1; i++)
                    {
                        int rows = oCmd[i].ExecuteNonQuery();
                    }

                    return true;
                }
                catch (SqlException e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return false;
                }
            }
        }

        /// <summary>
        /// Authenticates the username and password
        /// 
        /// Returns 1 if username is invalid
        /// Returns 2 if password does not match username
        /// Returns 0 if successful
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public int Authenticate(string username, string password)
        {
            // Get salt from database
            string fName = "", lName = "";
            string salt = "";
            string passHash = "";
            bool isAdmin = false;

            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdString = "SELECT * FROM Users WHERE username=@username";

                SqlCommand oCmd = new SqlCommand(cmdString, sqlCon);
                oCmd.Parameters.AddWithValue("@username", username);

                sqlCon.Open();
                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    // Check if username is found
                    if (!oReader.HasRows)
                    {
                        return 1;
                    }

                    while (oReader.Read())
                    {
                        fName = oReader["fName"].ToString();
                        lName = oReader["lName"].ToString();
                        isAdmin = oReader["isAdmin"].ToString().Equals("True") ? true : false;

                        salt = oReader["salt"].ToString();
                        passHash = oReader["password"].ToString();
                    }
                }
            }

            string hashedPass = HashPassword(salt, password);

            if (!hashedPass.Equals(passHash))
            {
                return 2;
            }

            // Initialize user 
            User.GetInstance().Init(username, fName, lName, isAdmin);

            return 0;
        }

        /// <summary>
        /// To be called from the router form when it is closed. Resets the i case the user was selecting a router position.
        /// </summary>
        public void ClosedRouterForm()
        {
            _routerForm.Dispose();
            SelectRouterPosClick(null, null);
        }

        /// <summary>
        /// To be called from the shift form so that the member creation form can be reenabled.
        /// </summary>
        public void ClosedShiftForm()
        {
            _minerForm.Enabled = true;
        }

        /// <summary>
        /// To be called from the UserAddForm so the UserEditForm can be shown again
        /// </summary>
        public void ClosedUserAddForm()
        {
            _userAddForm.Dispose();
            _userEditForm.Show();
        }

        /// <summary>
        /// Returns a list of all the names of all the mine sites
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllMineSites()
        {
            List<string> mineSites = new List<string>();

            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdString = "SELECT siteName FROM Site ORDER BY siteName";

                SqlCommand oCmd = new SqlCommand(cmdString, sqlCon);

                sqlCon.Open();
                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        mineSites.Add(oReader["siteName"].ToString());
                    }
                }
            }

            return mineSites;
        }

        /// <summary>
        /// Returns a list of all tags and whether or not they are assigned to a miner
        /// </summary>
        /// <returns>A Dictionary of all tags and whether or not they are assigned to a miner</returns>
        public Dictionary<string, bool> GetAllTagsAssigned()
        {
            Dictionary<string, bool> tags = new Dictionary<string, bool>();

            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdString = "SELECT Id, CASE WHEN EXISTS(SELECT * FROM Members where tagId=Id) THEN 1 ELSE 0 END AS 'isAssigned' FROM Tags";

                SqlCommand oCmd = new SqlCommand(cmdString, sqlCon);

                sqlCon.Open();
                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        tags.Add(oReader["Id"].ToString(), oReader["isAssigned"].ToString().Equals("1") ? true : false);
                    }
                }
            }

            return tags;
        }

        /// <summary>
        /// Returns a list of all usernames
        /// </summary>
        /// <returns>A list of all registered usernames</returns>
        public List<string> GetAllUsernames()
        {
            List<string> usernames = new List<string>();

            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdString = "SELECT username FROM Users";

                SqlCommand oCmd = new SqlCommand(cmdString, sqlCon);

                sqlCon.Open();
                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        usernames.Add(oReader["username"].ToString());
                    }
                }
            }

            return usernames;
        }

        #region Password Hashing

        /// <summary>
        /// Creates a random salt for the database to increase passsword security.
        /// </summary>
        /// <returns></returns>
        private string GenerateSalt()
        {
            string salt = "";

            Random random = new Random(unchecked((int)DateTime.Now.Ticks));
            if (random != null)
            {
                byte[] saltBytes = new byte[16];
                new RNGCryptoServiceProvider().GetBytes(saltBytes);

                //salt = Encoding.UTF8.GetString(saltBytes);
                salt = Convert.ToBase64String(saltBytes);
            }
            
            return salt;
        }

        /// <summary>
        /// Uses SHA 256 to has the password and return it in base 64
        /// </summary>
        /// <param name="salt"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private string HashPassword(string salt, string password)
        {
            byte[] passBytes = Encoding.UTF8.GetBytes(salt + password);

            SHA256 hasherSHA256 = SHA256Managed.Create();
            byte[] hashValue = hasherSHA256.ComputeHash(passBytes);

            string hashedPass = "";
            foreach (byte x in hashValue)
            {
                hashedPass += String.Format("{0:x2}", x);
            }

            return hashedPass;
        }

        #endregion

        /// <summary>
        /// Loads all info into MineSite
        /// </summary>
        private void LoadAllSiteInfo(int siteId)   
        {
            // Load site info
            string siteName = "", mapAddr = "";
            float mapScale = 0;
            List<Router> routers = new List<Router>();
            Dictionary<string, Member> members = new Dictionary<string, Member>();
            List<string> sensors = new List<string>();

            // Load routers and members
            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdString = "SELECT * FROM Site WHERE Id=@siteId";
                SqlCommand oCmd = new SqlCommand(cmdString, sqlCon);
                oCmd.Parameters.AddWithValue("@siteId", siteId);

                sqlCon.Open();
                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        siteId = Int32.Parse(oReader["Id"].ToString());
                        siteName = oReader["siteName"].ToString();
                        mapAddr = oReader["localMapFileAddr"].ToString();
                        mapScale = float.Parse(oReader["mapScale"].ToString());
                    }
                }
                Dictionary<string, Router> routerMap = new Dictionary<string, Router>();
                Dictionary<string, Member> memberMap = new Dictionary<string, Member>();


                // Load all routers
                cmdString = "SELECT * FROM Routers WHERE siteId = @siteId";
                oCmd = new SqlCommand(cmdString, sqlCon);
                oCmd.Parameters.AddWithValue("@siteId", siteId);

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        Router router = new Router(oReader["Id"].ToString(),
                            oReader["address"].ToString(),
                            oReader["location"].ToString(),
                            Int32.Parse(oReader["x"].ToString()),
                            Int32.Parse(oReader["y"].ToString()),
                            bool.Parse(oReader["isBlocked"].ToString()));

                        routers.Add(router);
                        routerMap.Add(router.routerId, router);
                    }
                }

                // Load all members and shifts
                cmdString = "SELECT * FROM Members m, Shifts s WHERE siteId = @siteId AND m.memberNo=s.memberNo";
                oCmd = new SqlCommand(cmdString, sqlCon);
                oCmd.Parameters.AddWithValue("@siteId", siteId);

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    string lastId = "";
                    Member member = null;
                    while (oReader.Read())
                    {
                        if (lastId.Equals(oReader["memberNo"].ToString()))
                        {
                            member.assignedShifts.Add(new Shift(
                                DateTime.Parse(oReader["start"].ToString()),
                                DateTime.Parse(oReader["end"].ToString())));
                        }
                        else
                        {
                            lastId = oReader["memberNo"].ToString();

                            member = new Member(oReader["memberNo"].ToString(),
                                oReader["fName"].ToString(),
                                oReader["mName"].ToString(),
                                oReader["lName"].ToString(),
                                oReader["address"].ToString(),
                                oReader["province"].ToString(),
                                oReader["city"].ToString(),
                                Int32.Parse(oReader["pinNo"].ToString()),
                                oReader["phoneNo"].ToString(),
                                oReader["mobileNo"].ToString(),
                                oReader["isVehicle"].ToString() == "1" ? true : false,
                                oReader["tagId"].ToString());

                            member.assignedShifts.Add(new Shift(
                                DateTime.Parse(oReader["start"].ToString()),
                                DateTime.Parse(oReader["end"].ToString())));

                            members.Add(member.memberId, member);
                            memberMap.Add(member.memberId, member);
                        }
                    }
                }

                // Load all sensors
                cmdString = "SELECT * FROM Sensors";
                oCmd = new SqlCommand(cmdString, sqlCon);

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        sensors.Add(oReader["Id"].ToString());
                    }
                }

                cmdString = "SELECT * FROM PathElement pe, Routers r WHERE pe.routerId=r.Id AND r.siteId=@siteId ORDER BY timeVisited DESC";
                oCmd = new SqlCommand(cmdString, sqlCon);
                oCmd.Parameters.AddWithValue("@siteId", siteId);

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        Member member = memberMap[oReader["memberId"].ToString()];
                        Router router = routerMap[oReader["routerId"].ToString()];

                        // Use the member map and router map from above to create each miner's path
                        member.path.AddLast(router);

                        // Add to router's connected members if this is the member's first router
                        if (member.path.Count == 1)
                        {
                            router.hasConnectedMembers.AddLast(member);
                        }
                    }
                }
            }

            MineSite.GetInstance().Init(siteId, siteName, mapAddr, mapScale, routers, members, sensors);
        }

        /// <summary>
        /// Opens a dialog to load a jpg, jpeg, or bmp image of map data
        /// </summary>
        public void LoadMap(PictureBox picMinePlan)
        {
            Stream imageStream = null;
            OpenFileDialog openImageFileDialog = new OpenFileDialog();

            openImageFileDialog.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
            openImageFileDialog.Filter = "Image Files (*.bmp, *.jpg, *.jpeg)|*.bmp;*.jpg;*.jpeg";
            openImageFileDialog.FilterIndex = 1;
            openImageFileDialog.RestoreDirectory = true;

            if (openImageFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if ((imageStream = openImageFileDialog.OpenFile()) != null)
                    {
                        using (imageStream)
                        {
                            Image image = Image.FromStream(imageStream);
                            picMinePlan.Image = image;

                            MineSite.GetInstance().localMapFileAddr = openImageFileDialog.FileName;
                            MineSiteUpdate();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not open file. \n", ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Loads the image at the file path into the picture box.
        /// </summary>
        /// <param name="picMinePlan"></param>
        /// <param name="filePath"></param>
        public void LoadMap(PictureBox picMinePlan, string filePath)
        {
            Stream imageStream = null;
            try
            {
                if (filePath.Equals(""))
                {
                    throw new FileNotFoundException();
                }

                imageStream = new FileStream(filePath, FileMode.Open);
            }
            catch (FileNotFoundException e)
            {
                MessageBox.Show("The map file associated with this mine site could not be found.", "Map File Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                LoadMap(picMinePlan);
                
                return;
            }

            try
            {
                using (imageStream)
                {
                    Image image = Image.FromStream(imageStream);
                    picMinePlan.Image = image;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: Could not open file. \n", ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
        }

        #region Open Object Creation Forms

        /// <summary>
        /// Opens the assign shift form to enter shift for a member
        /// </summary>
        public void OpenAssignShift(Member member)
        {
            if (_shiftForm == null || _shiftForm.Visible == false)
            {
                _minerForm.Enabled = false;
                _shiftForm = new ShiftAddEditForm(this, member);
                _shiftForm.Show();
            }
        }

        /// <summary>
        /// Opens member form to create / edit / delete members
        /// </summary>
        public void OpenCreateMember()
        {
            if (_minerForm == null || _minerForm.Visible == false)
            {
                _minerForm = new MemberAddEditForm(this);
                if (!_minerForm.IsDisposed)
                {
                    _minerForm.Show();
                }
            }
        }

        /// <summary>
        /// Open tag form
        /// </summary>
        public void OpenCreateTag()
        {
            if (_tagForm == null || _tagForm.Visible == false)
            {
                _tagForm = new TagAddEditForm(this);
                _tagForm.Show();
            }
        }

        /// <summary>
        /// Opens user creation form
        /// </summary>
        public void OpenCreateUser()
        {
            if (_userAddForm == null || _userAddForm.Visible == false)
            {
                _userAddForm = new UserAddForm(this);
                _userAddForm.Show();

                _userEditForm.Hide();

            }
        }

        /// <summary>
        /// Closes the login form and opens the main form
        /// </summary>
        public void OpenMainForm(int siteId)
        {
            _loginForm.Hide();

            if (_mainForm != null)
            {
                MineSite.GetInstance().Dispose();
            }
            else
            {
                _mainForm = new MainForm(this);
            }

            LoadAllSiteInfo(siteId);

            _mainForm.Init();
            _mainForm.Show();

            LoadMap(_picMinePlan, MineSite.GetInstance().localMapFileAddr);
        }

        /// <summary>
        /// Open a form to edit mine site info
        /// </summary>
        public void OpenMineSiteForm()
        {
            if (_mineSiteForm == null || _mineSiteForm.Visible == false)
            {
                _mineSiteForm = new MineSiteEditForm(this);
                _mineSiteForm.Show();

            }
        }

        /// <summary>
        /// Opens the form for adding / editing routers and adds it to the left pane of the main form.
        /// </summary>
        public void OpenRoutersForm()
        {
            if (_routerForm == null || _routerForm.Visible == false)
            {
                _routerForm = new RouterAddEditForm(this);
                _mainForm.AddToLeftPanel(_routerForm);

            }
        }

        /// <summary>
        /// Opens the form for adding / deleting sensors.
        /// </summary>
        public void OpenSensors()
        {
            if (_sensorForm == null || _sensorForm.Visible == false)
            {
                _sensorForm = new SensorAddEditForm(this);
                _sensorForm.Show();
            }

        }

        /// <summary>
        /// Opens the form for creating / editing users
        /// </summary>
        public void OpenUsersForm()
        {
            if (_userEditForm == null || _userEditForm.Visible == false)
            {
                _userEditForm = new UserEditForm(this);
                _userEditForm.Show();
            }
        }

        #endregion

        /// <summary>
        /// Creates a member object in the database and adds it to MineSite
        /// </summary>
        public bool MemberCreate(string memberNo, 
            string fName, string mName, string lName,
            string addr, string prov, string city, int pinNo, 
            string phoneNo, string mobileNo, bool isVehicle, 
            string tagId)
        {
            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdString = 
                    "INSERT INTO Members(memberNo, fName, mName, lName, address, province, city, pinNo, phoneNo, mobileNo, isVehicle,  tagId, siteId) " + 
                                 "VALUES(@memberNo, @fName, @mName, @lName, @address, @province, @city, @pinNo, @phoneNo, @mobileNo, @isVehicle,  @tagId, @siteId)";

                sqlCon.Open();
                SqlCommand oCmd = new SqlCommand(cmdString, sqlCon);
                oCmd.Parameters.AddWithValue("@memberNo", memberNo);
                oCmd.Parameters.AddWithValue("@fName", fName);
                oCmd.Parameters.AddWithValue("@mName", mName);
                oCmd.Parameters.AddWithValue("@lName", lName);
                oCmd.Parameters.AddWithValue("@address", addr);
                oCmd.Parameters.AddWithValue("@province", prov);
                oCmd.Parameters.AddWithValue("@city", city);
                oCmd.Parameters.AddWithValue("@pinNo", pinNo);
                oCmd.Parameters.AddWithValue("@phoneNo", phoneNo);
                oCmd.Parameters.AddWithValue("@mobileNo", mobileNo);
                oCmd.Parameters.AddWithValue("@isVehicle", isVehicle ? 1 : 0);
                oCmd.Parameters.AddWithValue("@tagId", tagId);
                oCmd.Parameters.AddWithValue("@siteId", MineSite.GetInstance().siteId);

                try
                {
                    int rows = oCmd.ExecuteNonQuery();
                    Member member = new Member( memberNo,
                                                fName, mName, lName,
                                                addr, prov, city, pinNo,
                                                phoneNo, mobileNo, isVehicle,
                                                tagId);
                    MineSite.GetInstance().siteMembers.Add(member.memberId, member);

                    Shift[] newShifts = { new Shift(new DateTime(0), new DateTime(0)),
                                 new Shift(new DateTime(0), new DateTime(0)),
                                 new Shift(new DateTime(0), new DateTime(0))};

                    AssignShift(member, newShifts);

                    member.OnInfoUpdated += _mainForm.LoadAllActiveMembers;
                    member.OnPathUpdated += _mainForm.LoadRoutersToTree;
                    _mainForm.LoadAllActiveMembers();

                    return true;
                }
                catch (SqlException e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return false;
                }

            }
        }

        /// <summary>
        /// Deletes the member from the database
        /// </summary>
        /// <param name="member"></param>
        /// <returns>Whether the function completed successfully</returns>
        public bool MemberDelete(Member member)
        {
            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdString = "DELETE FROM Members WHERE memberNo=@Id";

                sqlCon.Open();
                SqlCommand oCmd = new SqlCommand(cmdString, sqlCon);
                oCmd.Parameters.AddWithValue("@Id", member.memberId);

                try
                {
                    int rows = oCmd.ExecuteNonQuery();

                    MineSite.GetInstance().siteMembers.Remove(member.memberId);
                    member.Dispose();

                    return true;
                }
                catch (SqlException e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return false;
                }

            }
        }

        /// <summary>
        /// Composes an update statement for Routers table
        /// </summary>
        /// <param name="member"></param>
        /// <param name="memberNo"></param>
        /// <param name="fName"></param>
        /// <param name="mName"></param>
        /// <param name="lName"></param>
        /// <param name="addr"></param>
        /// <param name="province"></param>
        /// <param name="city"></param>
        /// <param name="pinNo"></param>
        /// <param name="phoneNo"></param>
        /// <param name="mobileNo"></param>
        /// <param name="isVehicle"></param>
        /// <param name="tagId"></param>
        /// <returns>Whether the function completed or not.</returns>
        public bool MemberUpdate(Member member, string memberNo,
            string fName, string mName, string lName,
            string addr, string province, string city, int pinNo,
            string phoneNo, string mobileNo, bool isVehicle,
            string tagId)
        {
            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdString = "UPDATE Members SET fName=@fName, mName=@mName, lName=@lName, address=@address, province=@province, city=@city, pinNo=@pinNo, phoneNo=@phoneNo, mobileNo=@mobileNo, isVehicle=@isVehicle, tagId=@tagId WHERE memberNo=@memberNo";

                sqlCon.Open();
                SqlCommand oCmd = new SqlCommand(cmdString, sqlCon);
                oCmd.Parameters.AddWithValue("@memberNo", memberNo);
                oCmd.Parameters.AddWithValue("@fName", fName);
                oCmd.Parameters.AddWithValue("@mName", mName);
                oCmd.Parameters.AddWithValue("@lName", lName);
                oCmd.Parameters.AddWithValue("@address", addr);
                oCmd.Parameters.AddWithValue("@province", province);
                oCmd.Parameters.AddWithValue("@city", city);
                oCmd.Parameters.AddWithValue("@pinNo", pinNo);
                oCmd.Parameters.AddWithValue("@phoneNo", phoneNo);
                oCmd.Parameters.AddWithValue("@mobileNo", mobileNo);
                oCmd.Parameters.AddWithValue("@isVehicle", isVehicle ? 1 : 0);
                oCmd.Parameters.AddWithValue("@tagId", tagId);
                
                try
                {
                    int rows = oCmd.ExecuteNonQuery();

                    member.Update(  memberNo,
                                    fName, mName, lName,
                                    addr, province, city, pinNo,
                                    phoneNo, mobileNo, isVehicle,
                                    tagId);

                    return true;
                }
                catch (SqlException e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return false;
                }
            }
        }

        /// <summary>
        /// Adds a new MineSite to the database
        /// </summary>
        /// <param name="siteName"></param>
        /// <returns></returns>
        public bool MineSiteCreate(string siteName)
        {
            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdString = "INSERT INTO Site(siteName, mapScale, localMapFileAddr) VALUES(@siteName, @mapScale, @localMapFileAddr)";

                SqlCommand oCmd = new SqlCommand(cmdString, sqlCon);
                oCmd.Parameters.AddWithValue("@mapScale", 1);
                oCmd.Parameters.AddWithValue("@localMapFileAddr", "");
                oCmd.Parameters.AddWithValue("@siteName", siteName);

                sqlCon.Open();

                try
                {
                    int rows = oCmd.ExecuteNonQuery();

                    _mainForm.UpdateMineSiteList();

                    return true;
                }
                catch (SqlException e)
                {
                    MessageBox.Show("Error", e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return false;
                }
            }
        }

        /// <summary>
        /// Updates the existing mine site in the database
        /// </summary>
        /// <returns></returns>
        public bool MineSiteUpdate()    
        {
            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdString = "UPDATE Site SET siteName=@siteName, mapScale=@mapScale, localMapFileAddr=@localMapFileAddr WHERE Id=@siteId";

                SqlCommand oCmd = new SqlCommand(cmdString, sqlCon);
                oCmd.Parameters.AddWithValue("@siteName", MineSite.GetInstance().siteName);
                oCmd.Parameters.AddWithValue("@mapScale", MineSite.GetInstance().mapScale);
                oCmd.Parameters.AddWithValue("@localMapFileAddr", MineSite.GetInstance().localMapFileAddr);
                oCmd.Parameters.AddWithValue("@siteId", MineSite.GetInstance().siteId);

                sqlCon.Open();

                try
                {
                    int rows = oCmd.ExecuteNonQuery();

                    // Update all MineSite related MainForm components
                    _mainForm.MineSiteUpdated();

                    return true;
                }
                catch (SqlException e)
                {
                    MessageBox.Show("Error", e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return false;
                }
            }
        }

        /// <summary>
        /// Queries the database for a list of all the tags
        /// </summary>
        /// <returns></returns>
        public List<string> RetrieveTagIDList()
        {
            List<string> tags = new List<string>();

            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdString = "SELECT DISTINCT t.Id FROM Tags t WHERE t.Id NOT IN (SELECT tagId FROM Members)";

                SqlCommand oCmd = new SqlCommand(cmdString, sqlCon);

                sqlCon.Open();
                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        tags.Add(oReader["Id"].ToString());
                    }
                }
            }

            return tags;
        }

        /// <summary>
        /// Adds a new router to the database. Also add the router to the MineSite.
        /// </summary>
        /// <param name="rId"></param>
        /// <param name="addr"></param>
        /// <param name="loc"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="isBlocked"></param>
        /// <returns></returns>
        public bool RouterCreate(string rId, string addr, string loc, int x, int y, bool isBlocked)
        {
            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdString = "INSERT INTO Routers(Id, address, location, x, y, isBlocked, siteId) VALUES(@Id, @address, @location, @x, @y, @isBlocked, @siteId)";

                sqlCon.Open();
                SqlCommand oCmd = new SqlCommand(cmdString, sqlCon);
                oCmd.Parameters.AddWithValue("@Id", rId);
                oCmd.Parameters.AddWithValue("@address", addr);
                oCmd.Parameters.AddWithValue("@location", loc);
                oCmd.Parameters.AddWithValue("@x", x);
                oCmd.Parameters.AddWithValue("@y", y);
                oCmd.Parameters.AddWithValue("@isBlocked", isBlocked ? 1 : 0);
                oCmd.Parameters.AddWithValue("@siteId", MineSite.GetInstance().siteId);

                try
                {
                    int rows = oCmd.ExecuteNonQuery();
                    Router router = new Router(rId, addr, loc, x, y, isBlocked);
                    MineSite.GetInstance().siteRouters.Add(router);

                    _mainForm.AddNewCreatedRouter(router);

                    return true;
                }
                catch (SqlException e)
                {
                    MessageBox.Show("Error", e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return false;
                }

            }
        }

        /// <summary>
        /// Removes a router from the database
        /// </summary>
        /// <param name="router">The router to be removed.</param>
        /// <returns>True if successfully removed, false otherwise.</returns>
        public bool RouterDelete(Router router)
        {
            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdString = "DELETE FROM Routers WHERE Id=@rId";

                sqlCon.Open();
                SqlCommand oCmd = new SqlCommand(cmdString, sqlCon);
                oCmd.Parameters.AddWithValue("@rId", router.routerId);

                try
                {
                    int rows = oCmd.ExecuteNonQuery();

                    // Remove router objects
                    router.Dispose();
                    MineSite.GetInstance().siteRouters.Remove(router);

                    return true;
                }
                catch (SqlException e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return false;
                }

            }
        }

        /// <summary>
        /// Composes an update statement for Routers table
        /// </summary>
        public bool RouterUpdate(Router router, string rId, string addr, string loc, int x, int y, bool isBlocked)
        {
            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdString = "UPDATE Routers SET address=@address, location=@location, x=@x, y=@y, isBlocked=@isBlocked WHERE Id=@Id";

                SqlCommand oCmd = new SqlCommand(cmdString, sqlCon);
                oCmd.Parameters.AddWithValue("@Id", rId);
                oCmd.Parameters.AddWithValue("@address", addr);
                oCmd.Parameters.AddWithValue("@location", loc);
                oCmd.Parameters.AddWithValue("@x", x);
                oCmd.Parameters.AddWithValue("@y", y);
                oCmd.Parameters.AddWithValue("@isBlocked", isBlocked ? 1 : 0);

                sqlCon.Open();

                try
                {
                    int rows = oCmd.ExecuteNonQuery();
                    router.Update(rId, addr, loc, x, y, isBlocked);
                    return true;
                }
                catch (SqlException e)
                {
                    MessageBox.Show("Error", e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return false;
                }
            }
        }

        /// <summary>
        /// Creates a sensor in the database.
        /// </summary>
        /// <param name="sensorId"></param>
        /// <returns></returns>
        public bool SensorCreate(string sensorId)
        {
            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdString = "INSERT INTO Sensors(Id) VALUES(@Id)";

                sqlCon.Open();
                SqlCommand oCmd = new SqlCommand(cmdString, sqlCon);
                oCmd.Parameters.AddWithValue("@Id", sensorId);

                try
                {
                    int rows = oCmd.ExecuteNonQuery();
                    MineSite.GetInstance().siteSensors.Add(sensorId);
                }
                catch (SqlException e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Deletes a sensor from the database.
        /// </summary>
        /// <param name="sensorId"></param>
        /// <returns></returns>
        public bool SensorDelete(string sensorId)
        {
            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdString = "DELETE FROM Sensors WHERE Id=@Id";

                sqlCon.Open();
                SqlCommand oCmd = new SqlCommand(cmdString, sqlCon);
                oCmd.Parameters.AddWithValue("@Id", sensorId);

                try
                {
                    int rows = oCmd.ExecuteNonQuery();
                    MineSite.GetInstance().siteSensors.Remove(sensorId);
                }
                catch (SqlException e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Sets routers on screen to invisible so the user can point to a router position
        /// </summary>
        public void SetRouterPositionSetable()
        {
            if (!isSelectingPos)
            {
                foreach (Control c in _picMinePlan.Controls)
                {
                    c.Visible = false;
                }

                _picMinePlan.MouseDown += SelectRouterPosClick;
                isSelectingPos = true;
            }
        }

        /// <summary>
        /// Creates a tag ID and adds it to the database
        /// </summary>
        /// <param name="tagId"></param>
        public bool TagCreate(string tagId)
        {
            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdString = "INSERT INTO Tags(Id) VALUES(@Id)";

                sqlCon.Open();
                SqlCommand oCmd = new SqlCommand(cmdString, sqlCon);
                oCmd.Parameters.AddWithValue("@Id", tagId);

                try
                {
                    int rows = oCmd.ExecuteNonQuery();
                }
                catch (SqlException e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Deletes the tag ID from the database
        /// </summary>
        /// <param name="tagId"></param>
        public bool TagDelete(string tagId)
        {
            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdString = "DELETE FROM Tags WHERE Id=@Id";

                sqlCon.Open();
                SqlCommand oCmd = new SqlCommand(cmdString, sqlCon);
                oCmd.Parameters.AddWithValue("@Id", tagId);

                try
                {
                    int rows = oCmd.ExecuteNonQuery();
                }
                catch (SqlException e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// After a router position has been chosen on the mine plan picture
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SelectRouterPosClick(object sender, MouseEventArgs e)
        {
            if (e != null)
            {
                _routerForm.SetRouterPosFromMap((int)(e.X / MineSite.GetInstance().mapScale), (int)(e.Y / MineSite.GetInstance().mapScale));
            }

            foreach (Control c in _picMinePlan.Controls)
            {
                c.Visible = true;
            }

            _picMinePlan.MouseDown -= SelectRouterPosClick;
            isSelectingPos = false;
        }

        /// <summary>
        /// Creates a new user in the database
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="fName"></param>
        /// <param name="lName"></param>
        public bool UserCreate(string username, string password, string fName, string lName)
        {
            string salt = GenerateSalt();

            string hashedPass = HashPassword(salt, password);

            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdString = "INSERT INTO Users(username, password, salt, fName, lName, isAdmin) VALUES(@username, @password, @salt, @fName, @lName, @isAdmin)";

                sqlCon.Open();
                SqlCommand oCmd = new SqlCommand(cmdString, sqlCon);
                oCmd.Parameters.AddWithValue("@username", username);
                oCmd.Parameters.AddWithValue("@password", hashedPass);
                oCmd.Parameters.AddWithValue("@salt", salt);
                oCmd.Parameters.AddWithValue("@fName", fName);
                oCmd.Parameters.AddWithValue("@lName", lName);
                oCmd.Parameters.AddWithValue("@isAdmin", false);

                try
                {
                    int rows = oCmd.ExecuteNonQuery();
                }
                catch (SqlException e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Updates the user's data in the database
        /// </summary>
        /// <param name="fName"></param>
        /// <param name="lName"></param>
        public bool UserUpdate(string fName, string lName)
        {
            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdString = "UPDATE Users SET fName=@fName, lName=@lName WHERE username=@username";

                sqlCon.Open();
                SqlCommand oCmd = new SqlCommand(cmdString, sqlCon);
                oCmd.Parameters.AddWithValue("@fName", fName);
                oCmd.Parameters.AddWithValue("@lName", lName);
                oCmd.Parameters.AddWithValue("@username", User.GetInstance().username);

                try
                {
                    int rows = oCmd.ExecuteNonQuery();

                    User.GetInstance().Init(User.GetInstance().username, fName, lName, User.GetInstance().isAdmin);
                }
                catch (SqlException e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Hashes and updates the user's password in the database
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool UserPasswordUpdate(string password)  
        {
            string salt = GenerateSalt();

            string hashedPass = HashPassword(salt, password);

            using (SqlConnection sqlCon = new SqlConnection(Properties.Settings.Default.TMS_DatabaseConnectionString))
            {
                string cmdString = "UPDATE Users SET password=@password, salt=@salt WHERE username=@username";

                sqlCon.Open();
                SqlCommand oCmd = new SqlCommand(cmdString, sqlCon);
                oCmd.Parameters.AddWithValue("@password", hashedPass);
                oCmd.Parameters.AddWithValue("@salt", salt);
                oCmd.Parameters.AddWithValue("@username", User.GetInstance().username);

                try
                {
                    int rows = oCmd.ExecuteNonQuery();
                }
                catch (SqlException e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return false;
                }
            }

            return true;
        }
    }
}
