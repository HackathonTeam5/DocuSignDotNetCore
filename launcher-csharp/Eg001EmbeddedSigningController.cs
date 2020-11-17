using System;
using System.Collections.Generic;
using Microsoft.SharePoint.Client;
using System.IO;
using System.Net;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Transactions;
using DocuSign.eSign.Api;
using DocuSign.eSign.Client;
using DocuSign.eSign.Model;
using DocuSign.CodeExamples.Controllers;
using DocuSign.CodeExamples.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace DocuSign.CodeExamples.Views
{
    [Route("eg001")]
    public class Eg001EmbeddedSigningController : HackathonController
    {
        private string dsPingUrl;
        private string signerClientId = "1000";
        private string dsReturnUrl;
        private ICredentials credentials;
        private string fileTobeSigned;

        public Eg001EmbeddedSigningController(DSConfiguration config, IRequestItemsService requestItemsService)
            : base(config, requestItemsService)
        {            
            dsPingUrl = config.AppUrl + "/";
            dsReturnUrl = config.AppUrl + "/dsReturn";           
            ViewBag.title = "Embedded Signing Ceremony";

            //set credential of SharePoint online
            SecureString secureString = new SecureString();
            foreach (char c in "HackathonTeam5@LCG".ToCharArray())
            {
                secureString.AppendChar(c);
            }
            credentials = new SharePointOnlineCredentials("OceanSunFish@nih1.onmicrosoft.com", secureString);             
        }

        // ***DS.snippet.0.start
        private string DoWork(string signerEmail, string signerName,
            string accessToken, string basePath, string accountId,string fileName)
        {
            // Data for this method
            // signerEmail 
            // signerName
            // accessToken
            // basePath
            // accountId

            // dsPingUrl -- class global
            // signerClientId -- class global
            // dsReturnUrl -- class global

            // Step 1. Create the envelope definition

           // fileName = DownloadFileViaRestAPI(fileName);
      //     fileName = Config.docPdf;
      fileName="wwwroot/World_Wide_Corp_lorem.pdf";            
             EnvelopeDefinition envelope = MakeEnvelope(signerEmail, signerName,fileName);

            // Step 2. Call DocuSign to create the envelope                   
            var apiClient = new ApiClient(basePath);
            apiClient.Configuration.DefaultHeader.Add("Authorization", "Bearer " + accessToken);
            var envelopesApi = new EnvelopesApi(apiClient);
            EnvelopeSummary results = envelopesApi.CreateEnvelope(accountId, envelope);
            string envelopeId = results.EnvelopeId;

            // Save for future use within the example launcher
            RequestItemsService.EnvelopeId = envelopeId;

            // Step 3. create the recipient view, the Signing Ceremony
            RecipientViewRequest viewRequest = MakeRecipientViewRequest(signerEmail, signerName,envelopeId);
            // call the CreateRecipientView API
            ViewUrl results1 = envelopesApi.CreateRecipientView(accountId, envelopeId, viewRequest);

            // Step 4. Redirect the user to the Signing Ceremony
            // Don't use an iFrame!
            // State can be stored/recovered using the framework's session or a
            // query parameter on the returnUrl (see the makeRecipientViewRequest method)
            string redirectUrl = results1.Url;
            return redirectUrl;
        }

        private RecipientViewRequest MakeRecipientViewRequest(string signerEmail, string signerName,string envelopeid)
        {
            // Data for this method
            // signerEmail 
            // signerName
            // dsPingUrl -- class global
            // signerClientId -- class global
            // dsReturnUrl -- class global


            RecipientViewRequest viewRequest = new RecipientViewRequest();
            // Set the url where you want the recipient to go once they are done signing
            // should typically be a callback route somewhere in your app.
            // The query parameter is included as an example of how
            // to save/recover state information during the redirect to
            // the DocuSign signing ceremony. It's usually better to use
            // the session mechanism of your web framework. Query parameters
            // can be changed/spoofed very easily.
            viewRequest.ReturnUrl = dsReturnUrl + "?state=123" +"&envelopeid=" +envelopeid;

            // How has your app authenticated the user? In addition to your app's
            // authentication, you can include authenticate steps from DocuSign.
            // Eg, SMS authentication
            viewRequest.AuthenticationMethod = "none";

            // Recipient information must match embedded recipient info
            // we used to create the envelope.
            viewRequest.Email = signerEmail;
            viewRequest.UserName = signerName;
            viewRequest.ClientUserId = signerClientId;

            // DocuSign recommends that you redirect to DocuSign for the
            // Signing Ceremony. There are multiple ways to save state.
            // To maintain your application's session, use the pingUrl
            // parameter. It causes the DocuSign Signing Ceremony web page
            // (not the DocuSign server) to send pings via AJAX to your
            // app,
            viewRequest.PingFrequency = "600"; // seconds
                                               // NOTE: The pings will only be sent if the pingUrl is an https address
            viewRequest.PingUrl = dsPingUrl; // optional setting

            return viewRequest;
        }

        private EnvelopeDefinition MakeEnvelope(string signerEmail, string signerName,string fileName)
        {
            // Data for this method
            // signerEmail 
            // signerName
            // signerClientId -- class global
            // Config.docPdf


            // Grab the docid from SharePoint. 
            // Stream the Contents on to the Disk and give it a unique name
            //  byte[] buffer = System.IO.File.ReadAllBytes();
            byte[] buffer = System.IO.File.ReadAllBytes(fileName);
          //  System.IO.File.Delete(fileName);
            EnvelopeDefinition envelopeDefinition = new EnvelopeDefinition();
            envelopeDefinition.EmailSubject = "Please sign this document";
            Document doc1 = new Document();

            String doc1b64 = Convert.ToBase64String(buffer);

            doc1.DocumentBase64 = doc1b64;
            doc1.Name = "Lorem Ipsum"; // can be different from actual file name
            doc1.FileExtension = "pdf";
            doc1.DocumentId = "3";

            // The order in the docs array determines the order in the envelope
            envelopeDefinition.Documents = new List<Document> { doc1 };

            // Create a signer recipient to sign the document, identified by name and email
            // We set the clientUserId to enable embedded signing for the recipient
            // We're setting the parameters via the object creation
            Signer signer1 = new Signer {
                Email = signerEmail,
                Name = signerName,
                ClientUserId = signerClientId,
                RecipientId = "1"
            };
           
            // Create signHere fields (also known as tabs) on the documents,
            // We're using anchor (autoPlace) positioning
            //
            // The DocuSign platform seaches throughout your envelope's
            // documents for matching anchor strings.
            SignHere signHere1 = new SignHere
            {
                AnchorString = "/sn1/",
                AnchorUnits = "pixels",
                AnchorXOffset = "10",
                AnchorYOffset = "20"
            };
            // Tabs are set per recipient / signer
            Tabs signer1Tabs = new Tabs
            {
                SignHereTabs = new List<SignHere> { signHere1 }
            };
            signer1.Tabs = signer1Tabs;

            // Add the recipient to the envelope object
            Recipients recipients = new Recipients
            {
                Signers = new List<Signer> { signer1 }
            };
            envelopeDefinition.Recipients = recipients;

            // Request that the envelope be sent by setting |status| to "sent".
            // To request that the envelope be created as a draft, set to "created"
            envelopeDefinition.Status = "sent";

            return envelopeDefinition;
        }
        // ***DS.snippet.0.end


        public override string EgName => "eg001";

        [HttpPost]
        public IActionResult Create(string signerEmail, string signerName)
        {
            // Data for this method
            // signerEmail 
            // signerName
            // dsPingUrl -- class global
            // signerClientId -- class global
            // dsReturnUrl -- class global
            string accessToken = RequestItemsService.User.AccessToken;
            string basePath = RequestItemsService.Session.BasePath + "/restapi";
            string accountId = RequestItemsService.Session.AccountId;

            // Check the token with minimal buffer time.
            bool tokenOk = CheckToken(3);
            if (!tokenOk)
            {
                // We could store the parameters of the requested operation 
                // so it could be restarted automatically.
                // But since it should be rare to have a token issue here,
                // we'll make the user re-enter the form data after 
                // authentication.
                RequestItemsService.EgName = EgName;
                return Redirect("/ds/mustAuthenticate");
            }

            X509Certificate2 cert = this.Request.HttpContext.Connection.ClientCertificate;

            string signerNameFromCert = string.Empty;

            if (cert == null)
            {
                byte[] clientCertBytes = Convert.FromBase64String(Request.Headers["X-ARR-ClientCert"]);
                cert = (new X509Certificate2(clientCertBytes));

                //if (cert.Subject.IndexOf("CN") > 0)
                //    signerNameFromCert = cert.Subject.Substring(cert.Subject.IndexOf("CN"));
                //else
                //    signerNameFromCert = cert.Subject;
            }
            else
            {
                Console.Out.WriteLine("Subject" + cert.Subject);
                Console.Out.WriteLine("cert" + cert.ToString());
                //string clientCertFromHeader = Request.Headers["X-ARR-ClientCert"];            

            }

            if (cert.Subject.IndexOf("CN") > 0)
                signerNameFromCert = cert.Subject.Substring(cert.Subject.IndexOf("CN"));
            else
                signerNameFromCert = cert.Subject.Substring(1,100);

            string redirectUrl = DoWork(signerEmail, signerNameFromCert, accessToken, basePath, accountId, fileTobeSigned);
            //string redirectUrl = DoWork(signerEmail, cert.Subject, accessToken, basePath, accountId,fileTobeSigned);
            // Redirect the user to the Signing Ceremony
            return Redirect(redirectUrl);
        }
        public  string DownloadFileViaRestAPI( string fileName)
        {
            string siteURL = "https://nih1.sharepoint.com/sites/SunFish/";
            string webUrl = siteURL;
            var path = ".";
            var documentLibName = "ApprovalDocuments";
            webUrl = webUrl.EndsWith("/") ? webUrl.Substring(0, webUrl.Length - 1) : webUrl;
            string webRelativeUrl = null;
            if (webUrl.Split('/').Length > 3)
            {
                webRelativeUrl = "/" + webUrl.Split(new char[] { '/' }, 4)[3];
            }
            else
            {
                webRelativeUrl = "";
            }

            using (WebClient client = new WebClient())
            {
               // Uri endpointUri = new Uri(webUrl + "/_api/web/GetFileByServerRelativeUrl('" + webRelativeUrl + "/" + documentLibName + "/" + fileName + "')/$value");
                Uri endpointUri = new Uri(webUrl + "/_api/web/lists/getbytitle('" + documentLibName + "')");
                //var httpWebRequest = WebRequest.Create(endpointUri) as HttpWebRequest;
                //httpWebRequest.Credentials = credentials;
                //httpWebRequest.Headers.Add("X-FORMS_BASED_AUTH_ACCEPTED", "f");

                //httpWebRequest.Headers.Add("ContentType", "application/json;odata=verbose");
                //httpWebRequest.Headers.Add("Accept", "application/json;odata=verbose");

                //httpWebRequest.Method = "GET";
                // httpWebRequest.UseDefaultCredentials = true;
                //   httpWebRequest.PreAuthenticate = true;

                // var response = httpWebRequest.GetResponse();

                // Console.Out.WriteLine("response" + response.ToString());

                //var stream = response.GetResponseStream();
                //string strdata;

                //using (var reader = new StreamReader(stream))
                //{
                //    strdata = reader.ReadToEnd();
                //}
                //byte[] data = Encoding.UTF8.GetBytes(strdata);


                client.Headers.Add("X-FORMS_BASED_AUTH_ACCEPTED", "f");
                client.Credentials = credentials;
                client.Headers.Add(HttpRequestHeader.ContentType, "application/json;odata=verbose");
                client.Headers.Add(HttpRequestHeader.Accept, "application/json;odata=verbose");
                

                //ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // .NET 4.0

                //
                byte[] data = client.DownloadData(endpointUri);
                FileStream outputStream = new FileStream(path + fileName, FileMode.OpenOrCreate | FileMode.Append, FileAccess.Write, FileShare.None);
                outputStream.Write(data, 0, data.Length);
                outputStream.Flush(true);
                outputStream.Close();
            }

            return path + fileName;
        }

        [HttpGet]
        public IActionResult handlegGet(string fileName)
        {

            fileTobeSigned = fileName;
            // Check that the token is valid and will remain valid for awhile to enable the
            // user to fill out the form. If the token is not available, now is the time
            // to have the user authenticate or re-authenticate.
            bool tokenOk = CheckToken();

            if (tokenOk)
            {
                //addSpecialAttributes(model);
                ViewBag.envelopeOk = RequestItemsService.EnvelopeId != null;
                ViewBag.documentsOk = RequestItemsService.EnvelopeDocuments != null;
                ViewBag.documentOptions = RequestItemsService.EnvelopeDocuments?.Documents;
                ViewBag.gatewayOk = Config.GatewayAccountId != null && Config.GatewayAccountId.Length > 25;
                ViewBag.templateOk = RequestItemsService.TemplateId != null;
                ViewBag.source = CreateSourcePath();
                ViewBag.documentation = Config.documentation + EgName;
                ViewBag.showDoc = Config.documentation != null;
                InitializeInternal();

                return View(EgName, this);
            }

            RequestItemsService.EgName = EgName;

            return Redirect("/ds/mustAuthenticate");
        }
    }
}