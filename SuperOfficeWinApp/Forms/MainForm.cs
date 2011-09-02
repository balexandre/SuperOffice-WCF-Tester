using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SuperOfficeWinApp
{
    public partial class MainForm : Form
    {
        public SuperOfficeWCFService service;
        private const string REGISTRY_LOCATION = "SOFTWARE\\SuperOfficeWCFTester\\Preferences";

        public MainForm()
        {
            InitializeComponent();
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            this.AcceptButton = btnAuthenticate;
            btnGetAssociates.Enabled = false;

            if (ReadRegistryVariables())
                // an existing user is available, let's focus on the password then
                // NOTE: Password fields are not able to focus
                txtPwd.Focus(); 
            else
                // no info exists, let's focus on the url
                txtUrl.Focus(); 
        }

        #region Events
        
        private void btnAuthenticate_Click(object sender, EventArgs e)
        {
            Authenticate();
        }
        private void btnGetAssociates_Click(object sender, EventArgs e)
        {
            FetchAssociates();
        }
        private void lblError_DoubleClick(object sender, EventArgs e)
        {
            // Double click to clean Error Message
            lblError.Text = "";
        }

        #endregion

        #region Registry Operations

        private bool ReadRegistryVariables()
        {
            RegistryKey masterKey = Registry.LocalMachine.CreateSubKey(REGISTRY_LOCATION);

            object regUrl = masterKey.GetValue("SuperOfficeTesterUrl");
            object regUser = masterKey.GetValue("SuperOfficeTesterUsr");

            if (regUrl != null) { txtUrl.Text = regUrl.ToString(); }
            if (regUser != null) { txtUser.Text = regUser.ToString(); }

            return regUser != null;
        }
        private void SaveRegistryVariables()
        {
            RegistryKey masterKey = Registry.LocalMachine.CreateSubKey(REGISTRY_LOCATION);

            masterKey.SetValue("SuperOfficeTesterUrl", txtUrl.Text.Trim());
            masterKey.SetValue("SuperOfficeTesterUsr", txtUser.Text.Trim());
        }

        #endregion

        #region Actions

        private void Authenticate()
        {
            // clean
            lblCompanyName.Text = "";
            lblDBType.Text = "";
            lblDBVersion.Text = "";
            lblSerial.Text = "";
            lblError.Text = "";

            // disable btn
            btnAuthenticate.Text = "Authenticating...";
            btnAuthenticate.Enabled = false;

            // save for later
            SaveRegistryVariables();

            // create object
            this.service = new SuperOfficeWCFService(
                                        txtUrl.Text.TrimEnd('/'),
                                        txtUser.Text.Trim(),
                                        txtPwd.Text.Trim());

            if (!bwAuth.IsBusy)
                bwAuth.RunWorkerAsync();
        }
        private void FetchAssociates()
        {
            // clean
            gv.DataSource = null;
            lblError.Text = "";

            // disable btn
            btnGetAssociates.Text = "Fetching...";
            btnGetAssociates.Enabled = false;

            if (!bwFetch.IsBusy)
                bwFetch.RunWorkerAsync();
        }

        #endregion

        #region Background Workers

        private void bwAuth_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            WorkerHelper h = new WorkerHelper();

            try
            {
                SOAuthResponse r = service.Authenticate();

                h.AuthResponse = r;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    while (ex.InnerException != null)
                        ex = ex.InnerException;

                h.Ex = ex;
            }

            // pass result
            e.Result = h;
        }
        private void bwAuth_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            WorkerHelper h = (WorkerHelper)e.Result;

            // enable btn
            btnAuthenticate.Text = "Authenticate";
            btnAuthenticate.Enabled = true;

            // set values
            if (h.Ex == null)
            {
                lblCompanyName.Text = h.AuthResponse.CompanyName;
                lblDBType.Text = h.AuthResponse.DatabaseType;
                lblDBVersion.Text = h.AuthResponse.DatabaseVersion;
                lblSerial.Text = h.AuthResponse.SerialNumber;
                lblNetServer.Text = h.AuthResponse.NetServerVersion;
                lblUnicode.Text = h.AuthResponse.Unicode;

                btnGetAssociates.Enabled = true;
            }
            // show error
            else
            {
                lblError.Text = h.Ex.Message;
                btnGetAssociates.Enabled = false;
            }

            // Fetch Associates Automatically When Authenticating is done
            //FetchAssociates();
        }

        private void bwFetch_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            WorkerHelper h = new WorkerHelper();

            try
            {
                List<SOAssociate> r = service.ListAllAssociates();

                h.ListAllAssociates = r;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    while (ex.InnerException != null)
                        ex = ex.InnerException;

                h.Ex = ex;
            }

            // pass result
            e.Result = h;
        }
        private void bwFetch_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            WorkerHelper h = (WorkerHelper)e.Result;

            // enable btn
            btnGetAssociates.Text = "Fetch Associates";
            btnGetAssociates.Enabled = true;

            // set values
            if (h.Ex == null)
            {
                gv.DataSource = h.ListAllAssociates;
            }
            // show error
            else
            {
                lblError.Text = h.Ex.Message;
            }
        }

        #endregion
    }

    public class WorkerHelper
    {
        public WorkerHelper() { }

        public Exception Ex { get; set; }
        public SOAuthResponse AuthResponse { get; set; }
        public List<SOAssociate> ListAllAssociates { get; set; }
    }
}
