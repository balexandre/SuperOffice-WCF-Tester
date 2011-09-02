using System;
using System.Collections.Generic;
using System.Linq;

namespace SuperOfficeWinApp
{
    public class SuperOfficeWCFService
    {
        private SOAuthResponse SOAuth { get; set; }
        private string endpointUrl { get; set; }
        private string endpointUser { get; set; }
        private string endpointPwd { get; set; }

        public SuperOfficeWCFService(string url, string user, string pwd) {
            this.endpointUrl = url;
            this.endpointUser = user;
            this.endpointPwd = pwd;
        }

        /// <summary>
        /// Authenticate withing SO Services using SoPrincipal Service
        /// </summary>
        /// <returns></returns>
        public SOAuthResponse Authenticate()
        {
            bool sucess = false;
            bool authenticationSuceeded;

            this.SOAuth = new SOAuthResponse();
            this.SOAuth.URL = this.endpointUrl.TrimEnd('/');

            // ### Service Header #################################################
            Service.SoWcfSoPrincipal.SoPrincipalClient credClient = new Service.SoWcfSoPrincipal.SoPrincipalClient();
            Service.SoWcfSoPrincipal.SoPrincipalCarrier responseCarrier;
            Service.SoWcfSoPrincipal.SoCredentials credentials;
            Service.SoWcfSoPrincipal.SoTimeZone timezone;

            // ### Service Endpoint ###############################################
            string url = String.Concat(this.SOAuth.URL, "/SoPrincipal.svc");
            credClient.Endpoint.Address = new System.ServiceModel.EndpointAddress(url);

            // ### Service Call ###################################################
            credClient.AuthenticateUsernamePassword(
                                    this.endpointPwd, this.endpointUser,
                                    out sucess,
                                    out timezone,
                                    out responseCarrier,
                                    out authenticationSuceeded,
                                    out credentials);

            // ####################################################################

            if (authenticationSuceeded)
            {
                // Save ticket for later
                this.SOAuth.Ticket = credentials.Ticket;

                Service.SoWcfSoPrincipal.SoTimeZone tzone;
                Service.SoWcfSoPrincipal.SoSystemInfoCarrier response;
                credClient.GetSystemInfo(out sucess, out tzone, out response);

                if (sucess)
                {
                    this.SOAuth.CompanyName = response.CompanyName;
                    this.SOAuth.DatabaseType = response.DatabaseType;
                    this.SOAuth.DatabaseVersion = response.DatabaseVersion.ToString();
                    this.SOAuth.SerialNumber = response.License.SerialNr;
                    this.SOAuth.LicenseVersion = response.License.LicenseVersion;
                    this.SOAuth.NetServerVersion = response.Description;
                    this.SOAuth.Unicode = response.IsUnicode ? "Yes" : "No";
                }
            }
            else
                throw new ArgumentException("Credentials were not authenticated.\nPlease verify them and try again.");

            credClient.Close();
            return this.SOAuth;
        }

        /// <summary>
        /// List all Associates using the Person Service
        /// </summary>
        /// <returns></returns>
        public List<SOAssociate> ListAllAssociates()
        {
            List<SOAssociate> list = new List<SOAssociate>();
            
            // Let's find all Associate ID's first
            List<int> ass = this.GetAllAssociateIds();

            // And from that List, Let's get their Person Id's
            List<int> pss = this.GetAllPersonIdsFromAssociateIds(ass);

            // Now that we have the Person Id's, let's get all their information
            bool sucess;
            Service.SoWcfPerson.Person[] response = null;
            List<int> listAssociates = new List<int>();

            // ### Service Endpoint ###############################################
            Service.SoWcfPerson.Person1Client client = new Service.SoWcfPerson.Person1Client();
            client.Endpoint.Address = new System.ServiceModel.EndpointAddress(this.SOAuth.URL + "/Person.svc");

            // ### Service Header #################################################
            Service.SoWcfPerson.SoTimeZone tzone = new Service.SoWcfPerson.SoTimeZone();
            Service.SoWcfPerson.SoCredentials cred = new Service.SoWcfPerson.SoCredentials();
            cred.Ticket = this.SOAuth.Ticket;


            // ### Service Call ###################################################
            Service.SoWcfPerson.SoExceptionInfo exec =
                                            client.GetPersonList(
                                                    cred, ref tzone,
                                                    pss.ToArray(),
                                                    out sucess, out response);

            // ####################################################################
            if (sucess)
            {
                foreach (var p in response)
                    list.Add(
                        new SOAssociate()
                        {
                            ID = p.AssociateId,
                            PersonId = p.PersonId,
                            Name = p.FullName,
                            Company = p.ContactName,
                            Username = p.AssociateName
                        });
            }

            client.Close();
            return list;
        }

        #region Helpers
        
        /// <summary>
        /// Gets all Associate Id's using the MDO Service
        /// </summary>
        /// <returns></returns>
        private List<int> GetAllAssociateIds()
        {
            bool sucess;
            Service.SoWcfMdo.MDOListItem[] response = null;
            List<int> listAssociates = new List<int>();

            // ### Service Endpoint ###############################################
            Service.SoWcfMdo.MDOClient client = new Service.SoWcfMdo.MDOClient();
            client.Endpoint.Address = new System.ServiceModel.EndpointAddress(this.SOAuth.URL + "/Mdo.svc");

            // ### Service Header #################################################
            Service.SoWcfMdo.SoTimeZone tzone = new Service.SoWcfMdo.SoTimeZone();
            Service.SoWcfMdo.SoCredentials cred = new Service.SoWcfMdo.SoCredentials();
            cred.Ticket = this.SOAuth.Ticket;


            // ### Service Call ###################################################
            Service.SoWcfMdo.SoExceptionInfo exec =
                                            client.GetList(
                                                    cred, ref tzone,
                                                    "", false, "associate", false,
                                                    out sucess, out response);

            // ####################################################################
            if (sucess)
            {
                foreach (var mdo in response)
                    if (mdo.ChildItems.Length > 0)
                    {
                        foreach (var mdoChild in mdo.ChildItems)
                            listAssociates.Add(mdoChild.Id);
                    }
                    else
                        listAssociates.Add(mdo.Id);
            }

            client.Close();
            return listAssociates;
        }
        /// <summary>
        /// Get's all the Associate Id's using the Associate Service
        /// </summary>
        /// <param name="associateIds"></param>
        /// <returns></returns>
        private List<int> GetAllPersonIdsFromAssociateIds(List<int> associateIds)
        {
            bool sucess;
            Service.SoWcfAssociate.Associate[] response = null;
            List<int> listAssociates = new List<int>();

            // ### Service Endpoint ###############################################
            Service.SoWcfAssociate.Associate1Client client = new Service.SoWcfAssociate.Associate1Client();
            client.Endpoint.Address = new System.ServiceModel.EndpointAddress(this.SOAuth.URL + "/Associate.svc");

            // ### Service Header #################################################
            Service.SoWcfAssociate.SoTimeZone tzone = new Service.SoWcfAssociate.SoTimeZone();
            Service.SoWcfAssociate.SoCredentials cred = new Service.SoWcfAssociate.SoCredentials();
            cred.Ticket = this.SOAuth.Ticket;


            // ### Service Call ###################################################
            Service.SoWcfAssociate.SoExceptionInfo exec =
                                            client.GetAssociateList(
                                                    cred, ref tzone,
                                                    associateIds.ToArray(),
                                                    out sucess, out response);

            // ####################################################################
            if (sucess)
            {
                foreach (var a in response)
                    listAssociates.Add(a.PersonId);
            }

            client.Close();
            return listAssociates;
        }

        #endregion
    }

    #region Helpers
    
    public class SOAuthResponse
    {
        public SOAuthResponse() { }

        public string URL { get; set; }
        public string Ticket { get; set; }

        public string CompanyName { get; set; }
        public string DatabaseType { get; set; }
        public string DatabaseVersion { get; set; }
        public string NetServerVersion { get; set; }
        public string SerialNumber { get; set; }
        public string LicenseVersion { get; set; }
        public string Unicode { get; set; }

    }
    public class SOAssociate
    {
        public SOAssociate() { }

        public int ID { get; set; }
        public int PersonId { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Company { get; set; }
    }
    
    #endregion
}
