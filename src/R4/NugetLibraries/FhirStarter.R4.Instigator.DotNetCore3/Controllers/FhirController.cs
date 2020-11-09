using System;
using System.Linq;
using System.Net;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using FhirStarter.R4.Detonator.DotNetCore3.Interface;
using FhirStarter.R4.Detonator.DotNetCore3.SparkEngine.Core;
using FhirStarter.R4.Detonator.DotNetCore3.SparkEngine.Extensions;
using FhirStarter.R4.Instigator.DotNetCore3.Helper;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace FhirStarter.R4.Instigator.DotNetCore3.Controllers
{
    public partial class FhirController : Controller
    {
        #region HelperMethods
        private ActionResult HandleServiceReadWithSearchParams(string type)
        {
            var searchParams = Request.GetSearchParams();
            var service = ControllerHelper.GetFhirService(type, HttpContext.RequestServices);
            if (service == null) throw new ArgumentException($"The resource {type} service does not exist!");
            var result = service.Read(searchParams);
            return HandleResult(result);
        }

        private ActionResult HandleServiceRead(string type, string id)
        {
            var service = ControllerHelper.GetFhirService(type, HttpContext.RequestServices);
            if (service == null) throw new ArgumentException($"The resource {type} service does not exist!");
            var result = service.Read(id);
            return HandleResult(result);
        }

        private ActionResult HandleResult(Base result)
        {
            if (_validationEnabled)
            {
                result = HandleValidation(result);
            }

            if (result is OperationOutcome)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        private Base HandleValidation(Base result)
        {
            var validation = _profileValidator.Validate((Resource)result);
            if (validation.Success) return result;
            if (!_returnValidatedResource) return validation;
            var xmlString = new FhirXmlSerializer().SerializeToDocument(result).ToString();
            var htmlEncoder = HtmlEncoder.Create(new TextEncoderSettings());
            var htmlEncodedString = htmlEncoder.Encode(xmlString);

            validation.Text = new Narrative
            {
                Status = Narrative.NarrativeStatus.Generated,
                Div = htmlEncodedString
            };

            return validation;
        }

        private ActionResult ResourceCreate(string type, Resource resource, IFhirBaseService service)
        {
            var headerAccept = Request.Headers.FirstOrDefault(t => t.Key.Equals("Accept"));
            if (headerAccept.Value.Equals("*/*"))
            {
                var contentType = Request.ContentType;
                Response.ContentType = contentType;
            }

            if (service == null || string.IsNullOrEmpty(type) || resource == null)
                return BadRequest($"Service for resource {nameof(resource)} must exist.");
            var key = Key.Create(type);
            var result = service.Create(key, resource);
            switch (result)
            {
                case null:
                    return BadRequest($"Service for resource {nameof(resource)} must exist.");
                case OperationOutcome _:
                    return BadRequest(result);
                default:
                    return Ok(result);
            }
        }

        public async Task<ActionResult> ResourceUpdate(string type, string id, Resource resource, IFhirBaseService service)
        {
            if (service == null || string.IsNullOrEmpty(type) || resource == null || string.IsNullOrEmpty(id))
                throw new ArgumentException("Service is null, cannot update resource of type " + type);
            if (!id.Equals(resource.Id))
                throw new FhirOperationException(
                    "Resource id in request body object does not match resource identifier in request url",
                    HttpStatusCode.BadRequest);
            var key = Key.Create(type, id);
            var (updatedResource, created) = await service.UpdateAsync(key, resource);
            if (created)
            {
                // If created, the resource is accessible with the same url as the PUT request
                return Created(Request.GetEncodedUrl(), updatedResource);
            }
            else
            {
                return Ok(updatedResource);
            }
        }

        public ActionResult ResourceDelete(string type, Key key, IFhirBaseService service)
        {
            if (service == null) return BadRequest($"Service is null, cannot update resource of {type}");
            var result = service.Delete(key);
            return result;
        }

        public ActionResult ResourcePatch(string type, IKey key, Resource resource, IFhirBaseService service)
        {
            if (service == null) return BadRequest($"Service is null, cannot update resource of {type}");
            var result = service.Patch(key, resource);
            return result;
        }

        #endregion HelperMethods

        [HttpGet, Route("{type}/{id}"), Route("{type}/identifier/{id}")]
        public ActionResult Read(string type, string id)
        {
            return HandleServiceRead(type, id);
        }

        [HttpGet, Route("fhir/{type}"), Route("{type}")]
        public ActionResult Read(string type)
        {
            return HandleServiceReadWithSearchParams(type);
        }

        [HttpGet, Route("")]
        // ReSharper disable once InconsistentNaming
        public ActionResult Query(string type, string _query)
        {
            return HandleServiceReadWithSearchParams(type);
        }

        // return 201 when created
        // return 202 when takes too long
        [HttpPost, Route("{type}")]
        public ActionResult Create(string type, [FromBody] Resource resource)
        {
            if (_validationEnabled)
            {
                resource = (Resource)ValidateResource(resource, true);
            }
            if (resource is OperationOutcome)
            {
                return BadRequest(resource);
            }
            var service = ControllerHelper.GetFhirService(type, HttpContext.RequestServices);
            return ResourceCreate(type, resource, service);
        }

        // return 201 when created
        // return 202 when takes too long
        [HttpPut, Route("{type}/{id}")]
        public async Task<ActionResult> Update(string type, string id, [FromBody] Resource resource)
        {
            if (_validationEnabled)
            {
                resource = (Resource)ValidateResource(resource, true);
            }
            if (resource is OperationOutcome)
            {
                return BadRequest(resource);
            }
            var service = ControllerHelper.GetFhirService(type, HttpContext.RequestServices);
            return await ResourceUpdate(type, id, resource, service);
        }

        // return 201 when created
        // return 202 when takes too long
        [HttpDelete, Route("{type}/{id}")]
        public ActionResult Delete(string type, string id)
        {
            var service = ControllerHelper.GetFhirService(type, HttpContext.RequestServices);
            if (service != null)
            {
                var result = ResourceDelete(type, Key.Create(id), service);
                return result;
            }
            return BadRequest($"Service is null, cannot delete resource of {type} and id {id}");
        }

        // return 201 when created
        // return 202 when takes too long
        [HttpPatch, Route("{type}/{id}")]
        public ActionResult Patch(string type, string id, [FromBody] Resource resource)
        {
            var service = ControllerHelper.GetFhirService(type, HttpContext.RequestServices);
            if (service != null)
            {
                var result = ResourcePatch(type, Key.Create(id), resource, service);
                return result;
            }
            return BadRequest($"Service is null, cannot delete resource of {type} and id {id}");
        }

        [HttpGet, Route("metadata")]
        public ActionResult MetaData()
        {
            var httpRequest = HttpContext.Request;

            var metaData = ControllerHelper.CreateMetaData(_fhirServices, _appSettings, httpRequest);
            return Ok(metaData);
        }
    }
}
