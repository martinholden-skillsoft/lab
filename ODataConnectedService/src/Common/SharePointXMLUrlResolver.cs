using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SharePoint.Client;

namespace Microsoft.OData.ConnectedService.Common
{
    class SharePointXMLUrlResolver: XmlUrlResolver
    {
        ICredentials credentials;

        public SharePointXMLUrlResolver()
        {
        }

        public override ICredentials Credentials
        {
            set
            {
                credentials = value;
                base.Credentials = value;
            }
        }

        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            if (absoluteUri == null)
            {
                throw new ArgumentNullException("absoluteUri");
            }

            //Use SharePoint Credentials
            if (credentials.GetType() == typeof(SharePointOnlineCredentials))
            {
                var spcredentials = credentials as SharePointOnlineCredentials;
                var authenticationCookie = spcredentials.GetAuthenticationCookie(absoluteUri);

                WebRequest webReq = WebRequest.Create(absoluteUri);
                webReq.Headers.Clear();
                webReq.Headers.Add("Cookie", authenticationCookie);

                WebResponse resp = webReq.GetResponse();
                return resp.GetResponseStream();
            }
            //otherwise use the default behavior of the XmlUrlResolver class (resolve resources from source)
            else
            {
                return base.GetEntity(absoluteUri, role, ofObjectToReturn);
            }
        }
    }
}
