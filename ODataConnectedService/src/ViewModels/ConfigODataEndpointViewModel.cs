// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security;
using System.Threading.Tasks;
using System.Xml;
using AdysTech.CredentialManager;
using Microsoft.OData.ConnectedService.Common;
using Microsoft.OData.ConnectedService.Models;
using Microsoft.OData.ConnectedService.Views;
using Microsoft.SharePoint.Client;
using Microsoft.VisualStudio.ConnectedServices;

namespace Microsoft.OData.ConnectedService.ViewModels
{
    internal class ConfigODataEndpointViewModel : ConnectedServiceWizardPage
    {
        private UserSettings userSettings;

        public string Endpoint { get; set; }
        public string ServiceName { get; set; }
        public Version EdmxVersion { get; set; }
        public string MetadataTempPath { get; set; }
        public bool ResetCredentials { get; set; }
        public bool SaveCredentials { get; set; }
        public bool CredentialsNeeded { get; set;  }
        public ICredentials Credentials { get; private set; }
        public UserSettings UserSettings
        {
            get { return this.userSettings; }
        }

        public ConfigODataEndpointViewModel(UserSettings userSettings) : base()
        {
            this.Title = "Configure endpoint";
            this.Description = "Enter or choose an OData service endpoint to begin";
            this.Legend = "Endpoint";
            this.View = new ConfigODataEndpoint();
            this.ServiceName = Constants.DefaultServiceName;
            this.View.DataContext = this;
            this.ResetCredentials = false;
            this.SaveCredentials = true;
            this.CredentialsNeeded = true;
            this.userSettings = userSettings;
        }

        public override Task<PageNavigationResult> OnPageLeavingAsync(WizardLeavingArgs args)
        {
            UserSettings.AddToTopOfMruList(((ODataConnectedServiceWizard)this.Wizard).UserSettings.MruEndpoints, this.Endpoint);
            try
            {
                this.MetadataTempPath = GetMetadata(out Version version);
                this.EdmxVersion = version;
                return base.OnPageLeavingAsync(args);
            }
            catch (Exception e)
            {
                return Task.FromResult<PageNavigationResult>(
                    new PageNavigationResult()
                    {
                        ErrorMessage = e.Message,
                        IsSuccess = false,
                        ShowMessageBoxOnFailure = true
                    });
            }
        }

        /// <summary>
        /// Gets the credentials.
        /// </summary>
        /// <returns></returns>
        private ICredentials GetCredentials()
        {
            if (this.CredentialsNeeded)
            {
                string listhostname = new Uri(this.Endpoint).Host;
                bool save = false;

                if (this.ResetCredentials)
                {
                    CredentialManager.RemoveCredentials(listhostname);
                }

                NetworkCredential cred;
                cred = CredentialManager.GetCredentials(listhostname, CredentialManager.CredentialType.Generic);

                if (cred == null)
                {
                    cred = CredentialManager.PromptForCredentials(listhostname, ref save, "Please provide Credentials for " + listhostname, "Credentials");
                }

                if (cred != null)
                {
                    if (this.SaveCredentials)
                    {
                        CredentialManager.SaveCredentials(listhostname, cred);
                    }

                    if (listhostname.EndsWith(".sharepoint.com"))
                    {
                        return new SharePointOnlineCredentials(cred.UserName, cred.SecurePassword);
                    }
                    else
                    {
                        return new NetworkCredential(cred.UserName, cred.SecurePassword);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <param name="edmxVersion">The edmx version.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">OData Service Endpoint - Please input the service endpoint</exception>
        /// <exception cref="InvalidOperationException">
        /// The metadata is an empty file
        /// or
        /// </exception>
        private string GetMetadata(out Version edmxVersion)
        {
            this.Credentials = null;

            if (String.IsNullOrEmpty(this.Endpoint))
            {
                throw new ArgumentNullException("OData Service Endpoint", "Please input the service endpoint");
            }

            if (this.Endpoint.StartsWith("https:", StringComparison.Ordinal)
                || this.Endpoint.StartsWith("http", StringComparison.Ordinal))
            {
                if (!this.Endpoint.EndsWith("$metadata", StringComparison.Ordinal))
                {
                    this.Endpoint = this.Endpoint.TrimEnd('/') + "/$metadata";
                }
                this.Credentials = GetCredentials();
            }

            XmlReaderSettings readerSettings = new XmlReaderSettings();

            if (this.Credentials?.GetType() == typeof(SharePointOnlineCredentials))
            {
                readerSettings.XmlResolver = new SharePointXMLUrlResolver()
                {
                    Credentials = this.Credentials
                };
            }
            else
            {
                readerSettings.XmlResolver = new XmlUrlResolver()
                {
                    Credentials = this.Credentials
                };
            }

            string workFile = Path.GetTempFileName();

            try
            {
                using (XmlReader reader = XmlReader.Create(this.Endpoint, readerSettings))
                {
                    using (XmlWriter writer = XmlWriter.Create(workFile))
                    {
                        while (reader.NodeType != XmlNodeType.Element)
                        {
                            reader.Read();
                        }

                        if (reader.EOF)
                        {
                            throw new InvalidOperationException("The metadata is an empty file");
                        }

                        Common.Constants.SupportedEdmxNamespaces.TryGetValue(reader.NamespaceURI, out edmxVersion);
                        writer.WriteNode(reader, false);
                    }
                }
                return workFile;
            }
            catch (WebException e)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Cannot access {0}", this.Endpoint), e);
            }
        }
    }
}
