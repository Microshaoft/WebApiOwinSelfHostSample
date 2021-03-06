﻿namespace Microshaoft.WebApi.Controllers
{
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using System.Collections.Generic;
    /// <summary>
    /// This sample controller reads the contents of an HTML file upload asynchronously and writes one or more body parts to a local file.
    /// </summary>
    [RoutePrefix("api/restful/Files")]
    [Authorize(Roles = "Administrators")]
    public class FilesController : ApiController
    {
        static readonly string ServerUploadFolder = @"d:\temp\";//th.GetTempPath();

        [HttpPost]
        [Route("Upload")]
        public async Task<HttpResponseMessage> UploadFile()
        {
            if (!Request.Content.IsMimeMultipartContent("form-data"))
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.UnsupportedMediaType));
            }
            var streamProvider = new MultipartFormDataStreamProvider(ServerUploadFolder);
            await Request
                        .Content
                        .ReadAsMultipartAsync(streamProvider);
            var r = new FileResult
            {
                FileNames = streamProvider
                                .FileData
                                .Select
                                    (
                                        (entry) =>
                                        {
                                            string fileName = entry.Headers.ContentDisposition.FileName;
                                            if (fileName.StartsWith("\"") && fileName.EndsWith("\""))
                                            {
                                                fileName = fileName.Trim('"');
                                            }
                                            if (fileName.Contains(@"/") || fileName.Contains(@"\"))
                                            {
                                                fileName = Path.GetFileName(fileName);
                                            }
                                            File.Move(entry.LocalFileName, Path.Combine(ServerUploadFolder, fileName));
                                            return fileName;
                                        }
                                    )
                , Submitter = streamProvider.FormData["submitter"]
            };
            return
                Request
                    .CreateResponse<FileResult>
                        (r);
        }
    }
    /// <summary>
    /// This class is used to carry the result of various file uploads.
    /// </summary>
    public class FileResult
    {
        /// <summary>
        /// Gets or sets the local path of the file saved on the server.
        /// </summary>
        /// <value>
        /// The local path.
        /// </value>
        public IEnumerable<string> FileNames { get; set; }

        /// <summary>
        /// Gets or sets the submitter as indicated in the HTML form used to upload the data.
        /// </summary>
        /// <value>
        /// The submitter.
        /// </value>
        public string Submitter { get; set; }
    }
}