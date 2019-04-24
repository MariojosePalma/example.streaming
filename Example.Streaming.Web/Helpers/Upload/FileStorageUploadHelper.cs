using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Example.Streaming.Web.Helpers.Upload.DependantClasses;
using Example.Streaming.Web.Helpers.Upload.Models;

namespace Example.Streaming.Web.Helpers.Upload
{
    public static class FileStorageUploadHelper
    {

        public static async Task<HttpResponseUploadModel> StreamFileToAzureBlobStorage(this HttpRequest request, string folder, CloudStorageAccount blobAccount, FormOptions _defaultFormOptions, List<string> logDetails)
        {

            // Setup Azure Blob Storage
            CloudBlobClient blobClient = blobAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(folder);
            CloudBlockBlob blockBlob = null;

            // Check if HttpRequest (Form Data) is a Multipart Content Type
            if (!MultipartRequestHelper.IsMultipartContentType(request.ContentType))
            {
                Console.WriteLine($"****: Expected a multipart request, but got { request.ContentType}");
                throw new Exception($"Expected a multipart request, but got {request.ContentType}");
            }
            else
            {
                Console.WriteLine($"****: Received a multipart Request.ContentType: { request.ContentType}");
            }

            // Create a Collection of KeyValue Pairs.
            var formAccumulator = new KeyValueAccumulator();

            // Determine the Multipart Boundary.
            var boundary = MultipartRequestHelper.GetBoundary(MediaTypeHeaderValue.Parse(request.ContentType), _defaultFormOptions.MultipartBoundaryLengthLimit);

            // Setup a Multipart Reader using the determined 'Section Boundary' and the 'Body' of the HttpRequest.
            var reader = new MultipartReader(boundary, request.Body);

            // Read the next Multipart Section inside the 'Body' of the Request.

            Console.Write("****: Begin Read of Section: " + DateTime.Now.ToString());
            logDetails.Add("****: Begin Read of Section: " + DateTime.Now.ToString());
            var section = await reader.ReadNextSectionAsync();
            Console.Write("****: End Read of Section: " + DateTime.Now.ToString());
            logDetails.Add("****: End Read of Section: " + DateTime.Now.ToString());


            Console.WriteLine("****: Current Multipart Section Size: " + section.Body.Length.ToString());

            // Loop through each 'Section', starting with the current 'Section'.
            while (section != null)
            {
                // Check if the current 'Section' has a ContentDispositionHeader.
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out ContentDispositionHeaderValue contentDisposition);

                //var disposition = ContentDispositionHeaderValue.Parse(section.ContentDisposition);

                if (hasContentDispositionHeader)
                {
                    if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                    {
                        try
                        {
                            string fileName = System.Web.HttpUtility.UrlEncode(contentDisposition.FileName.Value.Replace("\"", ""), Encoding.UTF8);
                            blockBlob = container.GetBlockBlobReference(string.Format("{0}{1}", Guid.NewGuid().ToString(), Path.GetExtension(fileName)));
                            blockBlob.Properties.ContentType = MimeTypeHelper.GetMimeType(fileName);
                            blockBlob.Metadata.Add("LocalFileName", fileName);
                            blockBlob.Metadata.Add("TenantID", "136af1ab-dce8-458f-af5c-1c7bd2711dd3");
                            //TODO: Revise comment here.
                            blockBlob.Properties.ContentDisposition = "attachment; filename*=UTF-8''" + fileName;

                            // Upload the current 'Section' including properties to Azure Blob Storage.
                            Console.Write("****: Begin Upload of Section to Azure: " + DateTime.Now.ToString() + " : " + fileName);
                            logDetails.Add("****: Begin Upload of Section to Azure: " + DateTime.Now.ToString() + " : " + fileName);
                            await blockBlob.UploadFromStreamAsync(section.Body);
                            Console.Write("****: End Upload of Section to Azure: " + DateTime.Now.ToString() + " : " + fileName);
                            logDetails.Add("****: End Upload of Section to Azure: " + DateTime.Now.ToString() + " : " + fileName);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                    else if (MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition))
                    {
                        var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name);
                        var encoding = GetEncoding(section);
                        using (var streamReader = new StreamReader(
                            section.Body,
                            encoding,
                            detectEncodingFromByteOrderMarks: true,
                            bufferSize: 1024,
                            leaveOpen: true))
                        {
                            var value = await streamReader.ReadToEndAsync();
                            if (String.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase))
                            {
                                value = String.Empty;
                            }
                            formAccumulator.Append(key.Value, value);

                            if (formAccumulator.ValueCount > _defaultFormOptions.ValueCountLimit)
                            {
                                throw new InvalidDataException($"Form key count limit {_defaultFormOptions.ValueCountLimit} exceeded.");
                            }
                        }
                    }
                }
                // Begin reading the next 'Section' inside the 'Body' of the Request.
                Console.Write("****: Begin Read of Section: " + DateTime.Now.ToString());
                logDetails.Add("****: Begin Read of Section: " + DateTime.Now.ToString());
                section = await reader.ReadNextSectionAsync();
                Console.Write("****: End Read of Section: " + DateTime.Now.ToString());
                logDetails.Add("****: End Read of Section: " + DateTime.Now.ToString());
            }

            var formValueProvider = new FormValueProvider(BindingSource.Form, new FormCollection(formAccumulator.GetResults()), CultureInfo.CurrentCulture);

            return new HttpResponseUploadModel { FormValueProvider = formValueProvider, Url = blockBlob?.Uri.ToString(), logDetails = logDetails };
        }

        private static Encoding GetEncoding(MultipartSection section)
        {
            var hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out MediaTypeHeaderValue mediaType);
            // UTF-7 is insecure and should not be honored. UTF-8 will succeed in 
            // most cases.
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
            {
                return Encoding.UTF8;
            }
            return mediaType.Encoding;
        }
    }

}

