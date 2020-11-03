using System;
using System.Collections.Generic;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using static System.Net.HttpStatusCode;

namespace FhirStarter.R4.Detonator.DotNetCore3.Filter
{
    public class FhirExceptionFilter:IExceptionFilter
    {
        private readonly ILogger<FhirExceptionFilter> _logger;

        public FhirExceptionFilter(ILogger<FhirExceptionFilter> logger)
        {
            _logger = logger;
        }
        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, context.Exception.Message);
            var operationOutCome = new OperationOutcome { Issue = new List<OperationOutcome.IssueComponent>() };
            var issueCode = OperationOutcome.IssueType.Exception;
            try
            {
                var ex = context.Exception as FhirOperationException;
                issueCode = ex.Status switch
                {
                    NotFound => OperationOutcome.IssueType.NotFound,
                    BadRequest => OperationOutcome.IssueType.Incomplete,
                    _ => issueCode
                };
            }
            catch (Exception e)
            {
                _logger.LogDebug("Exception was not a FhirOperationException");
            }

            var issue = new OperationOutcome.IssueComponent
            {
                Severity = OperationOutcome.IssueSeverity.Fatal,
                Code = issueCode,
                Details = new CodeableConcept("StandardException", context.Exception.GetType().ToString(), context.Exception.Message),
                Diagnostics = context.Exception.StackTrace
            };

            operationOutCome.Issue.Add(issue);
            context.Result = new ObjectResult(operationOutCome);
            context.ExceptionHandled = true;
            
        }
    }
}
