using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Example.Streaming.Web.Helpers.Upload;
using Example.Streaming.Web.Helpers.Upload.CustomAttributes;
using Example.Streaming.Web.Helpers.Upload.Models;
using Example.Streaming.Web.Models;

namespace Example.Streaming.Web.Controllers
{
    public class UploadController : Controller
    {
        private readonly AzureStorageConfigModel storageConfig = null;
        private readonly ILogger<UploadController> _logger;
        private static readonly FormOptions _defaultFormOptions = new FormOptions();

        private List<string> logDetails = new List<string>();

        public UploadController(IOptions<AzureStorageConfigModel> config, ILogger<UploadController> logger)
        {
            storageConfig = config.Value;
            _logger = logger;
        }

        [HttpPost]
        [DisableFormValueModelBinding]
        [RequestSizeLimit(100000000)]
        public async Task<IActionResult> UploadFile()
        {
            Console.WriteLine("****: Received Upload Request");
            _logger.LogInformation("****: Received a Rquest to UploadAzureFiles : " + DateTime.Now.ToString());
            logDetails.Add("****: Received a Rquest to UploadAzureFiles : " + DateTime.Now.ToString());

            // Create storagecredentials object by reading the values from the configuration (appsettings.json)
            StorageCredentials storageCredentials = new StorageCredentials(storageConfig.AccountName, storageConfig.AccountKey);

            // Create cloudstorage account by passing the storagecredentials
            CloudStorageAccount storageAccount = new CloudStorageAccount(storageCredentials, true);

            // Create the blob client.
            //CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Get reference to the blob container by passing the name by reading the value from the configuration (appsettings.json)
            //CloudBlobContainer container = blobClient.GetContainerReference(storageConfig.ContainerName);

            // Get the reference to the block blob from the container
            //CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);

            //var myR = FileStreamingHelper.StreamFile(Request, )
            HttpResponseUploadModel myResponse = new HttpResponseUploadModel();
            try
            {
                myResponse = await FileStorageUploadHelper.StreamFileToAzureBlobStorage(Request, storageConfig.ContainerName, storageAccount, _defaultFormOptions, logDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError("****: " + ex.Message.ToString() + "{@ex}", ex);
            }


            _logger.LogInformation("****: Finished Request to UploadAzureFiles : " + DateTime.Now.ToString());
            logDetails.Add("****: Finished Request to UploadAzureFiles : " + DateTime.Now.ToString());

            string finalLogDetails = string.Empty;
            foreach (string s in logDetails)
            {
                finalLogDetails = finalLogDetails + s + " \n ";

            }


            return Ok("FormValueProvider: " + myResponse.FormValueProvider + "\n\nUrl: " + myResponse.Url + "\n\nLogs:\n\n " + finalLogDetails);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
