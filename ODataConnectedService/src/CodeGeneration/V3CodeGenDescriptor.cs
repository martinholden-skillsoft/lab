// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Data.Services.Design;
using System.IO;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using EnvDTE;
using Microsoft.OData.ConnectedService.Common;
using Microsoft.SharePoint.Client;
using Microsoft.VisualStudio.ConnectedServices;

namespace Microsoft.OData.ConnectedService.CodeGeneration
{
    internal class V3CodeGenDescriptor : BaseCodeGenDescriptor
    {
        public V3CodeGenDescriptor(string metadataUri, ConnectedServiceHandlerContext context, Project project)
            : base(metadataUri, context, project)
        {
            this.ClientNuGetPackageName = Common.Constants.V3ClientNuGetPackage;
            this.ClientDocUri = Common.Constants.V3DocUri;
        }

        public async override Task AddNugetPackages()
        {
            await this.Context.Logger.WriteMessageAsync(LoggerMessageCategory.Information, "Adding Nuget Packages");

            if (!PackageInstallerServices.IsPackageInstalled(this.Project, this.ClientNuGetPackageName))
            {
                Version packageVersion = null;
                PackageInstaller.InstallPackage(Common.Constants.NuGetOnlineRepository, this.Project, this.ClientNuGetPackageName, packageVersion, false);
            }
        }

        public async override Task AddGeneratedClientCode()
        {
            await this.Context.Logger.WriteMessageAsync(LoggerMessageCategory.Information, "Generating Client Proxy v3 ...");

            EntityClassGenerator generator = new EntityClassGenerator(LanguageOption.GenerateCSharpCode)
            {
                UseDataServiceCollection = this.ServiceConfiguration.UseDataServiceCollection,
                Version = DataServiceCodeVersion.V3
            };

            XmlReaderSettings readerSettings = new XmlReaderSettings();
            if (this.ServiceConfiguration.Credentials?.GetType() == typeof(SharePointOnlineCredentials))
            {
                readerSettings.XmlResolver = new SharePointXMLUrlResolver()
                {
                    Credentials = this.ServiceConfiguration.Credentials
                };
            }
            else
            {
                readerSettings.XmlResolver = new XmlUrlResolver()
                {
                    Credentials = this.ServiceConfiguration.Credentials
                };
            }


            using (XmlReader reader = XmlReader.Create(this.MetadataUri, readerSettings))
            {
                string tempFile = Path.GetTempFileName();

                using (StreamWriter writer = System.IO.File.CreateText(tempFile))
                {
                    var errors = generator.GenerateCode(reader, writer, this.ServiceConfiguration.NamespacePrefix);
                    await writer.FlushAsync();
                    if (errors != null && errors.Count() > 0)
                    {
                        foreach (var err in errors)
                        {
                            await this.Context.Logger.WriteMessageAsync(LoggerMessageCategory.Warning, err.Message);
                        }
                    }
                }

                string outputFile = Path.Combine(GetReferenceFileFolder(), this.GeneratedFileNamePrefix + ".cs");
                await this.Context.HandlerHelper.AddFileAsync(tempFile, outputFile);
            }
        }
    }
}
