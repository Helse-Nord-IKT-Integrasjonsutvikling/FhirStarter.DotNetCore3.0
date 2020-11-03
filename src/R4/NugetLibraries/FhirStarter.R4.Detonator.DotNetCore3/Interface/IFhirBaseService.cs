using System;
using System.Threading.Tasks;
using FhirStarter.R4.Detonator.DotNetCore3.SparkEngine.Core;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Mvc;

namespace FhirStarter.R4.Detonator.DotNetCore3.Interface
{
    public interface IFhirBaseService
    {
        string GetServiceResourceReference();
        Base Create(IKey key, Resource resource);
        Base Read(SearchParams searchParams);
        Base Read(string id);
        Task<Tuple<Base, bool>> UpdateAsync(IKey key, Resource resource);
        ActionResult Delete(IKey key);
        ActionResult Patch(IKey key, Resource resource);
    }
}
