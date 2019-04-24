using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Example.Streaming.Web.Helpers.Upload.Models
{
    public class HttpResponseUploadModel
    {

        public FormValueProvider FormValueProvider;
        public string Url;
        public List<string> logDetails;

    }
}
