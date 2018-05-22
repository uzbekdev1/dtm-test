using System.Web.Mvc;
using DTM.Test.OMR.Helpers;

namespace DTM.Test.Api.Filters
{
    public class LogErrorAttribute : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            LogHelper.Logger.ErrorFormat("Api Exception:{0}", filterContext.Exception);
        }
    }
}