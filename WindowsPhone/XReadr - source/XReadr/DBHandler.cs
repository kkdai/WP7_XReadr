using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Data;

namespace XReadr
{
    public class DBHandler : DataContext
    {
        public static string DBConnection = "Data Source=isostore:/XReadr.sdf";
        public DBHandler(string pDBConnection) : base(pDBConnection) 
        {} 
        public Table<User> UserInfo;
     }    
}
