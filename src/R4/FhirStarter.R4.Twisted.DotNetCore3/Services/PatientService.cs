using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FhirStarter.R4.Detonator.DotNetCore3.Interface;
using FhirStarter.R4.Detonator.DotNetCore3.SparkEngine.Core;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FhirStarter.R4.Twisted.DotNetCore3.Services
{
    public class PatientService : IFhirService
    {
        public string GetServiceResourceReference()
        {
            return nameof(Patient);
        }


        public Base Create(IKey key, Base resource)
        {
            throw new Exception("SHould not get here");
        }

        public Base Create(IKey key,Resource resource)
        {
            var test = (Patient) resource;
            return test;
        }

        public Base Read(SearchParams searchParams)
        {
            var patient = new Patient {Name = new List<HumanName>()};
            var humanName = new HumanName { Family = "yo", Given = new List<string> { "yo", "next"}};
            patient.Name.Add(humanName);
            return patient;
        }

        public Base Read(string id)
        {
            if (id.Equals("Fail"))
            {
                throw new ArgumentException($"Could not find patient by id {id}");
            }
            var patient = new Patient { Name = new List<HumanName>() };
            var humanName = new HumanName { Family = "yo", Given = new List<string> { "yo", "next" } };
            patient.Name.Add(humanName);
            return patient;
        }

        public Task<(Base resource, bool created)> UpdateAsync(IKey key, Resource resource)
        {
            throw new NotImplementedException();
        }

        public ActionResult Delete(IKey key)
        {
            throw new NotImplementedException();
        }

        public ActionResult Patch(IKey key, Resource resource)
        {
            throw new NotImplementedException();
        }

        public CapabilityStatement.RestComponent GetRestDefinition()
        {
            throw new NotImplementedException();
        }

        public OperationDefinition GetOperationDefinition(HttpRequest request)
        {
            throw new NotImplementedException();
        }

        public string GetStructureDefinitionNameForResourceProfile()
        {
            throw new NotImplementedException();
        }
    }
}
