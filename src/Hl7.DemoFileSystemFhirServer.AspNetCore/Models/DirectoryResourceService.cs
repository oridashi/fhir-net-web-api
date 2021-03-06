﻿using System;
using System.Collections.Generic;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.WebApi;
using Hl7.Fhir.Utility;
using System.Threading.Tasks;
using System.Linq;

namespace Hl7.DemoFileSystemFhirServer
{
    public class DirectoryResourceService : Hl7.Fhir.WebApi.IFhirResourceServiceSTU3<IServiceProvider>
    {
        public ModelBaseInputs<IServiceProvider> RequestDetails { get; set; }

        public string ResourceName { get; set; }

        public Task<Resource> Create(Resource resource, string ifMatch, string ifNoneExist, DateTimeOffset? ifModifiedSince)
        {
            if (String.IsNullOrEmpty(resource.Id))
                resource.Id = Guid.NewGuid().ToFhirId();
            if (resource.Meta == null)
                resource.Meta = new Meta();
            resource.Meta.LastUpdated = DateTime.Now;
            string path = System.IO.Path.Combine(DirectorySystemService.Directory, $"{resource.TypeName}.{resource.Id}.{resource.Meta.VersionId}.xml");
            System.IO.File.WriteAllText(
                path,
                new Hl7.Fhir.Serialization.FhirXmlSerializer().SerializeToString(resource));
            resource.SetAnnotation<CreateOrUpate>(CreateOrUpate.Create);
            return System.Threading.Tasks.Task.FromResult(resource);
        }

        public Task<string> Delete(string id, string ifMatch)
        {
            string path = System.IO.Path.Combine(DirectorySystemService.Directory, $"{this.ResourceName}.{id}..xml");
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
            return System.Threading.Tasks.Task.FromResult<string>(null);
        }

        public Task<Resource> Get(string resourceId, string VersionId, SummaryType summary)
        {
            string path = System.IO.Path.Combine(DirectorySystemService.Directory, $"{this.ResourceName}.{resourceId}.{VersionId}.xml");
            if (System.IO.File.Exists(path))
                return System.Threading.Tasks.Task.FromResult(new Fhir.Serialization.FhirXmlParser().Parse<Resource>(System.IO.File.ReadAllText(path)));
            return System.Threading.Tasks.Task.FromResult<Resource>(null);
        }

        public Task<CapabilityStatement.ResourceComponent> GetRestResourceComponent()
        {
            throw new NotImplementedException();
        }

        public Task<Bundle> InstanceHistory(string ResourceId, DateTimeOffset? since, DateTimeOffset? Till, int? Count, SummaryType summary)
        {
            throw new NotImplementedException();
        }

        public Task<Resource> PerformOperation(string operation, Parameters operationParameters, SummaryType summary)
        {
            if (operation == "count-em")
            {
                var result = new OperationOutcome();
                result.Issue.Add(new OperationOutcome.IssueComponent()
                {
                    Code = OperationOutcome.IssueType.Informational,
                    Severity = OperationOutcome.IssueSeverity.Information,
                    Details = new CodeableConcept(null, null, $"{ResourceName} resource instances: {System.IO.Directory.EnumerateFiles(DirectorySystemService.Directory, $"{ResourceName}.*.xml").Count()}")
                });
                return System.Threading.Tasks.Task.FromResult<Resource>(result);
            }

            throw new NotImplementedException();
        }

        public Task<Resource> PerformOperation(string id, string operation, Parameters operationParameters, SummaryType summary)
        {
            throw new NotImplementedException();
        }

        public Task<Bundle> Search(IEnumerable<KeyValuePair<string, string>> parameters, int? Count, SummaryType summary)
        {
            throw new NotImplementedException();
            //Bundle result = new Bundle();
            //result.Meta = new Meta();
            //result.Id = new Uri("urn:uuid:" + Guid.NewGuid().ToString("n")).OriginalString;
            //result.Type = Bundle.BundleType.Searchset;
            //result.ResourceBase = RequestDetails.BaseUri;

            // Check that the Last Update value is correctly entered and that the count is at least as big as the data included
            // and update the links
            // result.ProcessLastModifiedFromEntriesAndLinks(Request.RequestUri, pagesize, pagenumber, snapshotID);
        }

        public Task<Bundle> TypeHistory(DateTimeOffset? since, DateTimeOffset? Till, int? Count, SummaryType summary)
        {
            Bundle result = new Bundle();
            result.Meta = new Meta()
            {
                LastUpdated = DateTime.Now
            };
            result.Id = new Uri("urn:uuid:" + Guid.NewGuid().ToString("n")).OriginalString;
            result.Type = Bundle.BundleType.History;

            var parser = new Fhir.Serialization.FhirXmlParser();
            var files = System.IO.Directory.EnumerateFiles(DirectorySystemService.Directory, $"{ResourceName}.*.xml");
            foreach (var filename in files)
            {
                var resource = parser.Parse<Resource>(System.IO.File.ReadAllText(filename));
                result.AddResourceEntry(resource,
                    ResourceIdentity.Build(RequestDetails.BaseUri,
                        resource.ResourceType.ToString(),
                        resource.Id,
                        resource.Meta.VersionId).OriginalString);
            }
            result.Total = result.Entry.Count;

            // also need to set the page links

            return System.Threading.Tasks.Task.FromResult(result);
        }
    }
}
