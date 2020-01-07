// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.OData.ConnectedService.Models
{
    [DataContract]
    internal class ServiceConfiguration
    {
        [DataMember]
        public string ServiceName { get; set; }
        [DataMember]
        public string Endpoint { get; set; }
        [DataMember]
        public Version EdmxVersion { get; set; }
        [DataMember]
        public string GeneratedFileNamePrefix { get; set; }

        
        public string SharePointOnlineUsername { get; set; }
        
        public string SharePointOnlinePassword { get; set; }
        [DataMember]
        public bool UseNameSpacePrefix { get; set; }
        [DataMember]
        public string NamespacePrefix { get; set; }
        [DataMember]
        public bool UseDataServiceCollection { get; set; }
    }

    [DataContract]
    internal class ServiceConfigurationV4 : ServiceConfiguration
    {
        [DataMember]
        public bool EnableNamingAlias { get; set; }
        [DataMember]
        public bool IgnoreUnexpectedElementsAndAttributes { get; set; }
        [DataMember]
        public bool IncludeT4File { get; set; }
    }
}
