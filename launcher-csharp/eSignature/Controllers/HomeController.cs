using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DocuSign.CodeExamples.Common;
using Microsoft.AspNetCore.Mvc;
using DocuSign.CodeExamples.Models;
using DocuSign.eSign.Api;
using DocuSign.eSign.Client;
using DocuSign.eSign.Model;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace DocuSign.CodeExamples.Controllers
{
    public class HomeController : Controller
    {
        private IRequestItemsService _requestItemsService { get; }
        private IConfiguration _configuration { get;  }

        public HomeController(IRequestItemsService requestItemsService, IConfiguration configuration)
        {
            _requestItemsService = requestItemsService;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            if (_configuration["quickstart"] == "true")
            {
                if (this.User.Identity.IsAuthenticated)
                {
                    _configuration["quickstart"] = "false";
                }
                return Redirect("eg001");
            }
            string egName = _requestItemsService.EgName;
            if (!string.IsNullOrWhiteSpace(egName))
            {
                _requestItemsService.EgName = null;
                return Redirect(egName);
            }

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Route("/dsReturn")]
        public IActionResult DsReturn(string state, string @event, string envelopeId)
        {            
            ViewBag.title = "Return from DocuSign";
            ViewBag._event = @event;
            ViewBag.state = state;
            ViewBag.envelopeId = envelopeId;
            //Retrieve the Document from envelopID and store it

            return Create("combined");
        }
        
        public ActionResult Create(string docSelect)
        {
            // Data for this method
            // docSelect -- argument
            var accessToken = _requestItemsService.User.AccessToken;
            var basePath = _requestItemsService.Session.BasePath + "/restapi";
            var accountId = _requestItemsService.Session.AccountId;
            var envelopeId = _requestItemsService.EnvelopeId;
            var apiClient = new ApiClient(basePath);
            

            
            // documents data for the envelope. See example EG006
            var envelopesApi = new EnvelopesApi(apiClient);
            apiClient.Configuration.DefaultHeader.Add("Authorization", "Bearer " + accessToken);

            EnvelopeDocumentsResult results = envelopesApi.ListDocuments(accountId, envelopeId);
            List<EnvelopeDocItem> envelopeDocItems = new List<EnvelopeDocItem>
            {
                new EnvelopeDocItem { Name = "Combined", Type = "content", DocumentId = "combined" },
                new EnvelopeDocItem { Name = "Zip archive", Type = "zip", DocumentId = "archive" }
            };

            foreach (EnvelopeDocument doc in results.EnvelopeDocuments)
            {
                envelopeDocItems.Add(new EnvelopeDocItem
                {

                    DocumentId = doc.DocumentId,
                    Name = doc.DocumentId == "certificate" ? "Certificate of completion" : doc.Name,
                    Type = doc.Type
                });
            }

            bool tokenOk = CheckToken(3);
            if (!tokenOk)
            {
                // We could store the parameters of the requested operation 
                // so it could be restarted automatically.
                // But since it should be rare to have a token issue here,
                // we'll make the user re-enter the form data after 
                // authentication.
                _requestItemsService.EgName = "Home";
                return Redirect("/ds/mustAuthenticate");
            }

            FileStreamResult result = DoWork(accessToken, basePath, accountId,
                envelopeId, envelopeDocItems, docSelect);
            return result;
        }
        
        protected bool CheckToken(int bufferMin = 60)
        {
            return _requestItemsService.CheckToken(bufferMin);
        }
        private FileStreamResult DoWork(string accessToken, string basePath, string accountId,
            string envelopeId, List<EnvelopeDocItem> documents, string docSelect)
        {
            // Data for this method
            // accessToken
            // basePath
            // accountId
            // envelopeId
            // docSelect -- the requested documentId 
            // documents -- from eg 6
            var apiClient = new ApiClient(basePath);
            apiClient.Configuration.DefaultHeader.Add("Authorization", "Bearer " + accessToken);
            var envelopesApi = new EnvelopesApi(apiClient);

            // Step 1. EnvelopeDocuments::get.
            // Exceptions will be caught by the calling function
            System.IO.Stream results = envelopesApi.GetDocument(accountId,
                            envelopeId, docSelect);

            // Step 2. Look up the document from the list of documents 
            EnvelopeDocItem docItem = documents.FirstOrDefault(d => docSelect.Equals(d.DocumentId));
            // Process results. Determine the file name and mimetype
            string docName = docItem.Name;
            bool hasPDFsuffix = docName.ToUpper().EndsWith(".PDF");
            bool pdfFile = hasPDFsuffix;
            // Add .pdf if it's a content or summary doc and doesn't already end in .pdf
            string docType = docItem.Type;
            if (("content".Equals(docType) || "summary".Equals(docType)) && !hasPDFsuffix)
            {
                docName += ".pdf";
                pdfFile = true;
            }
            // Add .zip as appropriate
            if ("zip".Equals(docType))
            {
                docName += ".zip";
            }
            // Return the file information
            // See https://stackoverflow.com/a/30625085/64904
            string mimetype;
            if (pdfFile)
            {
                mimetype = "application/pdf";
            }
            else if ("zip".Equals(docType))
            {
                mimetype = "application/zip";
            }
            else
            {
                mimetype = "application/octet-stream";
            }

            return File(results, mimetype, docName);
        }
        
    }
}
