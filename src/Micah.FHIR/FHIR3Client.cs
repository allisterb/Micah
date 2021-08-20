extern alias stu3;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TTask = System.Threading.Tasks.Task;

using stu3::Hl7.Fhir.Rest;
using stu3::Hl7.Fhir.Model;
using Micah;

namespace Micah.FHIR
{

    public class FHIR3Client : Runtime 
    {
        #region Constructors
        public FHIR3Client(string endpoint) : base()
        {
            var settings = new Hl7.Fhir.Rest.FhirClientSettings
            {
                PreferredFormat = Hl7.Fhir.Rest.ResourceFormat.Json,
                VerifyFhirVersion = false,
                PreferredParameterHandling = Hl7.Fhir.Rest.SearchParameterHandling.Strict,
                ParserSettings = new Hl7.Fhir.Serialization.ParserSettings() { PermissiveParsing = true, AcceptUnknownMembers = true, AllowUnrecognizedEnums = true}
            };
            Client = new FhirClient(endpoint, settings);
            Initialized = Client != null;
        }
        #endregion

        #region Properties
        protected FhirClient Client { get; }
        #endregion

        #region Methods
        protected virtual TTask PrepClient() => TTask.CompletedTask;

        public async TTask CreateDemoPatient1(string pid)
        {
            // example Patient setup, fictional data only
            var pat = new Patient();

            var id = new Hl7.Fhir.Model.Identifier();
            id.System = "http://hl7.org/fhir/sid/us-ssn";
            id.Value = pid;
            pat.Identifier.Add(id);

            var name = new HumanName().WithGiven("Michael").WithGiven("C.H.").AndFamily("Park");
            name.Prefix = new string[] { "Mr." };
            name.Use = HumanName.NameUse.Official;

            var nickname = new HumanName();
            nickname.Use = HumanName.NameUse.Nickname;
            nickname.GivenElement.Add(new Hl7.Fhir.Model.FhirString("Mike"));

            pat.Name.Add(name);
            pat.Name.Add(nickname);

            pat.Gender = AdministrativeGender.Male;

            pat.BirthDate = "1983-04-23";

            var birthplace = new Hl7.Fhir.Model.Extension();
            birthplace.Url = "http://hl7.org/fhir/StructureDefinition/birthPlace";
            birthplace.Value = new Address() { City = "Seattle" };
            pat.Extension.Add(birthplace);

            var birthtime = new Hl7.Fhir.Model.Extension("http://hl7.org/fhir/StructureDefinition/patient-birthTime",
                                           new Hl7.Fhir.Model.FhirDateTime(1983, 4, 23, 7, 44));
            pat.BirthDateElement.Extension.Add(birthtime);

            var address = new Address()
            {
                Line = new string[] { "3300 Washtenaw Avenue, Suite 227" },
                City = "Ann Arbor",
                State = "MI",
                PostalCode = "48104",
                Country = "USA"
            };
            pat.Address.Add(address);

            var contact = new Patient.ContactComponent();
            contact.Name = new HumanName();
            contact.Name.Given = new string[] { "Susan" };
            contact.Name.Family = "Parks";
            contact.Gender = AdministrativeGender.Female;
            contact.Relationship.Add(new Hl7.Fhir.Model.CodeableConcept("http://hl7.org/fhir/v2/0131", "N"));
            contact.Telecom.Add(new Hl7.Fhir.Model.ContactPoint(Hl7.Fhir.Model.ContactPoint.ContactPointSystem.Phone, null, ""));
            pat.Contact.Add(contact);

            pat.Deceased = new Hl7.Fhir.Model.FhirBoolean(false);
            await PrepClient();
            await Client.CreateAsync<Patient>(pat);
        }
        public async Task<Bundle> Search<T>(string [] where, string orderBy = null, string include = null, int limit = -1) where T: Hl7.Fhir.Model.Resource
        {
            var q = new Hl7.Fhir.Rest.SearchParams();
            foreach(var w in where)
            {
                q = q.Where(w);
            }
            if (!ReferenceEquals(orderBy, null))
            {
                q = q.OrderBy(orderBy);
            }
            if (!ReferenceEquals(include, null))
            {
                q = q.Include(include);
            }
            if(limit != -1)
            {
                q = q.LimitTo(limit);
            }
            q = q.SummaryOnly()
                .LimitTo(5);
            
            return await Client.SearchAsync<T>(q);
        }

        public async Task<Bundle> SearchPatients(string[] where, string orderBy = null, string include = null, int limit = -1) => await Search<Patient>(where, orderBy, include, limit);
        
        public async Task<Bundle> SearchPatients(string q)
        {
            Dictionary<string, object> Parse(string o)
            {
                Dictionary<string, object> options = new Dictionary<string, object>();
                Regex re = new Regex(@"(\w+)\=([^\,]+)", RegexOptions.Compiled);
                string[] pairs = o.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in pairs)
                {
                    Match m = re.Match(s);
                    if (!m.Success)
                    {
                        
                    }
                    else if (options.ContainsKey(m.Groups[1].Value))
                    {
                        options[m.Groups[1].Value] = m.Groups[2].Value;
                    }
                    else
                    {
                        options.Add(m.Groups[1].Value, m.Groups[2].Value);
                    }
                }
                return options;
            }
            var search_params = Parse(q).Select(kvp => kvp.Key + ":exact=" + kvp.Value).ToArray();
            return await SearchPatients(search_params);
        }
        #endregion

        #region Fields

        #endregion
    }
}
