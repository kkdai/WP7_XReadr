using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Data;

namespace XReadr
{
    //實作INotifyPropertyChanged與INotifyPropertyChanging介面，做為屬性(欄位)值變更時的事件觸發。
    [Table(Name = "User")]
    public class User : INotifyPropertyChanged, INotifyPropertyChanging
    {
        //該Contacts的Table具有二個欄位：ID、Password
        private int gID;
        [Column(IsPrimaryKey = true, IsDbGenerated = true,
                DbType = "INT NOT NULL Identity", CanBeNull = false, AutoSync = AutoSync.OnInsert)]
        public int ID
        {
            get
            {
                return gID;
            }
            set
            {
                NotifyPropertyChanging("ID");
                gID = value;
                NotifyPropertyChanged("ID");
            }
        }

        private string gEmail;
        [Column(DbType = "NVarChar(30) NOT NULL", CanBeNull = false, Storage = "Email")]
        public string Email
        {
            get
            {
                return gEmail;
            }
            set
            {
                NotifyPropertyChanging("Email");
                gEmail = value;
                NotifyPropertyChanging("Email");
            }
        }

        private string gPassword;
        [Column(DbType = "NVarChar(30) NOT NULL", CanBeNull = false, Storage = "Password")]
        public string Password
        {
            get
            {
                return gPassword;
            }
            set
            {
                NotifyPropertyChanging("Password");
                gPassword = value;
                NotifyPropertyChanging("Password");
            }
        }

        #region INotifyPropertyChanging觸發事件
        public event PropertyChangingEventHandler PropertyChanging;

        private void NotifyPropertyChanging(string pPropertyName)
        {
            if (PropertyChanging != null)
            {
                PropertyChanging(this, new PropertyChangingEventArgs(pPropertyName));
            }
        }
        #endregion

        #region INotifyPropertyChanged觸發事件
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string pPropertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(pPropertyName));
            }
        }
        #endregion
    }
}
