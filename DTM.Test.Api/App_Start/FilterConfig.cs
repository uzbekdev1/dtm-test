using System.Web.Mvc;
using DTM.Test.Api.Filters;

namespace DTM.Test.Api
{
    public static class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new LogErrorAttribute()); 
        }
    }
}
