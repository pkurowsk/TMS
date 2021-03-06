﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS
{
    class User
    {
        private static User _instance;

        public string username { get; private set; }
        public String fName { get; private set; }
        public String lName { get; private set; }

        public bool isAdmin { get; private set; }

        public void Init(string uName, string f, string l, bool isA)
        {
            username = uName;
            fName = f;
            lName = l;
            isAdmin = isA;
        }
        public static User GetInstance()
        {
            if (_instance == null)
            {
                _instance = new User();
            }

            return _instance;
        }

    }
}
