// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}

// Learn how to locally debug an Event Grid-triggered function:
//    https://aka.ms/AA30pjh

// Use for local testing:
//   https://{ID}.ngrok.io/runtime/webhooks/EventGrid?functionName=Thumbnail

using Azure.Storage.Blobs;
using Azure.Storage;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ImageFunctions
{
    public static class Thumbnail
    {
        private static readonly string BLOB_STORAGE_CONNECTION_STRING = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        private static string GetBlobNameFromUrl(string bloblUrl)
        {
            var uri = new Uri(bloblUrl);
            var blobClient = new BlobClient(uri);
            return blobClient.Name;
        }

        private static IImageEncoder GetEncoder(string extension)
        {
            IImageEncoder encoder = null;

            extension = extension.Replace(".", "");

            var isSupported = Regex.IsMatch(extension, "gif|png|jpe?g", RegexOptions.IgnoreCase);

            if (isSupported)
            {
                switch (extension.ToLower())
                {
                    case "png":
                        encoder = new PngEncoder();
                        break;
                    case "jpg":
                        encoder = new JpegEncoder();
                        break;
                    case "jpeg":
                        encoder = new JpegEncoder();
                        break;
                    case "gif":
                        encoder = new GifEncoder();
                        break;
                    default:
                        break;
                }
            }

            return encoder;
        }

        [FunctionName("Thumbnail")]
        public static async Task Run(
            [EventGridTrigger]EventGridEvent eventGridEvent,
            ILogger log)
        {
            try
            {
                    var accountName = Environment.GetEnvironmentVariable("ACCOUNT_NAME");
                    var accountKey = Environment.GetEnvironmentVariable("ACCOUNT_KEY");
                    var thumbnailWidth = Convert.ToInt32(Environment.GetEnvironmentVariable("THUMBNAIL_WIDTH"));
                    var thumbContainerName = Environment.GetEnvironmentVariable("THUMBNAIL_CONTAINER_NAME");
                    var imageContainerName = Environment.GetEnvironmentVariable("IMAGE_CONTAINER_NAME");
                    var createdEvent = ((JObject)eventGridEvent.Data).ToObject<StorageBlobCreatedEventData>();
                    var extension = Path.GetExtension(createdEvent.Url);
                    var encoder = GetEncoder(extension);
                    var blobClient = new BlobClient(new Uri(reatedEvent.Url));
                    var thumbUri = createdEvent.Url.Replace(imageContainerName);
                    if (encoder != null)
                    {

                        var blobServiceClient = new BlobServiceClient(BLOB_STORAGE_CONNECTION_STRING);
                        var blobContainerClient = blobServiceClient.GetBlobContainerClient(thumbContainerName);
                        var blobName = GetBlobNameFromUrl(createdEvent.Url);

                        var imageStream = new MemoryStream();
                        var resizedImageStream = new MemoryStream();
                        await blobClient.DownloadToAsync(imageStream);
                        log.LogInformation($"Resizeing image from {createdEvent.Uri}");
                        // Read the image using SixLabors.ImageSharp
                        imageStream.Position= 0;
                        var image = Image.Load(imageStream);

                        var divisor = image.Width / thumbnailWidth;
                        var height = Convert.ToInt32(Math.Round((decimal)(image.Height / divisor)));
                        image.Mutate(x => x.Resize(thumbnailWidth, height));
                        image.Save(resizedImageStream, encoder);
                        resizedImageStream.Position = 0;
                        log.LogInformation($"Uploading thumb to {thumbUri}");
                        Uri blobUri = new Uri(thumbUri);

                        StorageSharedKeyCredential storageCredentials =
                            new StorageSharedKeyCredential(accountName, accountKey);

                        // Create the blob client.
                        BlobClient blobClient2 = new BlobClient(blobUri, storageCredentials);

                        // Upload the file
                        await blobClient2.UploadAsync(resizedImageStream);
                    }
                    else
                    {
                        log.LogInformation($"No encoder support for: {createdEvent.Url}");
                    }
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
                throw;
            }
        }
    }
}
