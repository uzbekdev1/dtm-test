using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using DTM.Test.OMR.Helpers;

namespace DTM.Test.Api.Controllers
{
    public class TestController : ApiController
    {

        [HttpGet]
        [ActionName("scanner")]
        public IHttpActionResult Scanner()
        {
            var root = ConfigurationManager.AppSettings["EngineExcutableDirectory"];
            var filePath = Path.Combine(root, "OmrScannerApplication.zip");

            return Ok(filePath);
        }

        [HttpPost]
        [ActionName("analizy")]
        public IHttpActionResult Analizy([FromBody]string file)
        {
            if (string.IsNullOrWhiteSpace(file))
                return BadRequest();

            var root = ConfigurationManager.AppSettings["EngineUploadDirectory"];
            var filePath = Path.Combine(root, file);

            using (var parser = new ParserHelper(filePath))
            {
                parser.Analizy();

                var model = parser.GetResult();

                if (model == null)
                    return InternalServerError(new Exception(parser.OutputMessage));

                return Ok(model);
            }
        }

    }
}