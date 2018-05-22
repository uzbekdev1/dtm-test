using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace DTM.Test.Api.Controllers
{
    public class FileController : ApiController
    {
        [HttpPost]
        [ActionName("upload")]
        public async Task<IHttpActionResult> Upload()
        {
            if (!Request.Content.IsMimeMultipartContent())
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);

            var root = ConfigurationManager.AppSettings["EngineUploadDirectory"];
            var provider = new MultipartFormDataStreamProvider(root);
            var result = await Request.Content.ReadAsMultipartAsync(provider);
            var files = new List<string>();

            foreach (var file in result.FileData)
            {
                var fileInfo = new FileInfo(file.LocalFileName);
                var fileName = Guid.NewGuid() + file.Headers.ContentDisposition.FileName.Replace("\"", "");
                var filePath = Path.Combine(root, fileName);

                File.Move(fileInfo.FullName, filePath);

                files.Add(fileName);
            }

            return Ok(files);
        }


        [HttpGet]
        [ActionName("download")]
        public HttpResponseMessage Download(string path)
        {
            if (String.IsNullOrWhiteSpace(path))
                return Request.CreateResponse(HttpStatusCode.BadRequest);

            var root = ConfigurationManager.AppSettings["EngineUploadDirectory"];
            var filePath = Path.Combine(root, path);

            if (!File.Exists(filePath))
                return Request.CreateResponse(HttpStatusCode.Gone);

            var result = Request.CreateResponse(HttpStatusCode.OK);

            result.Content = new StreamContent(new FileStream(filePath, FileMode.Open, FileAccess.Read));
            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = path
            };

            return result;
        }

    }
}