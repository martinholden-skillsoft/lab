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

            //Removed dependency on installed version of WCF Data Services.
            //The call to CodeGeneratorUtils.GetWCFDSInstallLocation() results in a NULL response if not installed
            //This then caused the Path.Combine to throw exception
            //This manifests as an error when adding/refreshing proxy of:
            // Adding OData Connected Service to the project failed: Value cannot be null. Parameter name: path1

            //var wcfDSInstallLocation = CodeGeneratorUtils.GetWCFDSInstallLocation();
            //var packageSource = Path.Combine(wcfDSInstallLocation, @"bin\NuGet");
            //if (Directory.Exists(packageSource))
            //{
            //    var files = Directory.EnumerateFiles(packageSource, "*.nupkg").ToList();
            //    foreach (var nugetPackage in Common.Constants.V3NuGetPackages)
            //    {
            //        if (!files.Any(f => Regex.IsMatch(f, nugetPackage + @"(.\d){2,4}.nupkg")))
            //        {
            //            packageSource = Common.Constants.NuGetOnlineRepository;
            //        }
            //    }
            //}
            //else
            //{
            //    packageSource = Common.Constants.NuGetOnlineRepository;
            //}

            if (!PackageInstallerServices.IsPackageInstalled(this.Project, this.ClientNuGetPackageName))
            {
                Version packageVersion = null;
                PackageInstaller.InstallPackage(Common.Constants.NuGetOnlineRepository, this.Project, this.ClientNuGetPackageName, packageVersion, false);
            }
        }

        public async override Task AddGeneratedClientCode()
        {
            await this.Context.Logger.WriteMessageAsync(LoggerMessageCategory.Information, "Generating Client Proxy v3 ...");

            EntityClassGenerator generator = new EntityClassGenerator(LanguageOption.GenerateCSharpCode);
            generator.UseDataServiceCollection = this.ServiceConfiguration.UseDataServiceCollection;
            generator.Version = DataServiceCodeVersion.V3;

            XmlReaderSettings settings = new XmlReaderSettings()
            {
                XmlResolver = new XmlUrlResolver()
                {
                    Credentials = System.Net.CredentialCache.DefaultNetworkCredentials
                }
            };

            if (!String.IsNullOrEmpty(this.ServiceConfiguration.SharePointOnlineUsername))
            {
                SecureString password = new SecureString();
                foreach (char c in this.ServiceConfiguration.SharePointOnlinePassword.ToCharArray()) password.AppendChar(c);
                SharePointOnlineCredentials spcredentials = new SharePointOnlineCredentials(this.ServiceConfiguration.SharePointOnlineUsername, password);

                settings.XmlResolver = new SharePointXMLUrlResolver()
                {
                    Credentials = spcredentials
                };
            }


            using (XmlReader reader = XmlReader.Create(this.MetadataUri, settings))
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
